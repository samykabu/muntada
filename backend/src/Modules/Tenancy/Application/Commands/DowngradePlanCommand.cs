using MediatR;
using Microsoft.EntityFrameworkCore;
using Muntada.SharedKernel.Application;
using Muntada.Tenancy.Application.Queries;
using Muntada.Tenancy.Application.Services;
using Muntada.Tenancy.Domain.Events;
using Muntada.Tenancy.Domain.Plan;
using Muntada.Tenancy.Infrastructure;

namespace Muntada.Tenancy.Application.Commands;

/// <summary>
/// Command to downgrade a tenant's subscription plan to a lower tier.
/// Supports immediate downgrade or scheduling for the next billing cycle.
/// </summary>
/// <param name="TenantId">The identifier of the tenant to downgrade.</param>
/// <param name="TargetPlanDefinitionId">The identifier of the target (lower-tier) plan definition.</param>
/// <param name="EffectiveDate">"immediate" to downgrade now, or "next-billing-cycle" to schedule for month end.</param>
/// <param name="RequestedBy">The identifier of the user requesting the downgrade.</param>
public sealed record DowngradePlanCommand(
    Guid TenantId,
    Guid TargetPlanDefinitionId,
    string EffectiveDate,
    Guid RequestedBy) : IRequest<PlanDowngradeResult>;

/// <summary>
/// Result returned after a plan downgrade request, including any usage warnings.
/// </summary>
/// <param name="PreviousPlan">The display name of the previous plan.</param>
/// <param name="NewPlan">The display name of the target plan.</param>
/// <param name="EffectiveDate">The UTC date and time when the downgrade takes or will take effect.</param>
/// <param name="IsScheduled">Whether the downgrade is scheduled for a future date rather than immediate.</param>
/// <param name="UsageWarnings">List of resources where current usage exceeds the new plan limits.</param>
/// <param name="NewLimits">The usage limits and feature flags of the target plan.</param>
public sealed record PlanDowngradeResult(
    string PreviousPlan,
    string NewPlan,
    DateTime EffectiveDate,
    bool IsScheduled,
    IReadOnlyList<UsageWarning> UsageWarnings,
    PlanLimitsResult NewLimits);

/// <summary>
/// Represents a warning where current tenant usage exceeds the limits of the target plan.
/// </summary>
/// <param name="Resource">The resource type (e.g. "rooms", "storage", "recording").</param>
/// <param name="CurrentUsage">The current usage value.</param>
/// <param name="NewLimit">The limit imposed by the target plan.</param>
/// <param name="Message">A user-facing warning message.</param>
public sealed record UsageWarning(
    string Resource,
    long CurrentUsage,
    long NewLimit,
    string Message);

/// <summary>
/// Handles <see cref="DowngradePlanCommand"/> by validating the target plan is a lower tier,
/// checking current usage against new limits, and either applying the downgrade immediately
/// or scheduling it for the next billing cycle.
/// Uses optimistic concurrency on <see cref="TenantPlan"/> to handle concurrent changes.
/// </summary>
public sealed class DowngradePlanCommandHandler : IRequestHandler<DowngradePlanCommand, PlanDowngradeResult>
{
    private readonly TenancyDbContext _dbContext;
    private readonly IIntegrationEventPublisher _eventPublisher;
    private readonly IPlanLimitService _planLimitService;

    /// <summary>
    /// Initializes a new instance of <see cref="DowngradePlanCommandHandler"/>.
    /// </summary>
    /// <param name="dbContext">The Tenancy module database context.</param>
    /// <param name="eventPublisher">Publisher for integration events.</param>
    /// <param name="planLimitService">Service for checking tenant usage against plan limits.</param>
    public DowngradePlanCommandHandler(
        TenancyDbContext dbContext,
        IIntegrationEventPublisher eventPublisher,
        IPlanLimitService planLimitService)
    {
        _dbContext = dbContext;
        _eventPublisher = eventPublisher;
        _planLimitService = planLimitService;
    }

