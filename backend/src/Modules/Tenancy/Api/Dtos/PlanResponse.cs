namespace Muntada.Tenancy.Api.Dtos;

/// <summary>
/// Response DTO representing a tenant's current plan with its usage limits and feature flags.
/// </summary>
/// <param name="TenantPlanId">The unique identifier of the tenant plan assignment.</param>
/// <param name="PlanDefinitionId">The identifier of the associated plan definition.</param>
/// <param name="PlanName">The display name of the plan.</param>
/// <param name="Tier">The pricing tier of the plan.</param>
/// <param name="MonthlyPriceUsd">The monthly price in US dollars.</param>
/// <param name="StartDate">The UTC date and time when the plan assignment started.</param>
/// <param name="Limits">The usage limits and feature flags for this plan.</param>
public sealed record PlanResponse(
    Guid TenantPlanId,
    Guid PlanDefinitionId,
    string PlanName,
    string Tier,
    decimal MonthlyPriceUsd,
    DateTime StartDate,
    PlanLimitsResponse Limits);

/// <summary>
/// Response DTO representing the usage limits and feature flags of a plan.
/// </summary>
/// <param name="MaxRoomsPerMonth">Maximum rooms per month (0 = unlimited).</param>
/// <param name="MaxParticipantsPerRoom">Maximum participants per room.</param>
/// <param name="MaxStorageGB">Maximum storage in gigabytes.</param>
/// <param name="MaxRecordingHoursPerMonth">Maximum recording hours per month.</param>
/// <param name="MaxDataRetentionDays">Maximum data retention in days.</param>
/// <param name="AllowRecording">Whether recording is allowed.</param>
/// <param name="AllowGuestAccess">Whether guest access is allowed.</param>
/// <param name="AllowCustomBranding">Whether custom branding is allowed.</param>
public sealed record PlanLimitsResponse(
    int MaxRoomsPerMonth,
    int MaxParticipantsPerRoom,
    int MaxStorageGB,
    int MaxRecordingHoursPerMonth,
    int MaxDataRetentionDays,
    bool AllowRecording,
    bool AllowGuestAccess,
    bool AllowCustomBranding);
