namespace Muntada.Tenancy.Application.Services;

/// <summary>
/// Service for evaluating feature toggle state, supporting global, per-tenant override,
/// and canary percentage scopes with caching.
/// </summary>
public interface IFeatureToggleService
{
    /// <summary>
    /// Determines whether a specific feature is enabled for the given tenant.
    /// Evaluates global toggle state, per-tenant overrides, and canary percentage in order.
    /// </summary>
    /// <param name="featureName">The unique name of the feature toggle.</param>
    /// <param name="tenantId">The identifier of the tenant to evaluate for.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns><c>true</c> if the feature is enabled for the tenant; otherwise <c>false</c>.</returns>
    Task<bool> IsFeatureEnabledAsync(
        string featureName,
        Guid tenantId,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Retrieves the list of feature names that are currently enabled for the given tenant.
    /// </summary>
    /// <param name="tenantId">The identifier of the tenant.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>A list of enabled feature names for the tenant.</returns>
    Task<IReadOnlyList<string>> GetEnabledFeaturesAsync(
        Guid tenantId,
        CancellationToken cancellationToken = default);
}
