namespace Muntada.Tenancy.Domain.Features;

/// <summary>
/// Defines the scope at which a feature toggle is evaluated.
/// </summary>
public enum FeatureToggleScope
{
    /// <summary>
    /// The toggle applies globally across all tenants and users.
    /// </summary>
    Global = 0,

    /// <summary>
    /// The toggle is evaluated per tenant.
    /// </summary>
    PerTenant = 1,

    /// <summary>
    /// The toggle is evaluated per individual user.
    /// </summary>
    PerUser = 2,

    /// <summary>
    /// The toggle is evaluated per deployment region.
    /// </summary>
    PerRegion = 3,

    /// <summary>
    /// The toggle targets a canary subset for progressive rollout.
    /// </summary>
    Canary = 4
}
