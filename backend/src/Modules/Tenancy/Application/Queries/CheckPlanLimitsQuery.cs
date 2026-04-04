using MediatR;
using Muntada.Tenancy.Application.Services;

namespace Muntada.Tenancy.Application.Queries;

/// <summary>
/// Query to check whether a tenant's current plan allows consumption of a specific resource type.
/// </summary>
/// <param name="TenantId">The identifier of the tenant to check limits for.</param>
/// <param name="ResourceType">The resource type to check (e.g. "rooms", "storage", "recording").</param>
public sealed record CheckPlanLimitsQuery(
    Guid TenantId,
    string ResourceType) : IRequest<LimitCheckResult>;

/// <summary>
/// Handles <see cref="CheckPlanLimitsQuery"/> by delegating to <see cref="IPlanLimitService"/>
/// to evaluate the tenant's current plan limits against actual usage.
/// </summary>
public sealed class CheckPlanLimitsQueryHandler : IRequestHandler<CheckPlanLimitsQuery, LimitCheckResult>
{
    private readonly IPlanLimitService _planLimitService;

    /// <summary>
    /// Initializes a new instance of <see cref="CheckPlanLimitsQueryHandler"/>.
    /// </summary>
    /// <param name="planLimitService">The plan limit checking service.</param>
    public CheckPlanLimitsQueryHandler(IPlanLimitService planLimitService)
    {
        _planLimitService = planLimitService;
    }

    /// <summary>
    /// Handles the query by invoking the plan limit service.
    /// </summary>
    /// <param name="request">The plan limits check query.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>A <see cref="LimitCheckResult"/> indicating whether the action is allowed.</returns>
    public async Task<LimitCheckResult> Handle(CheckPlanLimitsQuery request, CancellationToken cancellationToken)
    {
        return await _planLimitService.CheckLimitAsync(request.TenantId, request.ResourceType, cancellationToken);
    }
}
