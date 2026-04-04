using MediatR;
using Microsoft.EntityFrameworkCore;
using Muntada.SharedKernel.Application;
using Muntada.Tenancy.Application.Queries;
using Muntada.Tenancy.Domain.Events;
using Muntada.Tenancy.Domain.Plan;
using Muntada.Tenancy.Infrastructure;

namespace Muntada.Tenancy.Application.Commands;

/// <summary>
/// Command to upgrade a tenant's subscription plan to a higher tier.
/// Calculates pro-rated charges for the remainder of the billing cycle.
/// </summary>
/// <param name="TenantId">The identifier of the tenant to upgrade.</param>
/// <param name="TargetPlanDefinitionId">The identifier of the target (higher-tier) plan definition.</param>
/// <param name="RequestedBy">The identifier of the user requesting the upgrade.</param>
public sealed record UpgradePlanCommand(
    Guid TenantId,
    Guid TargetPlanDefinitionId,
    Guid RequestedBy) : IRequest<PlanChangeResult>;

/// <summary>
/// Result returned after a successful plan upgrade, including pro-rated charge details.
/// </summary>
/// <param name="PreviousPlan">The display name of the previous plan.</param>
/// <param name="NewPlan">The display name of the new plan.</param>
/// <param name="EffectiveDate">The UTC date and time when the upgrade took effect.</param>
/// <param name="ProratedChargeUsd">The pro-rated charge in US dollars for the remainder of the billing cycle.</param>
/// <param name="NewLimits">The usage limits and feature flags of the new plan.</param>
public sealed record PlanChangeResult(
    string PreviousPlan,
    string NewPlan,
    DateTime EffectiveDate,
    decimal ProratedChargeUsd,
    PlanLimitsResult NewLimits);

/// <summary>
/// Handles <see cref="UpgradePlanCommand"/> by validating the target plan is a higher tier,
/// calculating pro-rated charges, ending the current plan, assigning the new plan,
/// and publishing a <see cref="PlanChangedEvent"/>.
/// </summary>
public sealed class UpgradePlanCommandHandler : IRequestHandler<UpgradePlanCommand, PlanChangeResult>
{
    private readonly TenancyDbContext _dbContext;
    private readonly IIntegrationEventPublisher _eventPublisher;

    /// <summary>
    /// Initializes a new instance of <see cref="UpgradePlanCommandHandler"/>.
    /// </summary>
    /// <param name="dbContext">The Tenancy module database context.</param>
    /// <param name="eventPublisher">Publisher for integration events.</param>
    public UpgradePlanCommandHandler(
        TenancyDbContext dbContext,
        IIntegrationEventPublisher eventPublisher)
    {
        _dbContext = dbContext;
        _eventPublisher = eventPublisher;
    }

    /// <summary>
    /// Handles the upgrade: validates tiers, calculates pro-rated charge, swaps plans,
    /// persists changes, and publishes a <see cref="PlanChangedEvent"/>.
    /// </summary>
    /// <param name="request">The upgrade command.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>The upgrade result including pro-rated charge and new plan limits.</returns>
    /// <exception cref="FluentValidation.ValidationException">
    /// Thrown when the target plan is not found, is inactive, or is not a higher tier.
    /// </exception>
    public async Task<PlanChangeResult> Handle(UpgradePlanCommand request, CancellationToken cancellationToken)
    {
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

        // Validate target is a higher tier
        if (targetPlan.Tier <= currentPlanDef.Tier)
        {
            throw new FluentValidation.ValidationException(
                [new FluentValidation.Results.ValidationFailure("TargetPlanDefinitionId",
                    $"Target plan tier ({targetPlan.Tier}) must be higher than current tier ({currentPlanDef.Tier}). Use the downgrade endpoint for lower tiers.")]);
        }

        // Calculate pro-rated charge
        var now = DateTime.UtcNow;
        var endOfMonth = new DateTime(now.Year, now.Month, DateTime.DaysInMonth(now.Year, now.Month), 23, 59, 59, DateTimeKind.Utc);
        var daysInMonth = DateTime.DaysInMonth(now.Year, now.Month);
        var daysRemaining = (endOfMonth.Date - now.Date).Days;

        var oldDailyRate = currentPlanDef.MonthlyPriceUsd / daysInMonth;
        var newDailyRate = targetPlan.MonthlyPriceUsd / daysInMonth;
        var proratedCharge = Math.Max(0, daysRemaining * (newDailyRate - oldDailyRate));
        proratedCharge = Math.Round(proratedCharge, 2);

        // End current plan and assign new one
        currentTenantPlan.End();

        var newTenantPlan = TenantPlan.Assign(request.TenantId, targetPlan.Id);
        _dbContext.TenantPlans.Add(newTenantPlan);

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

        return new PlanChangeResult(
            PreviousPlan: currentPlanDef.Name,
            NewPlan: targetPlan.Name,
            EffectiveDate: now,
            ProratedChargeUsd: proratedCharge,
            NewLimits: new Queries.PlanLimitsResult(
                MaxRoomsPerMonth: targetPlan.Limits.MaxRoomsPerMonth,
                MaxParticipantsPerRoom: targetPlan.Limits.MaxParticipantsPerRoom,
                MaxStorageGB: targetPlan.Limits.MaxStorageGB,
                MaxRecordingHoursPerMonth: targetPlan.Limits.MaxRecordingHoursPerMonth,
                MaxDataRetentionDays: targetPlan.Limits.MaxDataRetentionDays,
                AllowRecording: targetPlan.Limits.AllowRecording,
                AllowGuestAccess: targetPlan.Limits.AllowGuestAccess,
                AllowCustomBranding: targetPlan.Limits.AllowCustomBranding));
    }
}
