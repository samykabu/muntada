using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Muntada.SharedKernel.Application;
using Muntada.Tenancy.Domain.Events;
using Muntada.Tenancy.Domain.Plan;
using Muntada.Tenancy.Domain.Tenant;
using Muntada.Tenancy.Infrastructure;

namespace Muntada.Tenancy.Application.BackgroundJobs;

/// <summary>
/// Daily background job that auto-downgrades expired trial tenants to the Free tier.
/// Data is preserved; only resource limits are reduced to Free plan levels.
/// </summary>
public class TrialExpirationJob
{
    private readonly TenancyDbContext _dbContext;
    private readonly IIntegrationEventPublisher _eventPublisher;
    private readonly ILogger<TrialExpirationJob> _logger;

    /// <summary>Initializes a new instance of the <see cref="TrialExpirationJob"/>.</summary>
    public TrialExpirationJob(
        TenancyDbContext dbContext,
        IIntegrationEventPublisher eventPublisher,
        ILogger<TrialExpirationJob> logger)
    {
        _dbContext = dbContext;
        _eventPublisher = eventPublisher;
        _logger = logger;
    }

    /// <summary>
    /// Processes all expired trial tenants by downgrading them to the Free plan.
    /// </summary>
    public async Task ExecuteAsync(CancellationToken cancellationToken = default)
    {
        var expiredTrialTenants = await _dbContext.Tenants
            .Where(t => t.BillingStatus == BillingStatus.Trial && t.TrialEndsAt < DateTime.UtcNow)
            .ToListAsync(cancellationToken);

        if (expiredTrialTenants.Count == 0)
        {
            _logger.LogInformation("No expired trial tenants found");
            return;
        }

        var freePlan = await _dbContext.PlanDefinitions
            .FirstOrDefaultAsync(p => p.Tier == PlanTier.Free && p.IsActive, cancellationToken)
            ?? throw new InvalidOperationException("Free plan definition not found in database");

        foreach (var tenant in expiredTrialTenants)
        {
            tenant.EndTrial(BillingStatus.Active);

            var currentPlan = await _dbContext.TenantPlans
                .FirstOrDefaultAsync(p => p.TenantId == tenant.Id && p.IsCurrent, cancellationToken);

            currentPlan?.End();

            var newPlan = TenantPlan.Assign(tenant.Id, freePlan.Id);
            _dbContext.TenantPlans.Add(newPlan);

            await _eventPublisher.PublishAsync(new PlanChangedEvent(
                Guid.NewGuid(), DateTimeOffset.UtcNow,
                tenant.Id.ToString(), "Tenant", 1,
                tenant.Id, freePlan.Id, freePlan.Name, freePlan.Tier.ToString()),
                cancellationToken);

            _logger.LogInformation(
                "Trial expired for tenant {TenantId} ({TenantName}). Downgraded to Free plan",
                tenant.Id, tenant.Name);
        }

        await _dbContext.SaveChangesAsync(cancellationToken);

        _logger.LogInformation("Processed {Count} expired trial tenants", expiredTrialTenants.Count);
    }
}
