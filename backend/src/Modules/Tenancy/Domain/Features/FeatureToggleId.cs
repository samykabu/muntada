namespace Muntada.Tenancy.Domain.Features;

/// <summary>
/// Strongly-typed identifier for a <see cref="FeatureToggle"/> entity.
/// Wraps a <see cref="Guid"/>.
/// </summary>
public readonly record struct FeatureToggleId(Guid Value)
{
    /// <summary>
    /// Creates a new <see cref="FeatureToggleId"/> with a freshly generated <see cref="Guid"/>.
    /// </summary>
    public static FeatureToggleId New() => new(Guid.NewGuid());
}
