using Muntada.SharedKernel.Domain;

namespace Muntada.Tenancy.Domain.Retention;

/// <summary>
/// Strongly-typed identifier for a <see cref="RetentionPolicy"/> entity.
/// Wraps a <see cref="Guid"/> and produces opaque display strings with prefix <c>rtn_</c>.
/// </summary>
public readonly record struct RetentionPolicyId(Guid Value)
{
    /// <summary>
    /// Creates a new <see cref="RetentionPolicyId"/> with a freshly generated <see cref="Guid"/>.
    /// </summary>
    public static RetentionPolicyId New() => new(Guid.NewGuid());

    /// <summary>
    /// Returns an opaque, URL-safe string representation (e.g. <c>rtn_a7k2jZ9xQpR4b1m</c>).
    /// </summary>
    public override string ToString() => OpaqueIdGenerator.Generate("rtn");
}
