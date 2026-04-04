namespace Muntada.Tenancy.Application.Services;

/// <summary>
/// Result of a plan limit check for a specific resource type.
/// </summary>
/// <param name="IsAllowed">Whether the requested action is within plan limits.</param>
/// <param name="ResourceType">The type of resource that was checked (e.g. "rooms", "storage", "recording").</param>
/// <param name="CurrentUsage">The current usage count or amount for this resource.</param>
/// <param name="Limit">The maximum allowed by the tenant's current plan (0 = unlimited).</param>
/// <param name="ErrorMessage">A user-facing error message when the limit is exceeded, or <c>null</c> if allowed.</param>
public sealed record LimitCheckResult(
    bool IsAllowed,
    string ResourceType,
    long CurrentUsage,
    long Limit,
    string? ErrorMessage);

/// <summary>
/// Service for checking whether a tenant's current plan allows a given action,
/// based on resource usage limits and feature flags defined in the plan definition.
/// </summary>
public interface IPlanLimitService
{
    /// <summary>
    /// Checks whether the tenant can consume an additional unit of the specified resource type.
    /// Supported resource types: "rooms", "participants", "storage", "recording".
    /// </summary>
    /// <param name="tenantId">The identifier of the tenant.</param>
    /// <param name="resourceType">The resource type to check (e.g. "rooms", "storage").</param>
    /// <param name="ct">Cancellation token.</param>
    /// <returns>A <see cref="LimitCheckResult"/> indicating whether the action is allowed.</returns>
    Task<LimitCheckResult> CheckLimitAsync(Guid tenantId, string resourceType, CancellationToken ct = default);

    /// <summary>
    /// Checks whether a specific feature is enabled for the tenant's current plan.
    /// Supported feature names: "recording", "guest_access", "custom_branding".
    /// </summary>
    /// <param name="tenantId">The identifier of the tenant.</param>
    /// <param name="featureName">The feature name to check (e.g. "recording", "guest_access").</param>
    /// <param name="ct">Cancellation token.</param>
    /// <returns><c>true</c> if the feature is allowed by the plan; otherwise <c>false</c>.</returns>
    Task<bool> IsFeatureAllowedAsync(Guid tenantId, string featureName, CancellationToken ct = default);
}
