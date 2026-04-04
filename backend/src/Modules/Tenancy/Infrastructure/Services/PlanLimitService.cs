using Microsoft.EntityFrameworkCore;
using Muntada.Tenancy.Application.Services;
using Muntada.Tenancy.Domain.Plan;

namespace Muntada.Tenancy.Infrastructure.Services;

/// <summary>
/// Implements plan limit checking by querying the tenant's current plan definition
/// and comparing usage against the configured <see cref="PlanLimits"/>.
/// </summary>
public sealed class PlanLimitService : IPlanLimitService
{
    private readonly TenancyDbContext _dbContext;

    /// <summary>
    /// Initializes a new instance of <see cref="PlanLimitService"/>.
    /// </summary>
    /// <param name="dbContext">The Tenancy module database context.</param>
    public PlanLimitService(TenancyDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    /// <inheritdoc />
    public async Task<LimitCheckResult> CheckLimitAsync(Guid tenantId, string resourceType, CancellationToken ct = default)
    {
        var planLimits = await GetCurrentPlanLimitsAsync(tenantId, ct);

        if (planLimits is null)
        {
            return new LimitCheckResult(false, resourceType, 0, 0,
                "No active plan found for this tenant. Please contact support.");
        }

        return resourceType.ToLowerInvariant() switch
        {
            "rooms" => await CheckRoomLimitAsync(tenantId, planLimits, ct),
            "participants" => CheckParticipantLimit(planLimits),
            "storage" => CheckStorageLimit(planLimits),
            "recording" => CheckRecordingLimit(planLimits),
            _ => new LimitCheckResult(false, resourceType, 0, 0,
                $"Unknown resource type: {resourceType}.")
        };
    }

    /// <inheritdoc />
    public async Task<bool> IsFeatureAllowedAsync(Guid tenantId, string featureName, CancellationToken ct = default)
    {
        var planLimits = await GetCurrentPlanLimitsAsync(tenantId, ct);

        if (planLimits is null)
            return false;

        return featureName.ToLowerInvariant() switch
        {
            "recording" => planLimits.AllowRecording,
            "guest_access" => planLimits.AllowGuestAccess,
            "custom_branding" => planLimits.AllowCustomBranding,
            _ => false
        };
    }

    /// <summary>
    /// Retrieves the <see cref="PlanLimits"/> for the tenant's current active plan.
    /// </summary>
    private async Task<PlanLimits?> GetCurrentPlanLimitsAsync(Guid tenantId, CancellationToken ct)
    {
        var currentPlan = await _dbContext.TenantPlans
            .AsNoTracking()
            .Where(tp => tp.TenantId == tenantId && tp.IsCurrent)
            .Select(tp => tp.PlanDefinitionId)
            .FirstOrDefaultAsync(ct);

        if (currentPlan == Guid.Empty)
            return null;

        var definition = await _dbContext.PlanDefinitions
            .AsNoTracking()
            .FirstOrDefaultAsync(pd => pd.Id == currentPlan, ct);

        return definition?.Limits;
    }

    /// <summary>
    /// Checks the monthly room creation limit for the tenant.
    /// A limit of 0 means unlimited rooms.
    /// </summary>
    private async Task<LimitCheckResult> CheckRoomLimitAsync(Guid tenantId, PlanLimits limits, CancellationToken ct)
    {
        // 0 means unlimited
        if (limits.MaxRoomsPerMonth == 0)
            return new LimitCheckResult(true, "rooms", 0, 0, null);

        // TODO: Replace with actual room count from Rooms module when available.
        // For now, return allowed since we cannot query cross-module tables directly.
        long currentUsage = 0;

        if (currentUsage >= limits.MaxRoomsPerMonth)
        {
            return new LimitCheckResult(false, "rooms", currentUsage, limits.MaxRoomsPerMonth,
                "Room limit reached for this month. Upgrade your plan or contact support.");
        }

        return new LimitCheckResult(true, "rooms", currentUsage, limits.MaxRoomsPerMonth, null);
    }

    /// <summary>
    /// Returns the participant-per-room limit from the plan.
    /// This is a capacity check rather than a cumulative usage check.
    /// </summary>
    private static LimitCheckResult CheckParticipantLimit(PlanLimits limits)
    {
        return new LimitCheckResult(true, "participants", 0, limits.MaxParticipantsPerRoom,
            limits.MaxParticipantsPerRoom > 0 ? null : "Room is at capacity.");
    }

    /// <summary>
    /// Checks the storage limit for the tenant.
    /// </summary>
    private static LimitCheckResult CheckStorageLimit(PlanLimits limits)
    {
        // TODO: Replace with actual storage usage from Storage module when available.
        long currentUsageGb = 0;

        if (limits.MaxStorageGB > 0 && currentUsageGb >= limits.MaxStorageGB)
        {
            return new LimitCheckResult(false, "storage", currentUsageGb, limits.MaxStorageGB,
                "Storage limit exceeded. Upgrade your plan.");
        }

        return new LimitCheckResult(true, "storage", currentUsageGb, limits.MaxStorageGB, null);
    }

    /// <summary>
    /// Checks whether recording is allowed and if the recording hours limit is exceeded.
    /// </summary>
    private static LimitCheckResult CheckRecordingLimit(PlanLimits limits)
    {
        if (!limits.AllowRecording)
        {
            return new LimitCheckResult(false, "recording", 0, 0,
                "Recording is not available in your plan.");
        }

        // TODO: Replace with actual recording hours from Rooms module when available.
        long currentHours = 0;

        if (limits.MaxRecordingHoursPerMonth > 0 && currentHours >= limits.MaxRecordingHoursPerMonth)
        {
            return new LimitCheckResult(false, "recording", currentHours, limits.MaxRecordingHoursPerMonth,
                "Recording hours limit reached for this month. Upgrade your plan.");
        }

        return new LimitCheckResult(true, "recording", currentHours, limits.MaxRecordingHoursPerMonth, null);
    }
}
