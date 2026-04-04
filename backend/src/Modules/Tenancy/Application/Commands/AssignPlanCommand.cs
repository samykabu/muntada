using MediatR;
using Microsoft.EntityFrameworkCore;
using Muntada.SharedKernel.Application;
using Muntada.Tenancy.Domain.Events;
using Muntada.Tenancy.Domain.Plan;
using Muntada.Tenancy.Infrastructure;

namespace Muntada.Tenancy.Application.Commands;

/// <summary>
/// Command to assign a new plan to a tenant, replacing the current active plan.
/// </summary>
/// <param name="TenantId">The identifier of the tenant to assign the plan to.</param>
/// <param name="PlanDefinitionId">The identifier of the plan definition to assign.</param>
/// <param name="RequestedBy">The identifier of the user requesting the plan change.</param>
public sealed record AssignPlanCommand(
    Guid TenantId,
    Guid PlanDefinitionId,
    Guid RequestedBy) : IRequest<AssignPlanResult>;

/// <summary>
/// Result returned after a successful plan assignment.
/// </summary>
/// <param name="TenantPlanId">The unique identifier of the new tenant plan assignment.</param>
/// <param name="PlanName">The display name of the assigned plan.</param>
/// <param name="Tier">The pricing tier of the assigned plan.</param>
public sealed record AssignPlanResult(
    Guid TenantPlanId,
    string PlanName,
    string Tier);

/// <summary>
/// Handles <see cref="AssignPlanCommand"/> by ending the current plan (if any),
/// creating a new <see cref="TenantPlan"/> assignment, and publishing a
/// <see cref="PlanChangedEvent"/> integration event.
/// </summary>
public sealed class AssignPlanCommandHandler : IRequestHandler<AssignPlanCommand, AssignPlanResult>
{
    private readonly TenancyDbContext _dbContext;
    private readonly IIntegrationEventPublisher _eventPublisher;

    /// <summary>
    /// Initializes a new instance of <see cref="AssignPlanCommandHandler"/>.
    /// </summary>
    /// <param name="dbContext">The Tenancy module database context.</param>
    /// <param name="eventPublisher">Publisher for integration events.</param>
    public AssignPlanCommandHandler(
        TenancyDbContext dbContext,
        IIntegrationEventPublisher eventPublisher)
    {
        _dbContext = dbContext;
        _eventPublisher = eventPublisher;
    }

    /// <summary>
    /// Handles plan assignment: verifies the plan definition is active, ends the current plan,
    /// creates a new assignment, persists changes, and publishes a <see cref="PlanChangedEvent"/>.
    /// </summary>
    /// <param name="request">The plan assignment command.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>The assignment result containing the new plan details.</returns>
    /// <exception cref="FluentValidation.ValidationException">
    /// Thrown when the plan definition is not found or is inactive.
    /// </exception>
    public async Task<AssignPlanResult> Handle(AssignPlanCommand request, CancellationToken cancellationToken)
    {
        // Verify plan definition exists and is active
        var planDefinition = await _dbContext.PlanDefinitions
            .FirstOrDefaultAsync(p => p.Id == request.PlanDefinitionId, cancellationToken);

        if (planDefinition is null || !planDefinition.IsActive)
        {
            throw new FluentValidation.ValidationException(
                [new FluentValidation.Results.ValidationFailure("PlanDefinitionId",
                    "The specified plan definition was not found or is not active.")]);
        }

        // End current plan if one exists
        var currentPlan = await _dbContext.TenantPlans
            .FirstOrDefaultAsync(tp => tp.TenantId == request.TenantId && tp.IsCurrent, cancellationToken);

        currentPlan?.End();

        // Create new plan assignment
        var newTenantPlan = TenantPlan.Assign(request.TenantId, planDefinition.Id);
        _dbContext.TenantPlans.Add(newTenantPlan);

        await _dbContext.SaveChangesAsync(cancellationToken);

        // Publish integration event
        var @event = new PlanChangedEvent(
            EventId: Guid.NewGuid(),
            OccurredAt: DateTimeOffset.UtcNow,
            AggregateId: request.TenantId.ToString(),
            AggregateType: nameof(Domain.Tenant.Tenant),
            Version: 1,
            TenantId: request.TenantId,
            PlanDefinitionId: planDefinition.Id,
            PlanName: planDefinition.Name,
            Tier: planDefinition.Tier.ToString());
        await _eventPublisher.PublishAsync(@event, cancellationToken);

        return new AssignPlanResult(
            TenantPlanId: newTenantPlan.Id,
            PlanName: planDefinition.Name,
            Tier: planDefinition.Tier.ToString());
    }
}