    /// <summary>
    /// Handles the downgrade: validates tiers, checks usage limits, applies or schedules
    /// the downgrade, persists changes, and publishes a <see cref="PlanChangedEvent"/>.
    /// </summary>
    /// <param name="request">The downgrade command.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>The downgrade result including any usage warnings.</returns>
    /// <exception cref="FluentValidation.ValidationException">
    /// Thrown when the target plan is not found, is inactive, is not a lower tier,
    /// or the effective date value is invalid.
    /// </exception>
    public async Task<PlanDowngradeResult> Handle(DowngradePlanCommand request, CancellationToken cancellationToken)
    {
        // Validate effective date parameter
        var effectiveDate = request.EffectiveDate?.ToLowerInvariant();
        if (effectiveDate is not "immediate" and not "next-billing-cycle")
        {
            throw new FluentValidation.ValidationException(
                [new FluentValidation.Results.ValidationFailure("EffectiveDate",
                    "EffectiveDate must be 'immediate' or 'next-billing-cycle'.")]);
        }

        // Load target plan definition
        var targetPlan = await _dbContext.PlanDefinitions
            .FirstOrDefaultAsync(p => p.Id == request.TargetPlanDefinitionId, cancellationToken);

        if (targetPlan is null || !targetPlan.IsActive)
        {
            throw new FluentValidation.ValidationException(
                [new FluentValidation.Results.ValidationFailure("TargetPlanDefinitionId",
                    "The specified plan definition was not found or is not active.")]);
        }

        // Load current plan assignment and its definition
        var currentTenantPlan = await _dbContext.TenantPlans
            .FirstOrDefaultAsync(tp => tp.TenantId == request.TenantId && tp.IsCurrent, cancellationToken);

        if (currentTenantPlan is null)
        {
            throw new FluentValidation.ValidationException(
                [new FluentValidation.Results.ValidationFailure("TenantId",
                    "No active plan found for this tenant.")]);
        }

        var currentPlanDef = await _dbContext.PlanDefinitions
            .FirstOrDefaultAsync(p => p.Id == currentTenantPlan.PlanDefinitionId, cancellationToken);

        if (currentPlanDef is null)
        {
            throw new FluentValidation.ValidationException(
                [new FluentValidation.Results.ValidationFailure("TenantId",
                    "Current plan definition not found.")]);
        }

        // Validate target is a lower tier
        if (targetPlan.Tier >= currentPlanDef.Tier)
        {
            throw new FluentValidation.ValidationException(
                [new FluentValidation.Results.ValidationFailure("TargetPlanDefinitionId",
                    $"Target plan tier ({targetPlan.Tier}) must be lower than current tier ({currentPlanDef.Tier}). Use the upgrade endpoint for higher tiers.")]);
        }

        // Check current usage against new plan limits and collect warnings
        var warnings = new List<UsageWarning>();
        var resourceTypes = new[] { "rooms", "participants", "storage", "recording" };

        foreach (var resource in resourceTypes)
        {
            var limitCheck = await _planLimitService.CheckLimitAsync(request.TenantId, resource, cancellationToken);
            var newLimit = GetLimitForResource(targetPlan.Limits, resource);

            if (newLimit > 0 && limitCheck.CurrentUsage > newLimit)
            {
                warnings.Add(new UsageWarning(
                    Resource: resource,
                    CurrentUsage: limitCheck.CurrentUsage,
                    NewLimit: newLimit,
                    Message: $"Current {resource} usage ({limitCheck.CurrentUsage}) exceeds the new plan limit ({newLimit})."));
            }
        }

        var now = DateTime.UtcNow;
        var isScheduled = effectiveDate == "next-billing-cycle";
        DateTime downgradeEffectiveDate;

        if (isScheduled)
        {
            // Schedule downgrade for end of current month (next billing cycle start)
            downgradeEffectiveDate = new DateTime(now.Year, now.Month, DateTime.DaysInMonth(now.Year, now.Month), 23, 59, 59, DateTimeKind.Utc);

            // Store pending downgrade as a plan assignment with a future start date
            var pendingPlan = TenantPlan.AssignFuture(request.TenantId, targetPlan.Id, downgradeEffectiveDate);
            _dbContext.TenantPlans.Add(pendingPlan);
        }
        else
        {
            // Immediate downgrade
            downgradeEffectiveDate = now;
            currentTenantPlan.End();

            var newTenantPlan = TenantPlan.Assign(request.TenantId, targetPlan.Id);
            _dbContext.TenantPlans.Add(newTenantPlan);
        }

        await _dbContext.SaveChangesAsync(cancellationToken);

        // Publish integration event
        await _eventPublisher.PublishAsync(new PlanChangedEvent(
            EventId: Guid.NewGuid(),
            OccurredAt: DateTimeOffset.UtcNow,
            AggregateId: request.TenantId.ToString(),
            AggregateType: nameof(Domain.Tenant.Tenant),
            Version: 1,
            TenantId: request.TenantId,
            PlanDefinitionId: targetPlan.Id,
            PlanName: targetPlan.Name,
            Tier: targetPlan.Tier.ToString()), cancellationToken);

        return new PlanDowngradeResult(
            PreviousPlan: currentPlanDef.Name,
            NewPlan: targetPlan.Name,
            EffectiveDate: downgradeEffectiveDate,
            IsScheduled: isScheduled,
            UsageWarnings: warnings,
            NewLimits: new PlanLimitsResult(
                MaxRoomsPerMonth: targetPlan.Limits.MaxRoomsPerMonth,
                MaxParticipantsPerRoom: targetPlan.Limits.MaxParticipantsPerRoom,
                MaxStorageGB: targetPlan.Limits.MaxStorageGB,
                MaxRecordingHoursPerMonth: targetPlan.Limits.MaxRecordingHoursPerMonth,
                MaxDataRetentionDays: targetPlan.Limits.MaxDataRetentionDays,
                AllowRecording: targetPlan.Limits.AllowRecording,
                AllowGuestAccess: targetPlan.Limits.AllowGuestAccess,
                AllowCustomBranding: targetPlan.Limits.AllowCustomBranding));
    }

    /// <summary>
    /// Returns the numeric limit for a given resource type from the plan limits.
    /// </summary>
    private static long GetLimitForResource(PlanLimits limits, string resourceType)
    {
        return resourceType.ToLowerInvariant() switch
        {
            "rooms" => limits.MaxRoomsPerMonth,
            "participants" => limits.MaxParticipantsPerRoom,
            "storage" => limits.MaxStorageGB,
            "recording" => limits.MaxRecordingHoursPerMonth,
            _ => 0
        };
    }
}
