namespace Muntada.Tenancy.Domain.Tenant;

/// <summary>
/// Strongly-typed identifier for a <see cref="Tenant"/> aggregate.
/// Wraps a <see cref="Guid"/>.
/// </summary>
public readonly record struct TenantId(Guid Value)
{
    /// <summary>
    /// Creates a new <see cref="TenantId"/> with a freshly generated <see cref="Guid"/>.
    /// </summary>
    public static TenantId New() => new(Guid.NewGuid());
}
