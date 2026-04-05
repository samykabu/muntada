using MediatR;
using Microsoft.EntityFrameworkCore;
using Muntada.Rooms.Domain.Invite;
using Muntada.Rooms.Domain.Occurrence;
using Muntada.Rooms.Infrastructure;

namespace Muntada.Rooms.Application.Queries;

/// <summary>
/// Query to list room invites for a specific occurrence with optional status filter and pagination.
/// </summary>
/// <param name="OccurrenceId">The room occurrence to list invites for.</param>
/// <param name="TenantId">The owning tenant's identifier.</param>
/// <param name="Status">Optional invite status filter.</param>
/// <param name="Page">Page number (1-based).</param>
/// <param name="PageSize">Number of items per page.</param>
public sealed record ListRoomInvitesQuery(
    string OccurrenceId,
    string TenantId,
    RoomInviteStatus? Status = null,
    int Page = 1,
    int PageSize = 20) : IRequest<PagedResult<RoomInvite>>;

/// <summary>
/// Handles <see cref="ListRoomInvitesQuery"/> — returns paginated invites for a room occurrence.
/// </summary>
public sealed class ListRoomInvitesQueryHandler : IRequestHandler<ListRoomInvitesQuery, PagedResult<RoomInvite>>
{
    private readonly RoomsDbContext _db;

    /// <summary>
    /// Initializes a new instance of the <see cref="ListRoomInvitesQueryHandler"/> class.
    /// </summary>
    public ListRoomInvitesQueryHandler(RoomsDbContext db)
    {
        _db = db;
    }

    /// <inheritdoc />
    public async Task<PagedResult<RoomInvite>> Handle(ListRoomInvitesQuery request, CancellationToken cancellationToken)
    {
        var occurrenceId = new RoomOccurrenceId(request.OccurrenceId);

        // Verify the occurrence belongs to the tenant
        var occurrenceExists = await _db.RoomOccurrences
            .AnyAsync(o => o.Id == occurrenceId && o.TenantId == request.TenantId, cancellationToken);

        if (!occurrenceExists)
            throw new SharedKernel.Domain.Exceptions.EntityNotFoundException(
                nameof(RoomOccurrence), request.OccurrenceId);

        var query = _db.RoomInvites
            .AsNoTracking()
            .Where(i => i.RoomOccurrenceId == occurrenceId);

        if (request.Status.HasValue)
            query = query.Where(i => i.Status == request.Status.Value);

        query = query.OrderByDescending(i => i.CreatedAt);

        var totalCount = await query.CountAsync(cancellationToken);

        var items = await query
            .Skip((request.Page - 1) * request.PageSize)
            .Take(request.PageSize)
            .ToListAsync(cancellationToken);

        return new PagedResult<RoomInvite>(items, totalCount, request.Page, request.PageSize);
    }
}
