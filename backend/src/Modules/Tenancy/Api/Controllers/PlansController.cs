using MediatR;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Muntada.Tenancy.Api.Dtos;
using Muntada.Tenancy.Application.Commands;
using Muntada.Tenancy.Application.Queries;
using Muntada.Tenancy.Infrastructure;

namespace Muntada.Tenancy.Api.Controllers;

/// <summary>
/// Handles plan-related operations: retrieving a tenant's current plan
/// and listing all available plan definitions.
/// </summary>
[ApiController]
[Route("api/v1")]
public sealed class PlansController : ControllerBase
{
    private readonly ISender _sender;
    private readonly TenancyDbContext _dbContext;

    /// <summary>
    /// Initializes a new instance of <see cref="PlansController"/>.
    /// </summary>
    /// <param name="sender">MediatR sender for dispatching queries.</param>
    /// <param name="dbContext">The Tenancy module database context.</param>
    public PlansController(ISender sender, TenancyDbContext dbContext)
    {
        _sender = sender;
        _dbContext = dbContext;
    }

    /// <summary>
    /// Retrieves the current plan and its limits for a specific tenant.
    /// </summary>
    /// <param name="tenantId">The identifier of the tenant.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>200 OK with the current plan details, or 404 Not Found.</returns>
    [HttpGet("tenants/{tenantId:guid}/plan")]
    [ProducesResponseType(typeof(PlanResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetTenantPlan(
        Guid tenantId,
        CancellationToken cancellationToken)
    {
        var result = await _sender.Send(new GetTenantPlanQuery(tenantId), cancellationToken);

        if (result is null)
            return NotFound();

        var response = new PlanResponse(
            TenantPlanId: result.TenantPlanId,
            PlanDefinitionId: result.PlanDefinitionId,
            PlanName: result.PlanName,
            Tier: result.Tier,
            MonthlyPriceUsd: result.MonthlyPriceUsd,
            StartDate: result.StartDate,
            Limits: new PlanLimitsResponse(
                MaxRoomsPerMonth: result.Limits.MaxRoomsPerMonth,
                MaxParticipantsPerRoom: result.Limits.MaxParticipantsPerRoom,
                MaxStorageGB: result.Limits.MaxStorageGB,
                MaxRecordingHoursPerMonth: result.Limits.MaxRecordingHoursPerMonth,
                MaxDataRetentionDays: result.Limits.MaxDataRetentionDays,
                AllowRecording: result.Limits.AllowRecording,
                AllowGuestAccess: result.Limits.AllowGuestAccess,
                AllowCustomBranding: result.Limits.AllowCustomBranding));

        return Ok(response);
    }

    /// <summary>
    /// Lists all active plan definitions available for subscription.
    /// </summary>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>200 OK with the list of available plan definitions.</returns>
    [HttpGet("plans/available")]
    [ProducesResponseType(typeof(IReadOnlyList<PlanDefinitionResponse>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetAvailablePlans(CancellationToken cancellationToken)
    {
        var plans = await _dbContext.PlanDefinitions
            .AsNoTracking()
            .Where(p => p.IsActive)
            .OrderBy(p => p.MonthlyPriceUsd)
            .Select(p => new PlanDefinitionResponse(
                Id: p.Id,
                Name: p.Name,
                Tier: p.Tier.ToString(),
                MonthlyPriceUsd: p.MonthlyPriceUsd,
                Limits: new PlanLimitsResponse(
                    MaxRoomsPerMonth: p.Limits.MaxRoomsPerMonth,
                    MaxParticipantsPerRoom: p.Limits.MaxParticipantsPerRoom,
                    MaxStorageGB: p.Limits.MaxStorageGB,
                    MaxRecordingHoursPerMonth: p.Limits.MaxRecordingHoursPerMonth,
                    MaxDataRetentionDays: p.Limits.MaxDataRetentionDays,
                    AllowRecording: p.Limits.AllowRecording,
                    AllowGuestAccess: p.Limits.AllowGuestAccess,
                    AllowCustomBranding: p.Limits.AllowCustomBranding)))
            .ToListAsync(cancellationToken);

        return Ok(plans);
    }

    /// <summary>
    /// Upgrades a tenant's subscription plan to a higher tier.
    /// Calculates pro-rated charges for the remainder of the billing cycle.
    /// </summary>
    /// <param name="tenantId">The identifier of the tenant.</param>
    /// <param name="request">The upgrade request containing the target plan definition identifier.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>200 OK with the upgrade result including pro-rated charge, or 400 Bad Request.</returns>
    [HttpPost("tenants/{tenantId:guid}/plan/upgrade")]
    [ProducesResponseType(typeof(PlanChangeResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> UpgradePlan(
        Guid tenantId,
        [FromBody] UpgradePlanRequest request,
        CancellationToken cancellationToken)
    {
        var command = new UpgradePlanCommand(
            TenantId: tenantId,
            TargetPlanDefinitionId: request.TargetPlanDefinitionId,
            RequestedBy: Guid.Empty); // TODO: Extract from authenticated user context

        var result = await _sender.Send(command, cancellationToken);

        return Ok(new PlanChangeResponse(
            PreviousPlan: result.PreviousPlan,
            NewPlan: result.NewPlan,
            EffectiveDate: result.EffectiveDate,
            ProratedChargeUsd: result.ProratedChargeUsd,
            NewLimits: new PlanLimitsResponse(
                MaxRoomsPerMonth: result.NewLimits.MaxRoomsPerMonth,
                MaxParticipantsPerRoom: result.NewLimits.MaxParticipantsPerRoom,
                MaxStorageGB: result.NewLimits.MaxStorageGB,
                MaxRecordingHoursPerMonth: result.NewLimits.MaxRecordingHoursPerMonth,
                MaxDataRetentionDays: result.NewLimits.MaxDataRetentionDays,
                AllowRecording: result.NewLimits.AllowRecording,
                AllowGuestAccess: result.NewLimits.AllowGuestAccess,
                AllowCustomBranding: result.NewLimits.AllowCustomBranding)));
    }

    /// <summary>
    /// Downgrades a tenant's subscription plan to a lower tier.
    /// Supports immediate downgrade or scheduling for the next billing cycle.
    /// </summary>
    /// <param name="tenantId">The identifier of the tenant.</param>
    /// <param name="request">The downgrade request containing the target plan and effective date.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>200 OK with the downgrade result including usage warnings, or 400 Bad Request.</returns>
    [HttpPost("tenants/{tenantId:guid}/plan/downgrade")]
    [ProducesResponseType(typeof(PlanDowngradeResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> DowngradePlan(
        Guid tenantId,
        [FromBody] DowngradePlanRequest request,
        CancellationToken cancellationToken)
    {
        var command = new DowngradePlanCommand(
            TenantId: tenantId,
            TargetPlanDefinitionId: request.TargetPlanDefinitionId,
            EffectiveDate: request.EffectiveDate,
            RequestedBy: Guid.Empty); // TODO: Extract from authenticated user context

        var result = await _sender.Send(command, cancellationToken);

        return Ok(new PlanDowngradeResponse(
            PreviousPlan: result.PreviousPlan,
            NewPlan: result.NewPlan,
            EffectiveDate: result.EffectiveDate,
            IsScheduled: result.IsScheduled,
            UsageWarnings: result.UsageWarnings.Select(w => new UsageWarningResponse(
                Resource: w.Resource,
                CurrentUsage: w.CurrentUsage,
                NewLimit: w.NewLimit,
                Message: w.Message)).ToList(),
            NewLimits: new PlanLimitsResponse(
                MaxRoomsPerMonth: result.NewLimits.MaxRoomsPerMonth,
                MaxParticipantsPerRoom: result.NewLimits.MaxParticipantsPerRoom,
                MaxStorageGB: result.NewLimits.MaxStorageGB,
                MaxRecordingHoursPerMonth: result.NewLimits.MaxRecordingHoursPerMonth,
                MaxDataRetentionDays: result.NewLimits.MaxDataRetentionDays,
                AllowRecording: result.NewLimits.AllowRecording,
                AllowGuestAccess: result.NewLimits.AllowGuestAccess,
                AllowCustomBranding: result.NewLimits.AllowCustomBranding)));
    }
}
