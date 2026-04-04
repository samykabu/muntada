namespace Muntada.Tenancy.Domain.Tenant;

/// <summary>
/// Represents the lifecycle status of a tenant.
/// </summary>
public enum TenantStatus
{
    /// <summary>
    /// The tenant is active and fully operational.
    /// </summary>
    Active = 0,

    /// <summary>
    /// The tenant has been suspended (e.g. due to billing issues or policy violations).
    /// Access is restricted until reactivation.
    /// </summary>
    Suspended = 1,

    /// <summary>
    /// The tenant has been soft-deleted and is pending permanent removal
    /// according to the configured retention policy.
    /// </summary>
    Deleted = 2
}
