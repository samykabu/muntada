namespace Muntada.Tenancy.Domain.Tenant;

/// <summary>
/// Represents the billing status of a tenant's subscription.
/// </summary>
public enum BillingStatus
{
    /// <summary>
    /// The tenant has an active, paid subscription in good standing.
    /// </summary>
    Active = 0,

    /// <summary>
    /// The tenant is on a time-limited trial period.
    /// </summary>
    Trial = 1,

    /// <summary>
    /// The tenant's payment is overdue. Grace period rules apply.
    /// </summary>
    Overdue = 2,

    /// <summary>
    /// The tenant's subscription has been cancelled.
    /// </summary>
    Cancelled = 3
}
