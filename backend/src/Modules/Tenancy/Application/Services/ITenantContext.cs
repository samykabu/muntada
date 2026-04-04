namespace Muntada.Tenancy.Application.Services;

/// <summary>
/// Provides the current tenant context for the active request.
/// Resolved from JWT claims or X-Tenant-ID header by middleware.
/// </summary>
public interface ITenantContext
{
    /// <summary>Gets the current tenant ID for the active request.</summary>
    Guid TenantId { get; }

    /// <summary>Gets whether a tenant context has been established.</summary>
    bool HasTenant { get; }

    /// <summary>Gets whether the current tenant is suspended (read-only access).</summary>
    bool IsSuspended { get; }
}
