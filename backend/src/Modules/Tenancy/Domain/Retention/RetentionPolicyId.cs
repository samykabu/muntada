namespace Muntada.Tenancy.Domain.Retention;

/// <summary>
/// Strongly-typed identifier for a <see cref="RetentionPolicy"/> entity.
/// Wraps a <see cref="Guid"/>.
/// </summary>
public readonly record struct RetentionPolicyId(Guid Value)
{
    /// <summary>
    /// Creates a new <see cref="RetentionPolicyId"/> with a freshly generated <see cref="Guid"/>.
    /// </summary>
    public static RetentionPolicyId New() => new(Guid.NewGuid());
}
