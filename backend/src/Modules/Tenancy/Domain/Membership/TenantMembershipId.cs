using Muntada.SharedKernel.Domain;

namespace Muntada.Tenancy.Domain.Membership;

/// <summary>
/// Strongly-typed identifier for a <see cref="TenantMembership"/> entity.
/// Wraps a <see cref="Guid"/> and produces opaque display strings with prefix <c>mbr_</c>.
/// </summary>
public readonly record struct TenantMembershipId(Guid Value)
{
    /// <summary>
    /// Creates a new <see cref="TenantMembershipId"/> with a freshly generated <see cref="Guid"/>.
    /// </summary>
    public static TenantMembershipId New() => new(Guid.NewGuid());

    /// <summary>
    /// Returns an opaque, URL-safe string representation (e.g. <c>mbr_a7k2jZ9xQpR4b1m</c>).
    /// </summary>
    public override string ToString() => OpaqueIdGenerator.Generate("mbr");
}
