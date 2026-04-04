using Muntada.SharedKernel.Domain;

namespace Muntada.Tenancy.Domain.Membership;

/// <summary>
/// Represents a user's membership within a tenant, including their role and status.
/// Supports both direct member creation (for owners) and invite-based membership.
/// </summary>
public class TenantMembership : Entity<Guid>
{
    /// <summary>
    /// Gets the identifier of the tenant this membership belongs to.
    /// </summary>
    public Guid TenantId { get; private set; }

    /// <summary>
    /// Gets the identifier of the user, or <c>null</c> if the invite has not yet been accepted.
    /// </summary>
    public Guid? UserId { get; private set; }

    /// <summary>
    /// Gets the email address of the invited user, or <c>null</c> if membership was created directly.
    /// </summary>
    public string? InvitedEmail { get; private set; }

    /// <summary>
    /// Gets the role the user holds within the tenant.
    /// </summary>
    public TenantRole Role { get; private set; }

    /// <summary>
    /// Gets the current status of this membership.
    /// </summary>
    public TenantMembershipStatus Status { get; private set; }

    /// <summary>
    /// Gets the UTC date and time when the user joined the tenant, or <c>null</c> if not yet joined.
    /// </summary>
    public DateTime? JoinedAt { get; private set; }

    /// <summary>
    /// Gets the UTC date and time when the invitation was sent, or <c>null</c> if not an invite.
    /// </summary>
    public DateTime? InvitedAt { get; private set; }

    /// <summary>
    /// Gets the identifier of the user who sent the invitation, or <c>null</c> if not an invite.
    /// </summary>
    public Guid? InvitedBy { get; private set; }

    /// <summary>
    /// Gets the UTC date and time when this membership was last updated.
    /// </summary>
    public DateTime UpdatedAt { get; private set; }

    /// <summary>
    /// Private constructor for EF Core materialization.
    /// </summary>
    private TenantMembership() { }

    /// <summary>
    /// Creates a membership for the tenant owner with <see cref="TenantRole.Owner"/> role
    /// and <see cref="TenantMembershipStatus.Active"/> status.
    /// </summary>
    /// <param name="tenantId">The identifier of the tenant.</param>
    /// <param name="userId">The identifier of the owner user.</param>
    /// <returns>A new active owner <see cref="TenantMembership"/>.</returns>
    public static TenantMembership CreateForOwner(Guid tenantId, Guid userId)
    {
        return new TenantMembership
        {
            Id = Guid.NewGuid(),
            TenantId = tenantId,
            UserId = userId,
            Role = TenantRole.Owner,
            Status = TenantMembershipStatus.Active,
            JoinedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };
    }

    /// <summary>
    /// Creates an invitation-based membership with <see cref="TenantMembershipStatus.Pending"/> status.
    /// The invited user must accept the invite to become an active member.
    /// </summary>
    /// <param name="tenantId">The identifier of the tenant.</param>
    /// <param name="email">The email address of the invited user.</param>
    /// <param name="role">The role to assign upon acceptance.</param>
    /// <param name="invitedBy">The identifier of the user sending the invitation.</param>
    /// <returns>A new pending <see cref="TenantMembership"/>.</returns>
    public static TenantMembership CreateInvite(Guid tenantId, string email, TenantRole role, Guid invitedBy)
    {
        return new TenantMembership
        {
            Id = Guid.NewGuid(),
            TenantId = tenantId,
            InvitedEmail = email,
            Role = role,
            Status = TenantMembershipStatus.Pending,
            InvitedAt = DateTime.UtcNow,
            InvitedBy = invitedBy,
            UpdatedAt = DateTime.UtcNow
        };
    }

    /// <summary>
    /// Accepts a pending invitation, transitioning the membership to <see cref="TenantMembershipStatus.Active"/>.
    /// </summary>
    /// <param name="userId">The identifier of the user accepting the invite.</param>
    /// <exception cref="InvalidOperationException">Thrown when the membership is not in Pending status.</exception>
    public void AcceptInvite(Guid userId)
    {
        if (Status != TenantMembershipStatus.Pending)
            throw new InvalidOperationException(
                $"Cannot accept invite for membership with status '{Status}'. Only Pending memberships can be accepted.");

        UserId = userId;
        Status = TenantMembershipStatus.Active;
        JoinedAt = DateTime.UtcNow;
        UpdatedAt = DateTime.UtcNow;
    }

    /// <summary>
    /// Updates the role assigned to this membership. Cannot change the role of the tenant owner.
    /// </summary>
    /// <param name="newRole">The new role to assign.</param>
    /// <exception cref="InvalidOperationException">Thrown when attempting to change the Owner role.</exception>
    public void UpdateRole(TenantRole newRole)
    {
        if (Role == TenantRole.Owner)
            throw new InvalidOperationException("Cannot change the role of the tenant owner.");

        Role = newRole;
        UpdatedAt = DateTime.UtcNow;
    }

    /// <summary>
    /// Deactivates this membership, preventing the user from accessing the tenant.
    /// </summary>
    /// <exception cref="InvalidOperationException">Thrown when the membership is already inactive.</exception>
    public void Deactivate()
    {
        if (Status == TenantMembershipStatus.Inactive)
            throw new InvalidOperationException("Membership is already inactive.");

        Status = TenantMembershipStatus.Inactive;
        UpdatedAt = DateTime.UtcNow;
    }
}
