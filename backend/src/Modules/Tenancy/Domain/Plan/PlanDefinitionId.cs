using Muntada.SharedKernel.Domain;

namespace Muntada.Tenancy.Domain.Plan;

/// <summary>
/// Strongly-typed identifier for a <see cref="PlanDefinition"/> entity.
/// Wraps a <see cref="Guid"/> and produces opaque display strings with prefix <c>pdef_</c>.
/// </summary>
public readonly record struct PlanDefinitionId(Guid Value)
{
    /// <summary>
    /// Creates a new <see cref="PlanDefinitionId"/> with a freshly generated <see cref="Guid"/>.
    /// </summary>
    public static PlanDefinitionId New() => new(Guid.NewGuid());

    /// <summary>
    /// Returns an opaque, URL-safe string representation (e.g. <c>pdef_a7k2jZ9xQpR4b1m</c>).
    /// </summary>
    public override string ToString() => OpaqueIdGenerator.Generate("pdef");
}
