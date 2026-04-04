using MediatR;
using Microsoft.EntityFrameworkCore;
using Muntada.Tenancy.Infrastructure;

namespace Muntada.Tenancy.Application.Queries;

/// <summary>
/// Query to retrieve the current plan and its limits for a specific tenant.
/// </summary>
/// <param name="TenantId">The identifier of the tenant whose plan to retrieve.</param>
public sealed record GetTenantPlanQuery(Guid TenantId) : IRequest<TenantPlanResult?>;

/// <summary>
/// Result containing the tenant's current plan details and associated limits.
/// </summary>
/// <param name="TenantPlanId">The unique identifier of the tenant plan assignment.</param>
/// <param name="PlanDefinitionId">The identifier of the associated plan definition.</param>
/// <param name="PlanName">The display name of the plan.</param>
/// <param name="Tier">The pricing tier of the plan.</param>
/// <param name="MonthlyPriceUsd">The monthly price in US dollars.</param>
/// <param name="StartDate">The UTC date and time when the plan assignment started.</param>
/// <param name="Limits">The usage limits and feature flags for this plan.</param>
public sealed record TenantPlanResult(
    Guid TenantPlanId,
    Guid PlanDefinitionId,
    string PlanName,
    string Tier,
    decimal MonthlyPriceUsd,
    DateTime StartDate,
    PlanLimitsResult Limits);

/// <summary>
/// Represents the usage limits and feature flags for a plan in query results.
/// </summary>
/// <param name="MaxRoomsPerMonth">Maximum rooms per month (0 = unlimited).</param>
/// <param name="MaxParticipantsPerRoom">Maximum participants per room.</param>
/// <param name="MaxStorageGB">Maximum storage in gigabytes.</param>
/// <param name="MaxRecordingHoursPerMonth">Maximum recording hours per month.</param>
/// <param name="MaxDataRetentionDays">Maximum data retention in days.</param>
/// <param name="AllowRecording">Whether recording is allowed.</param>
/// <param name="AllowGuestAccess">Whether guest access is allowed.</param>
/// <param name="AllowCustomBranding">Whether custom branding is allowed.</param>
public sealed record PlanLimitsResult(
    int MaxRoomsPerMonth,
    int MaxParticipantsPerRoom,
    int MaxStorageGB,
    int MaxRecordingHoursPerMonth,
    int MaxDataRetentionDays,
    bool AllowRecording,
    bool AllowGuestAccess,
    bool AllowCustomBranding);

/// <summary>
/// Handles <see cref="GetTenantPlanQuery"/> by joining the current <c>TenantPlan</c>
/// with its <c>PlanDefinition</c> and projecting to <see cref="TenantPlanResult"/>.
/// Returns <c>null</c> when the tenant has no current plan.
/// </summary>
public sealed class GetTenantPlanQueryHandler : IRequestHandler<GetTenantPlanQuery, TenantPlanResult?>
{
    private readonly TenancyDbContext _dbContext;

    /// <summary>
    /// Initializes a new instance of <see cref="GetTenantPlanQueryHandler"/>.
    /// </summary>
    /// <param name="dbContext">The Tenancy module database context.</param>
    public GetTenantPlanQueryHandler(TenancyDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    /// <summary>
    /// Handles the query by looking up the tenant's current plan and its definition.
    /// </summary>
    /// <param name="request">The tenant plan query.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>The tenant plan result with limits, or <c>null</c> if no current plan exists.</returns>
    public async Task<TenantPlanResult?> Handle(GetTenantPlanQuery request, CancellationToken cancellationToken)
    {
        var currentPlan = await _dbContext.TenantPlans
            .AsNoTracking()
            .Where(tp => tp.TenantId == request.TenantId && tp.IsCurrent)
            .FirstOrDefaultAsync(cancellationToken);

        if (currentPlan is null)
            return null;

        var definition = await _dbContext.PlanDefinitions
            .AsNoTracking()
            .FirstOrDefaultAsync(pd => pd.Id == currentPlan.PlanDefinitionId, cancellationToken);

        if (definition is null)
            return null;

        return new TenantPlanResult(
            TenantPlanId: currentPlan.Id,
            PlanDefinitionId: definition.Id,
            PlanName: definition.Name,
            Tier: definition.Tier.ToString(),
            MonthlyPriceUsd: definition.MonthlyPriceUsd,
            StartDate: currentPlan.StartDate,
            Limits: new PlanLimitsResult(
                MaxRoomsPerMonth: definition.Limits.MaxRoomsPerMonth,
                MaxParticipantsPerRoom: definition.Limits.MaxParticipantsPerRoom,
                MaxStorageGB: definition.Limits.MaxStorageGB,
                MaxRecordingHoursPerMonth: definition.Limits.MaxRecordingHoursPerMonth,
                MaxDataRetentionDays: definition.Limits.MaxDataRetentionDays,
                AllowRecording: definition.Limits.AllowRecording,
                AllowGuestAccess: definition.Limits.AllowGuestAccess,
                AllowCustomBranding: definition.Limits.AllowCustomBranding));
    }
}
