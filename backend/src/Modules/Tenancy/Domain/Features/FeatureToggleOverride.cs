using Muntada.SharedKernel.Domain;

namespace Muntada.Tenancy.Domain.Features;

/// <summary>
/// Represents a per-tenant override for a <see cref="FeatureToggle"/>,
/// allowing a specific tenant to have a different enabled/disabled state
/// than the global toggle configuration.
/// </summary>
public class FeatureToggleOverride : Entity<Guid>
{
    /// <summary>
    /// Gets the identifier of the parent <see cref="FeatureToggle"/>.
    /// </summary>
    public Guid FeatureToggleId { get; private set; }

    /// <summary>
    /// Gets the identifier of the tenant this override applies to.
    /// </summary>
    public Guid TenantId { get; private set; }

    /// <summary>
    /// Gets a value indicating whether the feature is enabled for this specific tenant.
    /// </summary>
    public bool IsEnabled { get; private set; }

    /// <summary>
    /// Private constructor for EF Core materialization.
    /// </summary>
    private FeatureToggleOverride() { }

    /// <summary>
    /// Creates a new feature toggle override for a specific tenant.
    /// </summary>
    /// <param name="featureToggleId">The identifier of the parent feature toggle.</param>
    /// <param name="tenantId">The identifier of the tenant.</param>
    /// <param name="isEnabled">Whether the feature should be enabled for this tenant.</param>
    /// <returns>A new <see cref="FeatureToggleOverride"/> instance.</returns>
    internal static FeatureToggleOverride Create(Guid featureToggleId, Guid tenantId, bool isEnabled)
    {
        return new FeatureToggleOverride
        {
            Id = Guid.NewGuid(),
            FeatureToggleId = featureToggleId,
            TenantId = tenantId,
            IsEnabled = isEnabled
        };
    }
}
