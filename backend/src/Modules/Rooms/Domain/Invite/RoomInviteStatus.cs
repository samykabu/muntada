namespace Muntada.Rooms.Domain.Invite;

/// <summary>
/// Represents the status of a <see cref="RoomInvite"/>.
/// </summary>
public enum RoomInviteStatus
{
    /// <summary>Invite sent but not yet accepted.</summary>
    Pending,

    /// <summary>Invite accepted and participant joined.</summary>
    Accepted,

    /// <summary>Invite explicitly revoked by the organizer.</summary>
    Revoked,

    /// <summary>Invite expired past its expiration date.</summary>
    Expired
}
