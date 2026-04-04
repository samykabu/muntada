using MediatR;
using Microsoft.EntityFrameworkCore;
using Muntada.Tenancy.Domain.Membership;
using Muntada.Tenancy.Infrastructure;

namespace Muntada.Tenancy.Application.Queries;

/// <summary>
/// Query to retrieve a paginated list of tenant members, optionally filtered by status.
/// </summary>
/// <param name="TenantId">The identifier of the tenant whose members to retrieve.</param>
/// <param name="Status">Optional membership status filter (Active, Pending, Inactive).</param>
/// <param name="Page">The 1-based page number (defaults to 1).</param>
/// <param name="PageSize">The number of items per page (defaults to 20, max 100).</param>
public sealed record GetTenantMembersQuery(
    Guid TenantId,
    TenantMembershipStatus? Status = null,
    int Page = 1,
    int PageSize = 20) : IRequest<TenantMembersResult>;

/// <summary>
/// Paginated result containing tenant member items and pagination metadata.
/// </summary>
/// <param name="Items">The list of member items for the current page.</param>
/// <param name="TotalCount">The total number of members matching the filter.</param>
/// <param name="Page">The current page number.</param>
/// <param name="PageSize">The number of items per page.</param>
public sealed record TenantMembersResult(
    IReadOnlyList<TenantMemberItem> Items,
    int TotalCount,
    int Page,
    int PageSize);

/// <summary>
/// Represents a single tenant member in query results.
/// </summary>
/// <param name="Id">The unique identifier of the membership.</param>
/// <param name="UserId">The identifier of the user, or <c>null</c> if the invite is pending.</param>
/// <param name="Email">The email address of the member or invited user, or <c>null</c>.</param>
/// <param name="DisplayName">The display name of the member, or <c>null</c> if not yet known.</param>
/// <param name="Role">The role assigned to the member.</param>
/// <param name="Status">The current membership status.</param>
/// <param name="JoinedAt">The UTC date and time when the member joined, or <c>null</c>.</param>
/// <param name="LastActivityAt">The UTC date and time of last activity, or <c>null</c>.</param>
public sealed record TenantMemberItem(
    Guid Id,
    Guid? UserId,
    string? Email,
    string? DisplayName,
    string Role,
    string Status,
    DateTime? JoinedAt,
    DateTime? LastActivityAt);

/// <summary>
/// Handles <see cref="GetTenantMembersQuery"/> by querying the database for tenant memberships
/// with optional status filtering and pagination.
/// </summary>
public sealed class GetTenantMembersQueryHandler : IRequestHandler<GetTenantMembersQuery, TenantMembersResult>
{
    private readonly TenancyDbContext _dbContext;

    /// <summary>
    /// Initializes a new instance of <see cref="GetTenantMembersQueryHandler"/>.
    /// </summary>
    /// <param name="dbContext">The Tenancy module database context.</param>
    public GetTenantMembersQueryHandler(TenancyDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    /// <summary>
    /// Handles the query by filtering memberships by tenant and optional status,
    /// applying pagination, and projecting to result items.
    /// </summary>
    /// <param name="request">The members query.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>A paginated result of tenant members.</returns>
    public async Task<TenantMembersResult> Handle(GetTenantMembersQuery request, CancellationToken cancellationToken)
    {
        var page = Math.Max(1, request.Page);
        var pageSize = Math.Clamp(request.PageSize, 1, 100);

        var query = _dbContext.TenantMemberships
            .AsNoTracking()
            .Where(m => m.TenantId == request.TenantId);

        if (request.Status.HasValue)
        {
            query = query.Where(m => m.Status == request.Status.Value);
        }

        var totalCount = await query.CountAsync(cancellationToken);

        var items = await query
            .OrderBy(m => m.Role)
            .ThenByDescending(m => m.JoinedAt ?? m.InvitedAt)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .Select(m => new TenantMemberItem(
                Id: m.Id,
                UserId: m.UserId,
                Email: m.InvitedEmail,
                DisplayName: null, // TODO: Join with Identity module user data when available
                Role: m.Role.ToString(),
                Status: m.Status.ToString(),
                JoinedAt: m.JoinedAt,
                LastActivityAt: null)) // TODO: Track last activity when activity logging is available
            .ToListAsync(cancellationToken);

        return new TenantMembersResult(
            Items: items,
            TotalCount: totalCount,
            Page: page,
            PageSize: pageSize);
    }
}
