using Muntada.SharedKernel.Domain;

namespace Muntada.Tenancy.Domain.Tenant;

/// <summary>
/// Strongly-typed identifier for a <see cref="Tenant"/> aggregate.
/// Wraps a <see cref="Guid"/> and produces opaque display strings with prefix <c>tnt_</c>.
/// </summary>
public readonly record struct TenantId(Guid Value)
{
    /// <summary>
    /// Creates a new <see cref="TenantId"/> with a freshly generated <see cref="Guid"/>.
    /// </summary>
    public static TenantId New() => new(Guid.NewGuid());

    /// <summary>
    /// Returns an opaque, URL-safe string representation (e.g. <c>tnt_a7k2jZ9xQpR4b1m</c>).
    /// </summary>
    public override string ToString() => OpaqueIdGenerator.Generate("tnt");
}
