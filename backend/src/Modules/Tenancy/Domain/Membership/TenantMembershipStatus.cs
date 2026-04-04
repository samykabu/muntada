namespace Muntada.Tenancy.Domain.Membership;

/// <summary>
/// Represents the status of a user's membership within a tenant.
/// </summary>
public enum TenantMembershipStatus
{
    /// <summary>
    /// The membership is active and the user can access the tenant.
    /// </summary>
    Active = 0,

    /// <summary>
    /// The membership invitation is pending acceptance by the user.
    /// </summary>
    Pending = 1,

    /// <summary>
    /// The membership has been deactivated. The user cannot access the tenant.
    /// </summary>
    Inactive = 2
}
