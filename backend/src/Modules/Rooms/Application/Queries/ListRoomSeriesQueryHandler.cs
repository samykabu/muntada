using MediatR;
using Microsoft.EntityFrameworkCore;
using Muntada.Rooms.Domain.Series;
using Muntada.Rooms.Infrastructure;

namespace Muntada.Rooms.Application.Queries;

/// <summary>
/// Query to list room series for a tenant with optional status filter and pagination.
/// </summary>
/// <param name="TenantId">The tenant to scope the query to.</param>
/// <param name="Status">Optional status filter.</param>
/// <param name="Page">Page number (1-based).</param>
/// <param name="PageSize">Number of items per page.</param>
public sealed record ListRoomSeriesQuery(
    string TenantId,
    RoomSeriesStatus? Status = null,
    int Page = 1,
    int PageSize = 20) : IRequest<PagedResult<RoomSeries>>;

/// <summary>
/// Handles <see cref="ListRoomSeriesQuery"/>.
/// </summary>
public sealed class ListRoomSeriesQueryHandler : IRequestHandler<ListRoomSeriesQuery, PagedResult<RoomSeries>>
{
    private readonly RoomsDbContext _db;

    /// <summary>
    /// Initializes a new instance of the <see cref="ListRoomSeriesQueryHandler"/> class.
    /// </summary>
    public ListRoomSeriesQueryHandler(RoomsDbContext db)
    {
        _db = db;
    }

    /// <inheritdoc />
    public async Task<PagedResult<RoomSeries>> Handle(ListRoomSeriesQuery request, CancellationToken cancellationToken)
    {
        var query = _db.RoomSeries
            .AsNoTracking()
            .Where(s => s.TenantId == request.TenantId);

        if (request.Status.HasValue)
            query = query.Where(s => s.Status == request.Status.Value);

        query = query.OrderByDescending(s => s.CreatedAt);

        var totalCount = await query.CountAsync(cancellationToken);

        var items = await query
            .Skip((request.Page - 1) * request.PageSize)
            .Take(request.PageSize)
            .ToListAsync(cancellationToken);

        return new PagedResult<RoomSeries>(items, totalCount, request.Page, request.PageSize);
    }
}
