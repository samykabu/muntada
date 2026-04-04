using MediatR;
using Microsoft.EntityFrameworkCore;
using Muntada.Rooms.Domain.Occurrence;
using Muntada.Rooms.Domain.Recording;
using Muntada.Rooms.Infrastructure;

namespace Muntada.Rooms.Application.Queries;

/// <summary>
/// Query to retrieve the most recent recording for a room occurrence.
/// </summary>
/// <param name="TenantId">The owning tenant's identifier.</param>
/// <param name="OccurrenceId">The room occurrence identifier.</param>
public sealed record GetRecordingQuery(string TenantId, string OccurrenceId) : IRequest<Recording?>;

/// <summary>
/// Handles <see cref="GetRecordingQuery"/> — returns the most recent recording for an occurrence.
/// </summary>
public sealed class GetRecordingQueryHandler : IRequestHandler<GetRecordingQuery, Recording?>
{
    private readonly RoomsDbContext _db;

    /// <summary>
    /// Initializes a new instance of the <see cref="GetRecordingQueryHandler"/> class.
    /// </summary>
    public GetRecordingQueryHandler(RoomsDbContext db)
    {
        _db = db;
    }

    /// <inheritdoc />
    public async Task<Recording?> Handle(GetRecordingQuery request, CancellationToken cancellationToken)
    {
        var roomOccurrenceId = new RoomOccurrenceId(request.OccurrenceId);

        return await _db.Recordings
            .AsNoTracking()
            .Where(r => r.RoomOccurrenceId == roomOccurrenceId && r.TenantId == request.TenantId)
            .OrderByDescending(r => r.CreatedAt)
            .FirstOrDefaultAsync(cancellationToken);
    }
}
