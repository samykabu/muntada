namespace Muntada.Tenancy.Domain.Membership;

/// <summary>
/// Represents the role a user holds within a tenant.
/// </summary>
public enum TenantRole
{
    /// <summary>
    /// The tenant owner with full administrative privileges, including billing and deletion.
    /// </summary>
    Owner = 0,

    /// <summary>
    /// An administrator who can manage tenant settings and members.
    /// </summary>
    Admin = 1,

    /// <summary>
    /// A regular member with standard access within the tenant.
    /// </summary>
    Member = 2
}
