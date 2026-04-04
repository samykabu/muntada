namespace Muntada.Tenancy.Api.Dtos;

/// <summary>
/// Request DTO for upgrading a tenant's subscription plan to a higher tier.
/// </summary>
/// <param name="TargetPlanDefinitionId">The identifier of the target (higher-tier) plan definition.</param>
public sealed record UpgradePlanRequest(Guid TargetPlanDefinitionId);

/// <summary>
/// Request DTO for downgrading a tenant's subscription plan to a lower tier.
/// </summary>
/// <param name="TargetPlanDefinitionId">The identifier of the target (lower-tier) plan definition.</param>
/// <param name="EffectiveDate">"immediate" to downgrade now, or "next-billing-cycle" to schedule for month end.</param>
public sealed record DowngradePlanRequest(Guid TargetPlanDefinitionId, string EffectiveDate);

/// <summary>
/// Response DTO returned after a successful plan upgrade, including pro-rated charge details.
/// </summary>
/// <param name="PreviousPlan">The display name of the previous plan.</param>
/// <param name="NewPlan">The display name of the new plan.</param>
/// <param name="EffectiveDate">The UTC date and time when the upgrade took effect.</param>
/// <param name="ProratedChargeUsd">The pro-rated charge in US dollars for the billing cycle remainder.</param>
/// <param name="NewLimits">The usage limits and feature flags of the new plan.</param>
public sealed record PlanChangeResponse(
    string PreviousPlan,
    string NewPlan,
    DateTime EffectiveDate,
    decimal ProratedChargeUsd,
    PlanLimitsResponse NewLimits);

/// <summary>
/// Response DTO returned after a plan downgrade request, including usage warnings.
/// </summary>
/// <param name="PreviousPlan">The display name of the previous plan.</param>
/// <param name="NewPlan">The display name of the target plan.</param>
/// <param name="EffectiveDate">The UTC date and time when the downgrade takes or will take effect.</param>
/// <param name="IsScheduled">Whether the downgrade is scheduled for a future date.</param>
/// <param name="UsageWarnings">List of resources where current usage exceeds the new plan limits.</param>
/// <param name="NewLimits">The usage limits and feature flags of the target plan.</param>
public sealed record PlanDowngradeResponse(
    string PreviousPlan,
    string NewPlan,
    DateTime EffectiveDate,
    bool IsScheduled,
    IReadOnlyList<UsageWarningResponse> UsageWarnings,
    PlanLimitsResponse NewLimits);

/// <summary>
/// Response DTO representing a warning where current usage exceeds the target plan limits.
/// </summary>
/// <param name="Resource">The resource type (e.g. "rooms", "storage", "recording").</param>
/// <param name="CurrentUsage">The current usage value.</param>
/// <param name="NewLimit">The limit imposed by the target plan.</param>
/// <param name="Message">A user-facing warning message.</param>
public sealed record UsageWarningResponse(
    string Resource,
    long CurrentUsage,
    long NewLimit,
    string Message);
