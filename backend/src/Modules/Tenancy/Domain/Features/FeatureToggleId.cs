using Muntada.SharedKernel.Domain;

namespace Muntada.Tenancy.Domain.Features;

/// <summary>
/// Strongly-typed identifier for a <see cref="FeatureToggle"/> entity.
/// Wraps a <see cref="Guid"/> and produces opaque display strings with prefix <c>ftg_</c>.
/// </summary>
public readonly record struct FeatureToggleId(Guid Value)
{
    /// <summary>
    /// Creates a new <see cref="FeatureToggleId"/> with a freshly generated <see cref="Guid"/>.
    /// </summary>
    public static FeatureToggleId New() => new(Guid.NewGuid());

    /// <summary>
    /// Returns an opaque, URL-safe string representation (e.g. <c>ftg_a7k2jZ9xQpR4b1m</c>).
    /// </summary>
    public override string ToString() => OpaqueIdGenerator.Generate("ftg");
}
