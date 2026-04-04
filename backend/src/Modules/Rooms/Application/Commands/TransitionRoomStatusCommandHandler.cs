using MediatR;
using Microsoft.EntityFrameworkCore;
using Muntada.Rooms.Domain.Occurrence;
using Muntada.Rooms.Infrastructure;
using Muntada.SharedKernel.Domain.Exceptions;

namespace Muntada.Rooms.Application.Commands;

/// <summary>
/// Command to transition a room occurrence to a new status via the state machine.
/// Used for explicit transitions such as moderator ending the room.
/// </summary>
/// <param name="TenantId">The owning tenant's identifier.</param>
/// <param name="OccurrenceId">The occurrence to transition.</param>
/// <param name="Trigger">The state machine trigger to fire.</param>
public sealed record TransitionRoomStatusCommand(
    string TenantId,
    string OccurrenceId,
    RoomTrigger Trigger) : IRequest<RoomOccurrence>;

/// <summary>
/// Handles <see cref="TransitionRoomStatusCommand"/> — validates the transition is permitted
/// by the state machine, then applies it.
/// </summary>
public sealed class TransitionRoomStatusCommandHandler : IRequestHandler<TransitionRoomStatusCommand, RoomOccurrence>
{
    private readonly RoomsDbContext _db;

    /// <summary>
    /// Initializes a new instance of the <see cref="TransitionRoomStatusCommandHandler"/> class.
    /// </summary>
    public TransitionRoomStatusCommandHandler(RoomsDbContext db)
    {
        _db = db;
    }

    /// <inheritdoc />
    public async Task<RoomOccurrence> Handle(TransitionRoomStatusCommand request, CancellationToken cancellationToken)
    {
        var occurrenceId = new RoomOccurrenceId(request.OccurrenceId);
        var occurrence = await _db.RoomOccurrences
            .FirstOrDefaultAsync(o => o.Id == occurrenceId && o.TenantId == request.TenantId, cancellationToken);

        if (occurrence is null)
            throw new EntityNotFoundException(nameof(RoomOccurrence), request.OccurrenceId);

        // Validate the transition is allowed before attempting it
        if (!occurrence.CanFire(request.Trigger))
            throw new ValidationException(
                "Trigger",
                $"Cannot apply trigger '{request.Trigger}' when room is in '{occurrence.Status}' status.");

        // Apply the appropriate domain method based on the trigger
        switch (request.Trigger)
        {
            case RoomTrigger.FirstParticipantJoins:
                occurrence.GoLive();
                break;
            case RoomTrigger.ModeratorDisconnects:
                occurrence.StartGracePeriod();
                break;
            case RoomTrigger.ModeratorReconnects:
                occurrence.ModeratorReconnects();
                break;
            case RoomTrigger.EndRoom:
                occurrence.End();
                break;
            case RoomTrigger.RetentionExpires:
                occurrence.Archive();
                break;
            default:
                throw new ValidationException(
                    "Trigger",
                    $"Trigger '{request.Trigger}' is not supported for explicit transitions.");
        }

        occurrence.IncrementVersion();
        await _db.SaveChangesAsync(cancellationToken);

        return occurrence;
    }
}
