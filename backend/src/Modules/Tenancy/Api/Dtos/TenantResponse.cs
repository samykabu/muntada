namespace Muntada.Tenancy.Api.Dtos;

/// <summary>
/// Response DTO representing a tenant's details.
/// </summary>
/// <param name="Id">The unique identifier of the tenant.</param>
/// <param name="Name">The display name of the tenant.</param>
/// <param name="Slug">The URL-safe slug assigned to the tenant.</param>
/// <param name="Status">The current lifecycle status of the tenant.</param>
/// <param name="BillingStatus">The current billing/subscription status.</param>
/// <param name="TrialEndsAt">The UTC date and time when the trial period expires, or <c>null</c>.</param>
/// <param name="Branding">The tenant's visual branding configuration.</param>
/// <param name="CreatedAt">The UTC date and time when the tenant was created.</param>
public sealed record TenantResponse(
    Guid Id,
    string Name,
    string Slug,
    string Status,
    string BillingStatus,
    DateTime? TrialEndsAt,
    TenantBrandingResponse Branding,
    DateTime CreatedAt);

/// <summary>
/// Response DTO representing a tenant's branding configuration.
/// </summary>
/// <param name="LogoUrl">The URL of the tenant's logo, or <c>null</c>.</param>
/// <param name="PrimaryColor">The primary brand color as a hex string, or <c>null</c>.</param>
/// <param name="SecondaryColor">The secondary brand color as a hex string, or <c>null</c>.</param>
/// <param name="CustomDomain">The tenant's custom domain, or <c>null</c>.</param>
public sealed record TenantBrandingResponse(
    string? LogoUrl,
    string? PrimaryColor,
    string? SecondaryColor,
    string? CustomDomain);
