namespace Muntada.Rooms.Domain.Participant;

/// <summary>
/// Represents the role of a participant within a room occurrence.
/// Determines permissions: Moderator has full controls, Member can speak/listen,
/// Guest can only listen.
/// </summary>
public enum ParticipantRole
{
    /// <summary>Full room controls (mute all, end room, record).</summary>
    Moderator,

    /// <summary>Can speak and listen.</summary>
    Member,

    /// <summary>Listen-only, no chat or file access.</summary>
    Guest
}
