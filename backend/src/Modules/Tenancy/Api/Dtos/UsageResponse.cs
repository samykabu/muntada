namespace Muntada.Tenancy.Api.Dtos;

/// <summary>
/// Response DTO representing a tenant's current usage metrics with threshold information.
/// </summary>
/// <param name="TenantId">The identifier of the tenant.</param>
/// <param name="PlanName">The display name of the tenant's current plan.</param>
/// <param name="Metrics">The list of usage metrics with threshold status.</param>
public sealed record UsageResponse(
    Guid TenantId,
    string PlanName,
    IReadOnlyList<UsageMetricResponse> Metrics);

/// <summary>
/// Response DTO representing a single resource usage metric.
/// </summary>
/// <param name="Resource">The resource type (e.g. "rooms", "storage", "recording").</param>
/// <param name="Current">The current usage value.</param>
/// <param name="Limit">The maximum allowed by the plan (0 = unlimited).</param>
/// <param name="Unit">The unit of measurement (e.g. "rooms/month", "GB", "hours/month").</param>
/// <param name="PercentUsed">The percentage of the limit currently used (0 if unlimited).</param>
/// <param name="ThresholdStatus">The threshold status: "normal", "warning", "critical", or "exceeded".</param>
public sealed record UsageMetricResponse(
    string Resource,
    long Current,
    long Limit,
    string Unit,
    int PercentUsed,
    string ThresholdStatus);
