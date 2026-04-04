namespace Muntada.Rooms.Domain.Invite;

/// <summary>
/// Represents the type of room invite delivery mechanism.
/// </summary>
public enum RoomInviteType
{
    /// <summary>Invite sent via email with a join link.</summary>
    Email,

    /// <summary>Direct join link shared with authenticated users.</summary>
    DirectLink,

    /// <summary>Magic link for unauthenticated guest access (listen-only).</summary>
    GuestMagicLink
}
