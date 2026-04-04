using MediatR;
using Microsoft.EntityFrameworkCore;
using Muntada.SharedKernel.Application;
using Muntada.Tenancy.Domain.Events;
using Muntada.Tenancy.Domain.Membership;
using Muntada.Tenancy.Domain.Plan;
using Muntada.Tenancy.Domain.Retention;
using Muntada.Tenancy.Domain.Tenant;
using Muntada.Tenancy.Infrastructure;

namespace Muntada.Tenancy.Application.Commands;

/// <summary>
/// Command to create a new tenant (organization/workspace) with a trial plan.
/// </summary>
/// <param name="Name">The display name of the tenant (3-100 characters).</param>
/// <param name="Slug">Optional URL-safe slug; auto-generated from <paramref name="Name"/> if omitted.</param>
/// <param name="Industry">Optional industry classification for the tenant.</param>
/// <param name="TeamSize">Optional team size descriptor.</param>
/// <param name="CreatedBy">The identifier of the user creating the tenant.</param>
public sealed record CreateTenantCommand(
    string Name,
    string? Slug,
    string? Industry,
    string? TeamSize,
    Guid CreatedBy) : IRequest<CreateTenantResult>;

/// <summary>
/// Result returned after successful tenant creation.
/// </summary>
/// <param name="TenantId">The unique identifier of the newly created tenant.</param>
/// <param name="Name">The display name of the tenant.</param>
/// <param name="Slug">The URL-safe slug assigned to the tenant.</param>
/// <param name="Status">The initial lifecycle status of the tenant.</param>
/// <param name="BillingStatus">The initial billing/subscription status.</param>
/// <param name="TrialEndsAt">The UTC date and time when the trial period expires.</param>
public sealed record CreateTenantResult(
    Guid TenantId,
    string Name,
    string Slug,
    string Status,
    string BillingStatus,
    DateTime? TrialEndsAt);

/// <summary>
/// Handles <see cref="CreateTenantCommand"/> by creating a new tenant aggregate,
/// assigning a trial plan, creating the owner membership, and publishing a
/// <see cref="TenantCreatedEvent"/> integration event.
/// </summary>
public sealed class CreateTenantCommandHandler : IRequestHandler<CreateTenantCommand, CreateTenantResult>
{
    private readonly TenancyDbContext _dbContext;
    private readonly IIntegrationEventPublisher _eventPublisher;

    /// <summary>
    /// Initializes a new instance of <see cref="CreateTenantCommandHandler"/>.
    /// </summary>
    /// <param name="dbContext">The Tenancy module database context.</param>
    /// <param name="eventPublisher">Publisher for integration events.</param>
    public CreateTenantCommandHandler(
        TenancyDbContext dbContext,
        IIntegrationEventPublisher eventPublisher)
    {
        _dbContext = dbContext;
        _eventPublisher = eventPublisher;
    }

    /// <summary>
    /// Handles tenant creation: generates slug, validates uniqueness, creates tenant aggregate,
    /// assigns trial plan, creates owner membership and default retention policy,
    /// persists all entities, and publishes the <see cref="TenantCreatedEvent"/>.
    /// </summary>
    /// <param name="request">The tenant creation command.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>The creation result containing the new tenant's details.</returns>
    /// <exception cref="FluentValidation.ValidationException">
    /// Thrown when the slug is already in use by another tenant.
    /// </exception>
    public async Task<CreateTenantResult> Handle(CreateTenantCommand request, CancellationToken cancellationToken)
    {
        // Generate slug from name if not provided
        var slug = string.IsNullOrWhiteSpace(request.Slug)
            ? TenantSlug.GenerateFromName(request.Name)
            : TenantSlug.Create(request.Slug);

        // Check slug uniqueness against DB
        var slugExists = await _dbContext.Tenants
            .AnyAsync(t => t.Slug.Value == slug.Value, cancellationToken);

        if (slugExists)
        {
            throw new FluentValidation.ValidationException(
                [new FluentValidation.Results.ValidationFailure("Slug", $"The slug '{slug.Value}' is already in use.")]);
        }

        // Create tenant aggregate
        var tenant = Domain.Tenant.Tenant.Create(request.Name, slug, request.CreatedBy);

        // Create owner membership
        var membership = TenantMembership.CreateForOwner(tenant.Id, request.CreatedBy);

        // Find the Trial PlanDefinition and assign it
        var trialPlan = await _dbContext.PlanDefinitions
            .FirstAsync(p => p.Tier == PlanTier.Trial && p.IsActive, cancellationToken);
        var tenantPlan = TenantPlan.Assign(tenant.Id, trialPlan.Id);

        // Create default retention policy
        var retentionPolicy = RetentionPolicy.CreateDefault(tenant.Id);

        // Persist all entities in a single transaction
        _dbContext.Tenants.Add(tenant);
        _dbContext.TenantMemberships.Add(membership);
        _dbContext.TenantPlans.Add(tenantPlan);
        _dbContext.RetentionPolicies.Add(retentionPolicy);
        await _dbContext.SaveChangesAsync(cancellationToken);

        // Publish integration event
        var @event = new TenantCreatedEvent(
            EventId: Guid.NewGuid(),
            OccurredAt: DateTimeOffset.UtcNow,
            AggregateId: tenant.Id.ToString(),
            AggregateType: nameof(Domain.Tenant.Tenant),
            Version: 1,
            TenantId: tenant.Id,
            TenantName: tenant.Name,
            Slug: slug.Value,
            CreatedBy: request.CreatedBy);
        await _eventPublisher.PublishAsync(@event, cancellationToken);

        return new CreateTenantResult(
            TenantId: tenant.Id,
            Name: tenant.Name,
            Slug: slug.Value,
            Status: tenant.Status.ToString(),
            BillingStatus: tenant.BillingStatus.ToString(),
            TrialEndsAt: tenant.TrialEndsAt);
    }
}
