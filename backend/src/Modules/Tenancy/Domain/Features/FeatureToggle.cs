using Muntada.SharedKernel.Domain;
using Muntada.SharedKernel.Domain.Exceptions;

namespace Muntada.Tenancy.Domain.Features;

/// <summary>
/// Aggregate root representing a feature toggle that controls feature availability
/// across the platform. Supports global, per-tenant, per-user, per-region, and canary scopes
/// with optional per-tenant overrides.
/// </summary>
public class FeatureToggle : AggregateRoot<Guid>
{
    private readonly List<FeatureToggleOverride> _overrides = new();

    /// <summary>
    /// Gets the unique name identifying this feature toggle.
    /// </summary>
    public string FeatureName { get; private set; } = default!;

    /// <summary>
    /// Gets a value indicating whether this feature is globally enabled.
    /// </summary>
    public bool IsEnabled { get; private set; }

    /// <summary>
    /// Gets the scope at which this feature toggle is evaluated.
    /// </summary>
    public FeatureToggleScope Scope { get; private set; }

    /// <summary>
    /// Gets the percentage of traffic to expose to this feature during canary rollouts (0-100).
    /// </summary>
    public int CanaryPercentage { get; private set; }

    /// <summary>
    /// Gets the read-only collection of per-tenant overrides for this feature toggle.
    /// </summary>
    public IReadOnlyList<FeatureToggleOverride> Overrides => _overrides.AsReadOnly();

    /// <summary>
    /// Private constructor for EF Core materialization.
    /// </summary>
    private FeatureToggle() { }

    /// <summary>
    /// Creates a new feature toggle with the specified configuration.
    /// </summary>
    /// <param name="featureName">The unique name of the feature.</param>
    /// <param name="scope">The evaluation scope.</param>
    /// <param name="isEnabled">Whether the feature is initially enabled.</param>
    /// <returns>A new <see cref="FeatureToggle"/> instance.</returns>
    public static FeatureToggle Create(string featureName, FeatureToggleScope scope, bool isEnabled = false)
    {
        return new FeatureToggle
        {
            Id = Guid.NewGuid(),
            FeatureName = featureName,
            IsEnabled = isEnabled,
            Scope = scope,
            CanaryPercentage = 0,
            CreatedAt = DateTimeOffset.UtcNow,
            UpdatedAt = DateTimeOffset.UtcNow
        };
    }

    /// <summary>
    /// Enables this feature toggle globally.
    /// </summary>
    public void Enable()
    {
        IsEnabled = true;
        UpdatedAt = DateTimeOffset.UtcNow;
    }

    /// <summary>
    /// Disables this feature toggle globally.
    /// </summary>
    public void Disable()
    {
        IsEnabled = false;
        UpdatedAt = DateTimeOffset.UtcNow;
    }

    /// <summary>
    /// Sets the canary rollout percentage for progressive feature deployment.
    /// </summary>
    /// <param name="percentage">The percentage of traffic to expose (0-100).</param>
    /// <exception cref="ValidationException">Thrown when the percentage is outside the valid range.</exception>
    public void SetCanaryPercentage(int percentage)
    {
        if (percentage < 0 || percentage > 100)
            throw new ValidationException(nameof(CanaryPercentage),
                "Canary percentage must be between 0 and 100.");

        CanaryPercentage = percentage;
        UpdatedAt = DateTimeOffset.UtcNow;
    }

    /// <summary>
    /// Adds a per-tenant override that enables or disables this feature for a specific tenant,
    /// regardless of the global toggle state.
    /// </summary>
    /// <param name="tenantId">The identifier of the tenant to override.</param>
    /// <param name="enabled">Whether the feature should be enabled for this tenant.</param>
    /// <exception cref="InvalidOperationException">Thrown when an override already exists for the tenant.</exception>
    public void AddOverride(Guid tenantId, bool enabled)
    {
        if (_overrides.Any(o => o.TenantId == tenantId))
            throw new InvalidOperationException(
                $"An override already exists for tenant '{tenantId}'. Remove it first before adding a new one.");

        var toggleOverride = FeatureToggleOverride.Create(Id, tenantId, enabled);
        _overrides.Add(toggleOverride);
        UpdatedAt = DateTimeOffset.UtcNow;
    }

    /// <summary>
    /// Removes the per-tenant override for the specified tenant.
    /// </summary>
    /// <param name="tenantId">The identifier of the tenant whose override should be removed.</param>
    /// <exception cref="InvalidOperationException">Thrown when no override exists for the tenant.</exception>
    public void RemoveOverride(Guid tenantId)
    {
        var existing = _overrides.FirstOrDefault(o => o.TenantId == tenantId);
        if (existing is null)
            throw new InvalidOperationException(
                $"No override exists for tenant '{tenantId}'.");

        _overrides.Remove(existing);
        UpdatedAt = DateTimeOffset.UtcNow;
    }
}
