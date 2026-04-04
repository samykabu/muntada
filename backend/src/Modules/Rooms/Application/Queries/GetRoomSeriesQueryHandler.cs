using MediatR;
using Microsoft.EntityFrameworkCore;
using Muntada.Rooms.Domain.Series;
using Muntada.Rooms.Infrastructure;
using Muntada.SharedKernel.Domain.Exceptions;

namespace Muntada.Rooms.Application.Queries;

/// <summary>
/// Query to retrieve a single room series by ID.
/// </summary>
/// <param name="TenantId">The tenant to scope the query to.</param>
/// <param name="SeriesId">The series identifier.</param>
public sealed record GetRoomSeriesQuery(string TenantId, string SeriesId) : IRequest<RoomSeries>;

/// <summary>
/// Handles <see cref="GetRoomSeriesQuery"/>.
/// </summary>
public sealed class GetRoomSeriesQueryHandler : IRequestHandler<GetRoomSeriesQuery, RoomSeries>
{
    private readonly RoomsDbContext _db;

    /// <summary>
    /// Initializes a new instance of the <see cref="GetRoomSeriesQueryHandler"/> class.
    /// </summary>
    public GetRoomSeriesQueryHandler(RoomsDbContext db)
    {
        _db = db;
    }

    /// <inheritdoc />
    public async Task<RoomSeries> Handle(GetRoomSeriesQuery request, CancellationToken cancellationToken)
    {
        var seriesId = new RoomSeriesId(request.SeriesId);
        var series = await _db.RoomSeries
            .AsNoTracking()
            .FirstOrDefaultAsync(s => s.Id == seriesId && s.TenantId == request.TenantId, cancellationToken);

        if (series is null)
            throw new EntityNotFoundException(nameof(RoomSeries), request.SeriesId);

        return series;
    }
}
