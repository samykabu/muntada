using MediatR;
using Microsoft.EntityFrameworkCore;
using Muntada.Tenancy.Application.Services;
using Muntada.Tenancy.Infrastructure;

namespace Muntada.Tenancy.Application.Queries;

/// <summary>
/// Query to retrieve the current usage metrics for a specific tenant,
/// including percentage used and threshold status for each resource.
/// </summary>
/// <param name="TenantId">The identifier of the tenant whose usage to retrieve.</param>
public sealed record GetTenantUsageQuery(Guid TenantId) : IRequest<TenantUsageResult?>;

/// <summary>
/// Result containing the tenant's current usage metrics with threshold information.
/// </summary>
/// <param name="TenantId">The identifier of the tenant.</param>
/// <param name="PlanName">The display name of the tenant's current plan.</param>
/// <param name="Metrics">The list of usage metrics with threshold status.</param>
public sealed record TenantUsageResult(
    Guid TenantId,
    string PlanName,
    IReadOnlyList<UsageMetric> Metrics);

/// <summary>
/// Represents a single resource usage metric with its limit and threshold status.
/// </summary>
/// <param name="Resource">The resource type (e.g. "rooms", "storage", "recording").</param>
/// <param name="Current">The current usage value.</param>
/// <param name="Limit">The maximum allowed by the plan (0 = unlimited).</param>
/// <param name="Unit">The unit of measurement (e.g. "rooms/month", "GB", "hours/month").</param>
/// <param name="PercentUsed">The percentage of the limit currently used (0 if unlimited).</param>
/// <param name="ThresholdStatus">The threshold status: "normal", "warning", "critical", or "exceeded".</param>
public sealed record UsageMetric(
    string Resource,
    long Current,
    long Limit,
    string Unit,
    int PercentUsed,
    string ThresholdStatus);

/// <summary>
/// Handles <see cref="GetTenantUsageQuery"/> by checking the tenant's current plan
/// and computing usage metrics with threshold statuses for each resource type.
/// </summary>
public sealed class GetTenantUsageQueryHandler : IRequestHandler<GetTenantUsageQuery, TenantUsageResult?>
{
    private readonly TenancyDbContext _dbContext;
    private readonly IPlanLimitService _planLimitService;

    /// <summary>
    /// Initializes a new instance of <see cref="GetTenantUsageQueryHandler"/>.
    /// </summary>
    /// <param name="dbContext">The Tenancy module database context.</param>
    /// <param name="planLimitService">Service for checking tenant plan limits and current usage.</param>
    public GetTenantUsageQueryHandler(
        TenancyDbContext dbContext,
        IPlanLimitService planLimitService)
    {
        _dbContext = dbContext;
        _planLimitService = planLimitService;
    }

    /// <summary>
    /// Handles the query by looking up the tenant's current plan and computing
    /// usage metrics for rooms, participants, storage, and recording resources.
    /// </summary>
    /// <param name="request">The usage query.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>The usage result with metrics, or <c>null</c> if no current plan exists.</returns>
    public async Task<TenantUsageResult?> Handle(GetTenantUsageQuery request, CancellationToken cancellationToken)
    {
        // Get current plan name
        var currentPlan = await _dbContext.TenantPlans
            .AsNoTracking()
            .Where(tp => tp.TenantId == request.TenantId && tp.IsCurrent)
            .Select(tp => tp.PlanDefinitionId)
            .FirstOrDefaultAsync(cancellationToken);

        if (currentPlan == Guid.Empty)
            return null;

        var planDef = await _dbContext.PlanDefinitions
            .AsNoTracking()
            .FirstOrDefaultAsync(p => p.Id == currentPlan, cancellationToken);

        if (planDef is null)
            return null;

        // Build metrics for each resource type
        var metrics = new List<UsageMetric>();

        var resourceConfigs = new (string Resource, string Unit)[]
        {
            ("rooms", "rooms/month"),
            ("participants", "per room"),
            ("storage", "GB"),
            ("recording", "hours/month")
        };

        foreach (var (resource, unit) in resourceConfigs)
        {
            var check = await _planLimitService.CheckLimitAsync(request.TenantId, resource, cancellationToken);

            var percentUsed = check.Limit > 0
                ? (int)(check.CurrentUsage * 100 / check.Limit)
                : 0;

            var status = DetermineThresholdStatus(percentUsed, check.Limit);

            metrics.Add(new UsageMetric(
                Resource: resource,
                Current: check.CurrentUsage,
                Limit: check.Limit,
                Unit: unit,
                PercentUsed: percentUsed,
                ThresholdStatus: status));
        }

        return new TenantUsageResult(
            TenantId: request.TenantId,
            PlanName: planDef.Name,
            Metrics: metrics);
    }

    /// <summary>
    /// Determines the threshold status based on the percentage of a resource used.
    /// </summary>
    /// <param name="percentUsed">The current usage percentage.</param>
    /// <param name="limit">The resource limit (0 = unlimited).</param>
    /// <returns>The threshold status string.</returns>
    private static string DetermineThresholdStatus(int percentUsed, long limit)
    {
        if (limit == 0) return "normal"; // Unlimited
        if (percentUsed >= 100) return "exceeded";
        if (percentUsed >= 95) return "critical";
        if (percentUsed >= 80) return "warning";
        return "normal";
    }
}
