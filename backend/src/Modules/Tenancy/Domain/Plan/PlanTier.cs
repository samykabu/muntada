namespace Muntada.Tenancy.Domain.Plan;

/// <summary>
/// Represents the pricing tier of a plan definition.
/// </summary>
public enum PlanTier
{
    /// <summary>
    /// A free tier with limited features and usage quotas.
    /// </summary>
    Free = 0,

    /// <summary>
    /// A time-limited trial tier providing access to premium features.
    /// </summary>
    Trial = 1,

    /// <summary>
    /// A paid starter tier for small teams.
    /// </summary>
    Starter = 2,

    /// <summary>
    /// A paid professional tier for growing organizations.
    /// </summary>
    Professional = 3,

    /// <summary>
    /// An enterprise tier with custom limits and dedicated support.
    /// </summary>
    Enterprise = 4
}
