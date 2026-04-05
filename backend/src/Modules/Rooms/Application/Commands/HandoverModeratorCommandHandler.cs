using MediatR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Muntada.Rooms.Domain.Occurrence;
using Muntada.Rooms.Infrastructure;
using Muntada.SharedKernel.Domain.Exceptions;

namespace Muntada.Rooms.Application.Commands;

/// <summary>
/// Command to hand over moderator control to another user during the Grace period.
/// Transitions the room from Grace back to Live with the new moderator.
/// </summary>
/// <param name="OccurrenceId">The room occurrence in Grace status.</param>
/// <param name="TenantId">The owning tenant's identifier.</param>
/// <param name="ToUserId">The user ID of the new moderator taking over.</param>
public sealed record HandoverModeratorCommand(
    string OccurrenceId,
    string TenantId,
    string ToUserId) : IRequest<RoomOccurrence>;

/// <summary>
/// Handles <see cref="HandoverModeratorCommand"/> — validates the room is in Grace status
/// and hands over moderator control to the specified user.
/// </summary>
public sealed class HandoverModeratorCommandHandler : IRequestHandler<HandoverModeratorCommand, RoomOccurrence>
{
    private readonly RoomsDbContext _db;
    private readonly ILogger<HandoverModeratorCommandHandler> _logger;

    /// <summary>
    /// Initializes a new instance of the <see cref="HandoverModeratorCommandHandler"/> class.
    /// </summary>
    public HandoverModeratorCommandHandler(RoomsDbContext db, ILogger<HandoverModeratorCommandHandler> logger)
    {
        _db = db;
        _logger = logger;
    }

    /// <inheritdoc />
    public async Task<RoomOccurrence> Handle(HandoverModeratorCommand request, CancellationToken cancellationToken)
    {
        var occurrenceId = new RoomOccurrenceId(request.OccurrenceId);
        var occurrence = await _db.RoomOccurrences
            .FirstOrDefaultAsync(o => o.Id == occurrenceId && o.TenantId == request.TenantId, cancellationToken);

        if (occurrence is null)
            throw new EntityNotFoundException(nameof(RoomOccurrence), request.OccurrenceId);

        if (occurrence.Status != RoomOccurrenceStatus.Grace)
            throw new ValidationException(
                "Status",
                $"Moderator handover is only allowed during Grace period. Room is currently in '{occurrence.Status}' status.");

        var oldModeratorId = occurrence.ModeratorAssignment?.UserId ?? "unknown";
        occurrence.HandoverModerator(request.ToUserId);
        occurrence.IncrementVersion();
        await _db.SaveChangesAsync(cancellationToken);

        RoomsLogging.ModeratorHandover(_logger, request.OccurrenceId, oldModeratorId, request.ToUserId, null);

        return occurrence;
    }
}
