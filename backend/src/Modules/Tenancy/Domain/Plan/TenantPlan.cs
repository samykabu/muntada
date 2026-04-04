using Muntada.SharedKernel.Domain;

namespace Muntada.Tenancy.Domain.Plan;

/// <summary>
/// Represents the assignment of a <see cref="PlanDefinition"/> to a specific tenant,
/// tracking the subscription period and whether it is the current active plan.
/// </summary>
public class TenantPlan : Entity<Guid>
{
    /// <summary>
    /// Gets the identifier of the tenant this plan is assigned to.
    /// </summary>
    public Guid TenantId { get; private set; }

    /// <summary>
    /// Gets the identifier of the associated <see cref="PlanDefinition"/>.
    /// </summary>
    public Guid PlanDefinitionId { get; private set; }

    /// <summary>
    /// Gets the UTC date and time when this plan assignment started.
    /// </summary>
    public DateTime StartDate { get; private set; }

    /// <summary>
    /// Gets the UTC date and time when this plan assignment ended, or <c>null</c> if still active.
    /// </summary>
    public DateTime? EndDate { get; private set; }

    /// <summary>
    /// Gets a value indicating whether this is the tenant's current active plan.
    /// </summary>
    public bool IsCurrent { get; private set; }

    /// <summary>
    /// Private constructor for EF Core materialization.
    /// </summary>
    private TenantPlan() { }

    /// <summary>
    /// Assigns a plan definition to a tenant, marking it as the current active plan.
    /// </summary>
    /// <param name="tenantId">The identifier of the tenant.</param>
    /// <param name="planDefinitionId">The identifier of the plan definition to assign.</param>
    /// <returns>A new <see cref="TenantPlan"/> instance marked as current.</returns>
    public static TenantPlan Assign(Guid tenantId, Guid planDefinitionId)
    {
        return new TenantPlan
        {
            Id = Guid.NewGuid(),
            TenantId = tenantId,
            PlanDefinitionId = planDefinitionId,
            StartDate = DateTime.UtcNow,
            EndDate = null,
            IsCurrent = true
        };
    }

    /// <summary>
    /// Ends this plan assignment by setting the end date to now and marking it as no longer current.
    /// </summary>
    /// <exception cref="InvalidOperationException">Thrown when the plan has already ended.</exception>
    public void End()
    {
        if (EndDate.HasValue)
            throw new InvalidOperationException("This plan assignment has already ended.");

        EndDate = DateTime.UtcNow;
        IsCurrent = false;
    }
}
