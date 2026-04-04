using Muntada.SharedKernel.Domain;

namespace Muntada.Tenancy.Domain.Plan;

/// <summary>
/// Strongly-typed identifier for a <see cref="TenantPlan"/> entity.
/// Wraps a <see cref="Guid"/> and produces opaque display strings with prefix <c>pln_</c>.
/// </summary>
public readonly record struct TenantPlanId(Guid Value)
{
    /// <summary>
    /// Creates a new <see cref="TenantPlanId"/> with a freshly generated <see cref="Guid"/>.
    /// </summary>
    public static TenantPlanId New() => new(Guid.NewGuid());

    /// <summary>
    /// Returns an opaque, URL-safe string representation (e.g. <c>pln_a7k2jZ9xQpR4b1m</c>).
    /// </summary>
    public override string ToString() => OpaqueIdGenerator.Generate("pln");
}
