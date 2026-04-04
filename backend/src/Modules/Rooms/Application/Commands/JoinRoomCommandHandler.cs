using MediatR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Muntada.Rooms.Domain.Invite;
using Muntada.Rooms.Domain.Occurrence;
using Muntada.Rooms.Domain.Participant;
using Muntada.Rooms.Infrastructure;
using Muntada.SharedKernel.Domain.Exceptions;

namespace Muntada.Rooms.Application.Commands;

/// <summary>
/// Command to join a room occurrence using an invite token.
/// </summary>
/// <param name="OccurrenceId">The room occurrence to join.</param>
/// <param name="Token">The invite token for validation.</param>
/// <param name="UserId">The authenticated user ID, or null for guests.</param>
/// <param name="DisplayName">Display name for the participant.</param>
/// <param name="TenantId">The tenant identifier for cross-tenant enforcement.</param>
public sealed record JoinRoomCommand(
    string OccurrenceId,
    string Token,
    string? UserId,
    string? DisplayName,
    string? TenantId = null) : IRequest<JoinRoomResult>;

/// <summary>
/// Result returned after successfully joining a room.
/// </summary>
/// <param name="ParticipantState">The created participant state.</param>
/// <param name="Occurrence">The room occurrence that was joined.</param>
public sealed record JoinRoomResult(
    RoomParticipantState ParticipantState,
    RoomOccurrence Occurrence);

/// <summary>
/// Handles <see cref="JoinRoomCommand"/> — validates the invite token,
/// checks room state and capacity, creates a participant record, and marks the invite as accepted.
/// </summary>
public sealed class JoinRoomCommandHandler : IRequestHandler<JoinRoomCommand, JoinRoomResult>
{
    private readonly RoomsDbContext _db;
    private readonly ILogger<JoinRoomCommandHandler> _logger;

    /// <summary>
    /// Initializes a new instance of the <see cref="JoinRoomCommandHandler"/> class.
    /// </summary>
    public JoinRoomCommandHandler(RoomsDbContext db, ILogger<JoinRoomCommandHandler> logger)
    {
        _db = db;
        _logger = logger;
    }

    /// <inheritdoc />
    public async Task<JoinRoomResult> Handle(JoinRoomCommand request, CancellationToken cancellationToken)
    {
        using var activity = RoomsTelemetry.ParticipantJoin(request.OccurrenceId, request.UserId, "pending");
        // Validate the invite token
        var invite = await _db.RoomInvites
            .FirstOrDefaultAsync(i => i.InviteToken == request.Token, cancellationToken);

        if (invite is null || !invite.IsValid())
            throw new ValidationException("Token", "Invalid or expired invite token.");

        // Verify occurrenceId matches the invite
        var occurrenceId = new RoomOccurrenceId(request.OccurrenceId);
        if (invite.RoomOccurrenceId != occurrenceId)
            throw new ValidationException("OccurrenceId", "Invite token does not match the specified occurrence.");

        // Load the occurrence
        var occurrence = await _db.RoomOccurrences
            .FirstOrDefaultAsync(o => o.Id == occurrenceId, cancellationToken);

        if (occurrence is null)
            throw new EntityNotFoundException(nameof(RoomOccurrence), request.OccurrenceId);

        // Enforce tenantId — ensure the caller's tenant matches the occurrence
        if (request.TenantId is not null && occurrence.TenantId != request.TenantId)
            throw new ValidationException("TenantId", "Occurrence does not belong to the expected tenant.");

        // Validate room state
        if (occurrence.Status != RoomOccurrenceStatus.Scheduled && occurrence.Status != RoomOccurrenceStatus.Live)
            throw new ValidationException(
                "Status",
                $"Cannot join room in '{occurrence.Status}' status. Room must be Scheduled or Live.");

        // Check participant count against plan limit
        var currentParticipantCount = await _db.RoomParticipantStates
            .CountAsync(p => p.RoomOccurrenceId == occurrenceId && p.LeftAt == null, cancellationToken);

        if (currentParticipantCount >= occurrence.Settings.MaxParticipants)
            throw new ValidationException(
                "MaxParticipants",
                $"Room has reached its maximum capacity of {occurrence.Settings.MaxParticipants} participants.");

        // Determine role based on invite type
        var role = invite.InviteType == RoomInviteType.GuestMagicLink
            ? ParticipantRole.Guest
            : ParticipantRole.Member;

        var displayName = request.DisplayName ?? request.UserId ?? "Guest";

        // Create participant state
        var participantState = RoomParticipantState.Create(
            occurrenceId,
            request.UserId,
            displayName,
            role);

        _db.RoomParticipantStates.Add(participantState);

        // Mark invite as accepted
        invite.Accept();

        await _db.SaveChangesAsync(cancellationToken);

        activity?.SetTag("rooms.participant_role", participantState.Role.ToString());
        RoomsLogging.ParticipantJoined(_logger, request.OccurrenceId, participantState.DisplayName, participantState.Role.ToString(), null);

        return new JoinRoomResult(participantState, occurrence);
    }
}
