namespace Muntada.Tenancy.Domain.Plan;

/// <summary>
/// Strongly-typed identifier for a <see cref="TenantPlan"/> entity.
/// Wraps a <see cref="Guid"/>.
/// </summary>
public readonly record struct TenantPlanId(Guid Value)
{
    /// <summary>
    /// Creates a new <see cref="TenantPlanId"/> with a freshly generated <see cref="Guid"/>.
    /// </summary>
    public static TenantPlanId New() => new(Guid.NewGuid());
}
