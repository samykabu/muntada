using MediatR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Muntada.Rooms.Domain.Occurrence;
using Muntada.Rooms.Infrastructure;
using Muntada.SharedKernel.Domain.Exceptions;

namespace Muntada.Rooms.Application.Commands;

/// <summary>
/// Command to assign or change the moderator for a room occurrence.
/// Room must be in Draft or Scheduled status.
/// </summary>
/// <param name="OccurrenceId">The room occurrence to update.</param>
/// <param name="TenantId">The owning tenant's identifier.</param>
/// <param name="ModeratorUserId">The user ID of the new moderator.</param>
public sealed record AssignModeratorCommand(
    string OccurrenceId,
    string TenantId,
    string ModeratorUserId) : IRequest<RoomOccurrence>;

/// <summary>
/// Handles <see cref="AssignModeratorCommand"/> — validates the room is in Draft or Scheduled
/// status and assigns the new moderator.
/// </summary>
public sealed class AssignModeratorCommandHandler : IRequestHandler<AssignModeratorCommand, RoomOccurrence>
{
    private readonly RoomsDbContext _db;
    private readonly ILogger<AssignModeratorCommandHandler> _logger;

    /// <summary>
    /// Initializes a new instance of the <see cref="AssignModeratorCommandHandler"/> class.
    /// </summary>
    public AssignModeratorCommandHandler(RoomsDbContext db, ILogger<AssignModeratorCommandHandler> logger)
    {
        _db = db;
        _logger = logger;
    }

    /// <inheritdoc />
    public async Task<RoomOccurrence> Handle(AssignModeratorCommand request, CancellationToken cancellationToken)
    {
        var occurrenceId = new RoomOccurrenceId(request.OccurrenceId);
        var occurrence = await _db.RoomOccurrences
            .FirstOrDefaultAsync(o => o.Id == occurrenceId && o.TenantId == request.TenantId, cancellationToken);

        if (occurrence is null)
            throw new EntityNotFoundException(nameof(RoomOccurrence), request.OccurrenceId);

        if (occurrence.Status != RoomOccurrenceStatus.Draft && occurrence.Status != RoomOccurrenceStatus.Scheduled)
            throw new ValidationException(
                "Status",
                $"Cannot assign moderator when room is in '{occurrence.Status}' status. Room must be in Draft or Scheduled.");

        occurrence.ChangeModerator(request.ModeratorUserId);
        occurrence.IncrementVersion();
        await _db.SaveChangesAsync(cancellationToken);

        RoomsLogging.ModeratorAssigned(_logger, request.OccurrenceId, request.ModeratorUserId, null);

        return occurrence;
    }
}
