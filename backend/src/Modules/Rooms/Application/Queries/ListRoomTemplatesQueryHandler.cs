using MediatR;
using Microsoft.EntityFrameworkCore;
using Muntada.Rooms.Domain.Template;
using Muntada.Rooms.Infrastructure;

namespace Muntada.Rooms.Application.Queries;

/// <summary>
/// Query to list room templates for a tenant with pagination.
/// </summary>
/// <param name="TenantId">The tenant to scope the query to.</param>
/// <param name="Page">Page number (1-based).</param>
/// <param name="PageSize">Number of items per page.</param>
public sealed record ListRoomTemplatesQuery(string TenantId, int Page = 1, int PageSize = 20) : IRequest<PagedResult<RoomTemplate>>;

/// <summary>
/// Represents a paginated result set.
/// </summary>
/// <typeparam name="T">The type of items in the result.</typeparam>
/// <param name="Items">The items on the current page.</param>
/// <param name="TotalCount">The total number of items across all pages.</param>
/// <param name="Page">The current page number.</param>
/// <param name="PageSize">The number of items per page.</param>
public sealed record PagedResult<T>(IReadOnlyList<T> Items, int TotalCount, int Page, int PageSize);

/// <summary>
/// Handles <see cref="ListRoomTemplatesQuery"/>.
/// </summary>
public sealed class ListRoomTemplatesQueryHandler : IRequestHandler<ListRoomTemplatesQuery, PagedResult<RoomTemplate>>
{
    private readonly RoomsDbContext _db;

    /// <summary>
    /// Initializes a new instance of the <see cref="ListRoomTemplatesQueryHandler"/> class.
    /// </summary>
    public ListRoomTemplatesQueryHandler(RoomsDbContext db)
    {
        _db = db;
    }

    /// <inheritdoc />
    public async Task<PagedResult<RoomTemplate>> Handle(ListRoomTemplatesQuery request, CancellationToken cancellationToken)
    {
        var query = _db.RoomTemplates
            .AsNoTracking()
            .Where(t => t.TenantId == request.TenantId)
            .OrderByDescending(t => t.CreatedAt);

        var totalCount = await query.CountAsync(cancellationToken);

        var items = await query
            .Skip((request.Page - 1) * request.PageSize)
            .Take(request.PageSize)
            .ToListAsync(cancellationToken);

        return new PagedResult<RoomTemplate>(items, totalCount, request.Page, request.PageSize);
    }
}
