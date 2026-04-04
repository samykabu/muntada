namespace Muntada.Tenancy.Api.Dtos;

/// <summary>
/// Request DTO for creating a new tenant.
/// </summary>
/// <param name="Name">The display name of the tenant (3-100 characters).</param>
/// <param name="Slug">Optional URL-safe slug; auto-generated from <paramref name="Name"/> if omitted.</param>
/// <param name="Industry">Optional industry classification for the tenant.</param>
/// <param name="TeamSize">Optional team size descriptor.</param>
public sealed record CreateTenantRequest(
    string Name,
    string? Slug,
    string? Industry,
    string? TeamSize);
