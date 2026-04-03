using MediatR;
using Microsoft.EntityFrameworkCore;
using Muntada.Identity.Domain.Pat;
using Muntada.Identity.Infrastructure;

namespace Muntada.Identity.Application.Queries;

/// <summary>
/// Query to retrieve all Personal Access Tokens for a given user and tenant.
/// </summary>
/// <param name="UserId">The unique identifier of the user whose PATs are requested.</param>
/// <param name="TenantId">The tenant to scope the PAT query to.</param>
public sealed record ListPatsQuery(Guid UserId, Guid TenantId) : IRequest<List<PatListItemDto>>;

/// <summary>
/// DTO representing a Personal Access Token in a list view (excludes the token hash).
/// </summary>
/// <param name="PatId">The unique identifier of the PAT.</param>
/// <param name="Name">The human-readable name of the PAT.</param>
/// <param name="Scopes">The permission scopes granted to this PAT.</param>
/// <param name="Status">The current status of the PAT.</param>
/// <param name="CreatedAt">The UTC timestamp when the PAT was created.</param>
/// <param name="ExpiresAt">The UTC timestamp when the PAT expires.</param>
/// <param name="LastUsedAt">The UTC timestamp of the last API call made with this PAT.</param>
public sealed record PatListItemDto(
    Guid PatId,
    string Name,
    List<string> Scopes,
    string Status,
    DateTimeOffset CreatedAt,
    DateTimeOffset ExpiresAt,
    DateTimeOffset? LastUsedAt);

/// <summary>
/// Handles <see cref="ListPatsQuery"/> by retrieving all PATs for the specified user
/// and tenant, projecting them to <see cref="PatListItemDto"/> records.
/// </summary>
public sealed class ListPatsQueryHandler : IRequestHandler<ListPatsQuery, List<PatListItemDto>>
{
    private readonly IdentityDbContext _dbContext;

    /// <summary>
    /// Initializes a new instance of <see cref="ListPatsQueryHandler"/>.
    /// </summary>
    /// <param name="dbContext">The Identity module database context.</param>
    public ListPatsQueryHandler(IdentityDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    /// <summary>
    /// Handles the query by fetching all PATs for the user and tenant
    /// and projecting them to list item DTOs.
    /// </summary>
    /// <param name="request">The list PATs query.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>A list of PAT DTOs for the specified user and tenant.</returns>
    public async Task<List<PatListItemDto>> Handle(ListPatsQuery request, CancellationToken cancellationToken)
    {
        var pats = await _dbContext.Set<PersonalAccessToken>()
            .Where(p => p.UserId == request.UserId && p.TenantId == request.TenantId)
            .OrderByDescending(p => p.CreatedAt)
            .ToListAsync(cancellationToken);

        return pats.Select(p => new PatListItemDto(
            p.Id,
            p.Name,
            p.Scopes,
            p.Status.ToString(),
            p.CreatedAt,
            p.ExpiresAt,
            p.LastUsedAt)).ToList();
    }
}
