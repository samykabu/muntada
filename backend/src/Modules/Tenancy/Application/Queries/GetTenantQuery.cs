using MediatR;
using Microsoft.EntityFrameworkCore;
using Muntada.Tenancy.Infrastructure;

namespace Muntada.Tenancy.Application.Queries;

/// <summary>
/// Query to retrieve a tenant's details by its unique identifier.
/// </summary>
/// <param name="TenantId">The identifier of the tenant to retrieve.</param>
public sealed record GetTenantQuery(Guid TenantId) : IRequest<TenantDetailResult?>;

/// <summary>
/// Detailed result containing a tenant's information, branding, and metadata.
/// </summary>
/// <param name="Id">The unique identifier of the tenant.</param>
/// <param name="Name">The display name of the tenant.</param>
/// <param name="Slug">The URL-safe slug assigned to the tenant.</param>
/// <param name="Status">The current lifecycle status of the tenant.</param>
/// <param name="BillingStatus">The current billing/subscription status.</param>
/// <param name="TrialEndsAt">The UTC date and time when the trial period expires, or <c>null</c>.</param>
/// <param name="Branding">The tenant's visual branding configuration.</param>
/// <param name="CreatedAt">The UTC date and time when the tenant was created.</param>
public sealed record TenantDetailResult(
    Guid Id,
    string Name,
    string Slug,
    string Status,
    string BillingStatus,
    DateTime? TrialEndsAt,
    BrandingResult Branding,
    DateTime CreatedAt);

/// <summary>
/// Represents a tenant's branding configuration in query results.
/// </summary>
/// <param name="LogoUrl">The URL of the tenant's logo, or <c>null</c>.</param>
/// <param name="PrimaryColor">The primary brand color as a hex string, or <c>null</c>.</param>
/// <param name="SecondaryColor">The secondary brand color as a hex string, or <c>null</c>.</param>
/// <param name="CustomDomain">The tenant's custom domain, or <c>null</c>.</param>
public sealed record BrandingResult(
    string? LogoUrl,
    string? PrimaryColor,
    string? SecondaryColor,
    string? CustomDomain);

/// <summary>
/// Handles <see cref="GetTenantQuery"/> by querying the database for tenant details.
/// Returns <c>null</c> when the tenant is not found.
/// </summary>
public sealed class GetTenantQueryHandler : IRequestHandler<GetTenantQuery, TenantDetailResult?>
{
    private readonly TenancyDbContext _dbContext;

    /// <summary>
    /// Initializes a new instance of <see cref="GetTenantQueryHandler"/>.
    /// </summary>
    /// <param name="dbContext">The Tenancy module database context.</param>
    public GetTenantQueryHandler(TenancyDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    /// <summary>
    /// Handles the query by looking up the tenant and projecting to a detail result.
    /// </summary>
    /// <param name="request">The tenant query.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>The tenant detail result, or <c>null</c> if the tenant was not found.</returns>
    public async Task<TenantDetailResult?> Handle(GetTenantQuery request, CancellationToken cancellationToken)
    {
        var tenant = await _dbContext.Tenants
            .AsNoTracking()
            .FirstOrDefaultAsync(t => t.Id == request.TenantId, cancellationToken);

        if (tenant is null)
            return null;

        return new TenantDetailResult(
            Id: tenant.Id,
            Name: tenant.Name,
            Slug: tenant.Slug.Value,
            Status: tenant.Status.ToString(),
            BillingStatus: tenant.BillingStatus.ToString(),
            TrialEndsAt: tenant.TrialEndsAt,
            Branding: new BrandingResult(
                LogoUrl: tenant.Branding.LogoUrl,
                PrimaryColor: tenant.Branding.PrimaryColor,
                SecondaryColor: tenant.Branding.SecondaryColor,
                CustomDomain: tenant.Branding.CustomDomain),
            CreatedAt: tenant.CreatedAt.UtcDateTime);
    }
}
