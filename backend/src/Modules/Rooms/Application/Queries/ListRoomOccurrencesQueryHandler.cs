using MediatR;
using Microsoft.EntityFrameworkCore;
using Muntada.Rooms.Domain.Occurrence;
using Muntada.Rooms.Infrastructure;

namespace Muntada.Rooms.Application.Queries;

/// <summary>
/// Query to list room occurrences for a tenant with optional filters and pagination.
/// </summary>
/// <param name="TenantId">The tenant to scope the query to.</param>
/// <param name="FromDate">Optional start of date range filter (inclusive).</param>
/// <param name="ToDate">Optional end of date range filter (inclusive).</param>
/// <param name="Status">Optional status filter.</param>
/// <param name="SeriesId">Optional series ID filter to get occurrences for a specific series.</param>
/// <param name="Page">Page number (1-based).</param>
/// <param name="PageSize">Number of items per page.</param>
public sealed record ListRoomOccurrencesQuery(
    string TenantId,
    DateTimeOffset? FromDate = null,
    DateTimeOffset? ToDate = null,
    RoomOccurrenceStatus? Status = null,
    string? SeriesId = null,
    int Page = 1,
    int PageSize = 20) : IRequest<PagedResult<RoomOccurrence>>;

/// <summary>
/// Handles <see cref="ListRoomOccurrencesQuery"/>.
/// </summary>
public sealed class ListRoomOccurrencesQueryHandler : IRequestHandler<ListRoomOccurrencesQuery, PagedResult<RoomOccurrence>>
{
    private readonly RoomsDbContext _db;

    /// <summary>
    /// Initializes a new instance of the <see cref="ListRoomOccurrencesQueryHandler"/> class.
    /// </summary>
    public ListRoomOccurrencesQueryHandler(RoomsDbContext db)
    {
        _db = db;
    }

    /// <inheritdoc />
    public async Task<PagedResult<RoomOccurrence>> Handle(ListRoomOccurrencesQuery request, CancellationToken cancellationToken)
    {
        var query = _db.RoomOccurrences
            .AsNoTracking()
            .Where(o => o.TenantId == request.TenantId);

        if (request.FromDate.HasValue)
            query = query.Where(o => o.ScheduledAt >= request.FromDate.Value);

        if (request.ToDate.HasValue)
            query = query.Where(o => o.ScheduledAt <= request.ToDate.Value);

        if (request.Status.HasValue)
            query = query.Where(o => o.Status == request.Status.Value);

        if (!string.IsNullOrWhiteSpace(request.SeriesId))
        {
            var seriesId = new Domain.Series.RoomSeriesId(request.SeriesId);
            query = query.Where(o => o.RoomSeriesId == seriesId);
        }

        query = query.OrderByDescending(o => o.ScheduledAt);

        var totalCount = await query.CountAsync(cancellationToken);

        var items = await query
            .Skip((request.Page - 1) * request.PageSize)
            .Take(request.PageSize)
            .ToListAsync(cancellationToken);

        return new PagedResult<RoomOccurrence>(items, totalCount, request.Page, request.PageSize);
    }
}
