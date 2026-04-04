namespace Muntada.Tenancy.Domain.Plan;

/// <summary>
/// Strongly-typed identifier for a <see cref="PlanDefinition"/> entity.
/// Wraps a <see cref="Guid"/>.
/// </summary>
public readonly record struct PlanDefinitionId(Guid Value)
{
    /// <summary>
    /// Creates a new <see cref="PlanDefinitionId"/> with a freshly generated <see cref="Guid"/>.
    /// </summary>
    public static PlanDefinitionId New() => new(Guid.NewGuid());
}
