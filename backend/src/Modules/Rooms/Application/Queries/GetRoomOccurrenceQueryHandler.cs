using MediatR;
using Microsoft.EntityFrameworkCore;
using Muntada.Rooms.Domain.Occurrence;
using Muntada.Rooms.Infrastructure;
using Muntada.SharedKernel.Domain.Exceptions;

namespace Muntada.Rooms.Application.Queries;

/// <summary>
/// Query to retrieve a single room occurrence by ID.
/// </summary>
/// <param name="TenantId">The tenant to scope the query to.</param>
/// <param name="OccurrenceId">The occurrence identifier.</param>
public sealed record GetRoomOccurrenceQuery(string TenantId, string OccurrenceId) : IRequest<RoomOccurrence>;

/// <summary>
/// Handles <see cref="GetRoomOccurrenceQuery"/>.
/// </summary>
public sealed class GetRoomOccurrenceQueryHandler : IRequestHandler<GetRoomOccurrenceQuery, RoomOccurrence>
{
    private readonly RoomsDbContext _db;

    /// <summary>
    /// Initializes a new instance of the <see cref="GetRoomOccurrenceQueryHandler"/> class.
    /// </summary>
    public GetRoomOccurrenceQueryHandler(RoomsDbContext db)
    {
        _db = db;
    }

    /// <inheritdoc />
    public async Task<RoomOccurrence> Handle(GetRoomOccurrenceQuery request, CancellationToken cancellationToken)
    {
        var occurrenceId = new RoomOccurrenceId(request.OccurrenceId);
        var occurrence = await _db.RoomOccurrences
            .AsNoTracking()
            .FirstOrDefaultAsync(o => o.Id == occurrenceId && o.TenantId == request.TenantId, cancellationToken);

        if (occurrence is null)
            throw new EntityNotFoundException(nameof(RoomOccurrence), request.OccurrenceId);

        return occurrence;
    }
}
