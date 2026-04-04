namespace Muntada.Tenancy.Domain.Membership;

/// <summary>
/// Strongly-typed identifier for a <see cref="TenantMembership"/> entity.
/// Wraps a <see cref="Guid"/>.
/// </summary>
public readonly record struct TenantMembershipId(Guid Value)
{
    /// <summary>
    /// Creates a new <see cref="TenantMembershipId"/> with a freshly generated <see cref="Guid"/>.
    /// </summary>
    public static TenantMembershipId New() => new(Guid.NewGuid());
}
