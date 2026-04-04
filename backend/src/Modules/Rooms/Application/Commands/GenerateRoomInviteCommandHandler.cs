using MediatR;
using Microsoft.EntityFrameworkCore;
using Muntada.Rooms.Domain.Invite;
using Muntada.Rooms.Domain.Occurrence;
using Muntada.Rooms.Infrastructure;
using Muntada.SharedKernel.Domain.Exceptions;

namespace Muntada.Rooms.Application.Commands;

/// <summary>
/// Command to generate one or more invites for a room occurrence.
/// </summary>
/// <param name="OccurrenceId">The room occurrence to invite participants to.</param>
/// <param name="TenantId">The owning tenant's identifier.</param>
/// <param name="Invites">The list of invite requests to process.</param>
/// <param name="InvitedBy">The user ID of the person creating the invites.</param>
public sealed record GenerateRoomInviteCommand(
    string OccurrenceId,
    string TenantId,
    List<InviteRequest> Invites,
    string InvitedBy) : IRequest<List<RoomInvite>>;

/// <summary>
/// A single invite request within the batch.
/// </summary>
/// <param name="Email">Email address for email invites.</param>
/// <param name="UserId">User ID for direct link invites.</param>
/// <param name="InviteType">The delivery mechanism type.</param>
public sealed record InviteRequest(
    string? Email,
    string? UserId,
    RoomInviteType InviteType);

/// <summary>
/// Handles <see cref="GenerateRoomInviteCommand"/> — creates room invites
/// after validating room state and enforcing rate limits.
/// </summary>
public sealed class GenerateRoomInviteCommandHandler : IRequestHandler<GenerateRoomInviteCommand, List<RoomInvite>>
{
    private readonly RoomsDbContext _db;

    /// <summary>
    /// Initializes a new instance of the <see cref="GenerateRoomInviteCommandHandler"/> class.
    /// </summary>
    public GenerateRoomInviteCommandHandler(RoomsDbContext db)
    {
        _db = db;
    }

    /// <inheritdoc />
    public async Task<List<RoomInvite>> Handle(GenerateRoomInviteCommand request, CancellationToken cancellationToken)
    {
        var occurrenceId = new RoomOccurrenceId(request.OccurrenceId);
        var occurrence = await _db.RoomOccurrences
            .FirstOrDefaultAsync(o => o.Id == occurrenceId && o.TenantId == request.TenantId, cancellationToken);

        if (occurrence is null)
            throw new EntityNotFoundException(nameof(RoomOccurrence), request.OccurrenceId);

        // Validate room is in a state that accepts invites
        if (occurrence.Status != RoomOccurrenceStatus.Scheduled && occurrence.Status != RoomOccurrenceStatus.Live)
            throw new ValidationException(
                "Status",
                $"Cannot invite participants when room is in '{occurrence.Status}' status. Room must be Scheduled or Live.");

        // Rate limit: max 100 invites per room per day
        var today = DateTimeOffset.UtcNow.Date;
        var todayStart = new DateTimeOffset(today, TimeSpan.Zero);
        var invitesTodayCount = await _db.RoomInvites
            .CountAsync(i => i.RoomOccurrenceId == occurrenceId && i.CreatedAt >= todayStart, cancellationToken);

        if (invitesTodayCount + request.Invites.Count > 100)
            throw new ValidationException(
                "RateLimit",
                $"Cannot exceed 100 invites per room per day. Already sent {invitesTodayCount} today.");

        var createdInvites = new List<RoomInvite>();

        foreach (var inviteReq in request.Invites)
        {
            var invite = RoomInvite.Create(
                occurrenceId,
                inviteReq.InviteType,
                request.InvitedBy,
                inviteReq.Email,
                inviteReq.UserId);

            _db.RoomInvites.Add(invite);
            createdInvites.Add(invite);
        }

        await _db.SaveChangesAsync(cancellationToken);

        return createdInvites;
    }
}
