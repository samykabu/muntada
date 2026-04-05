using System.Security.Cryptography;
using Muntada.Rooms.Domain.Occurrence;
using Muntada.SharedKernel.Domain;

namespace Muntada.Rooms.Domain.Invite;

/// <summary>
/// Entity representing an invitation to a specific room occurrence.
/// Supports email invites, direct links, and guest magic links.
/// </summary>
public class RoomInvite : Entity<RoomInviteId>
{
    /// <summary>Gets the room occurrence this invite belongs to.</summary>
    public RoomOccurrenceId RoomOccurrenceId { get; private set; } = default!;

    /// <summary>Gets the email address of the invitee (for email invites).</summary>
    public string? InvitedEmail { get; private set; }

    /// <summary>Gets the user ID of the invitee (for direct user invites).</summary>
    public string? InvitedUserId { get; private set; }

    /// <summary>Gets the unique invite token for tracking and validation.</summary>
    public string InviteToken { get; private set; } = default!;

    /// <summary>Gets the current status of the invite.</summary>
    public RoomInviteStatus Status { get; private set; }

    /// <summary>Gets the type of invite delivery mechanism.</summary>
    public RoomInviteType InviteType { get; private set; }

    /// <summary>Gets the user who sent the invite.</summary>
    public string InvitedBy { get; private set; } = default!;

    /// <summary>Gets the UTC creation timestamp.</summary>
    public DateTimeOffset CreatedAt { get; private set; }

    /// <summary>Gets the UTC expiration timestamp (default 7 days).</summary>
    public DateTimeOffset ExpiresAt { get; private set; }

    private RoomInvite() { }

    /// <summary>
    /// Creates a new room invite.
    /// </summary>
    /// <param name="occurrenceId">The room occurrence to invite to.</param>
    /// <param name="inviteType">The delivery mechanism type.</param>
    /// <param name="invitedBy">The user sending the invite.</param>
    /// <param name="invitedEmail">Email address (required for Email type).</param>
    /// <param name="invitedUserId">User ID (optional, for DirectLink type).</param>
    /// <param name="expiresInDays">Days until expiration (default 7).</param>
    /// <returns>A new <see cref="RoomInvite"/>.</returns>
    public static RoomInvite Create(
        RoomOccurrenceId occurrenceId,
        RoomInviteType inviteType,
        string invitedBy,
        string? invitedEmail = null,
        string? invitedUserId = null,
        int expiresInDays = 7)
    {
        if (inviteType == RoomInviteType.Email && string.IsNullOrWhiteSpace(invitedEmail))
            throw new SharedKernel.Domain.Exceptions.ValidationException(
                "Validation", "Email address is required for email invites.");

        return new RoomInvite
        {
            Id = RoomInviteId.New(),
            RoomOccurrenceId = occurrenceId,
            InvitedEmail = invitedEmail,
            InvitedUserId = invitedUserId,
            InviteToken = GenerateToken(),
            Status = RoomInviteStatus.Pending,
            InviteType = inviteType,
            InvitedBy = invitedBy,
            CreatedAt = DateTimeOffset.UtcNow,
            ExpiresAt = DateTimeOffset.UtcNow.AddDays(expiresInDays)
        };
    }

    /// <summary>
    /// Marks the invite as accepted when the invitee joins.
    /// </summary>
    public void Accept()
    {
        if (Status != RoomInviteStatus.Pending)
            throw new InvalidOperationException($"Cannot accept invite with status {Status}.");

        Status = RoomInviteStatus.Accepted;
    }

    /// <summary>
    /// Revokes the invite, invalidating the token immediately.
    /// </summary>
    public void Revoke()
    {
        if (Status != RoomInviteStatus.Pending)
            throw new InvalidOperationException($"Cannot revoke invite with status {Status}.");

        Status = RoomInviteStatus.Revoked;
    }

    /// <summary>
    /// Checks if the invite is valid (pending and not expired).
    /// </summary>
    public bool IsValid() =>
        Status == RoomInviteStatus.Pending && ExpiresAt > DateTimeOffset.UtcNow;

    private static string GenerateToken()
    {
        var bytes = RandomNumberGenerator.GetBytes(32);
        return Convert.ToBase64String(bytes)
            .Replace("+", "-")
            .Replace("/", "_")
            .TrimEnd('=');
    }
}
