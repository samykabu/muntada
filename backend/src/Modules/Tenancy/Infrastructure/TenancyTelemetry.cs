using System.Diagnostics;

namespace Muntada.Tenancy.Infrastructure;

/// <summary>
/// Provides OpenTelemetry instrumentation for the Tenancy module.
/// Defines an <see cref="ActivitySource"/> named <c>Muntada.Tenancy</c> and
/// static factory methods for creating activities (spans) around key operations.
/// </summary>
public static class TenancyTelemetry
{
    /// <summary>
    /// The name of the <see cref="ActivitySource"/> for the Tenancy module.
    /// </summary>
    public const string SourceName = "Muntada.Tenancy";

    /// <summary>
    /// The shared <see cref="ActivitySource"/> for creating Tenancy module spans.
    /// Register this source with OpenTelemetry tracing configuration to capture traces.
    /// </summary>
    public static readonly ActivitySource ActivitySource = new(SourceName);

    /// <summary>
    /// Creates an activity for tenant creation operations.
    /// </summary>
    /// <param name="tenantId">The identifier of the tenant being created.</param>
    /// <param name="slug">The URL-safe slug assigned to the tenant.</param>
    /// <returns>An <see cref="Activity"/> if a listener is registered; otherwise <c>null</c>.</returns>
    public static Activity? TenantCreation(Guid tenantId, string slug)
    {
        var activity = ActivitySource.StartActivity("Tenancy.TenantCreation");
        activity?.SetTag("tenancy.tenant_id", tenantId.ToString());
        activity?.SetTag("tenancy.slug", slug);
        activity?.SetTag("tenancy.operation", "create");
        return activity;
    }

    /// <summary>
    /// Creates an activity for plan change operations (upgrade or downgrade).
    /// </summary>
    /// <param name="tenantId">The identifier of the tenant changing plans.</param>
    /// <param name="oldPlan">The name or tier of the current plan.</param>
    /// <param name="newPlan">The name or tier of the target plan.</param>
    /// <returns>An <see cref="Activity"/> if a listener is registered; otherwise <c>null</c>.</returns>
    public static Activity? PlanChange(Guid tenantId, string oldPlan, string newPlan)
    {
        var activity = ActivitySource.StartActivity("Tenancy.PlanChange");
        activity?.SetTag("tenancy.tenant_id", tenantId.ToString());
        activity?.SetTag("tenancy.plan.old", oldPlan);
        activity?.SetTag("tenancy.plan.new", newPlan);
        activity?.SetTag("tenancy.operation", "plan_change");
        return activity;
    }

    /// <summary>
    /// Creates an activity for membership change operations (invite, accept, role change, remove).
    /// </summary>
    /// <param name="tenantId">The identifier of the tenant.</param>
    /// <param name="membershipId">The identifier of the membership being modified.</param>
    /// <param name="changeType">The type of membership change (e.g., "invite", "accept", "role_update", "remove").</param>
    /// <returns>An <see cref="Activity"/> if a listener is registered; otherwise <c>null</c>.</returns>
    public static Activity? MembershipChange(Guid tenantId, Guid membershipId, string changeType)
    {
        var activity = ActivitySource.StartActivity("Tenancy.MembershipChange");
        activity?.SetTag("tenancy.tenant_id", tenantId.ToString());
        activity?.SetTag("tenancy.membership_id", membershipId.ToString());
        activity?.SetTag("tenancy.membership.change_type", changeType);
        activity?.SetTag("tenancy.operation", "membership_change");
        return activity;
    }

    /// <summary>
    /// Creates an activity for feature toggle evaluation.
    /// </summary>
    /// <param name="featureName">The name of the feature toggle being evaluated.</param>
    /// <param name="tenantId">The identifier of the tenant the toggle is evaluated for.</param>
    /// <param name="result">Whether the feature is enabled for the tenant.</param>
    /// <returns>An <see cref="Activity"/> if a listener is registered; otherwise <c>null</c>.</returns>
    public static Activity? FeatureToggleEvaluation(string featureName, Guid tenantId, bool result)
    {
        var activity = ActivitySource.StartActivity("Tenancy.FeatureToggleEvaluation");
        activity?.SetTag("tenancy.feature_name", featureName);
        activity?.SetTag("tenancy.tenant_id", tenantId.ToString());
        activity?.SetTag("tenancy.feature_toggle.result", result);
        activity?.SetTag("tenancy.operation", "feature_toggle_evaluation");
        return activity;
    }

    /// <summary>
    /// Creates an activity for usage limit checks.
    /// </summary>
    /// <param name="tenantId">The identifier of the tenant.</param>
    /// <param name="resource">The resource being checked (e.g., "rooms", "storage", "recording_hours").</param>
    /// <param name="currentUsage">The current usage value.</param>
    /// <param name="limit">The maximum allowed value.</param>
    /// <returns>An <see cref="Activity"/> if a listener is registered; otherwise <c>null</c>.</returns>
    public static Activity? UsageCheck(Guid tenantId, string resource, long currentUsage, long limit)
    {
        var activity = ActivitySource.StartActivity("Tenancy.UsageCheck");
        activity?.SetTag("tenancy.tenant_id", tenantId.ToString());
        activity?.SetTag("tenancy.usage.resource", resource);
        activity?.SetTag("tenancy.usage.current", currentUsage);
        activity?.SetTag("tenancy.usage.limit", limit);
        activity?.SetTag("tenancy.usage.percentage", limit > 0 ? (double)currentUsage / limit * 100 : 0);
        activity?.SetTag("tenancy.operation", "usage_check");
        return activity;
    }
}
