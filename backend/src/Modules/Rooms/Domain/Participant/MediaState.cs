namespace Muntada.Rooms.Domain.Participant;

/// <summary>
/// Represents the state of a participant's audio or video track.
/// </summary>
public enum MediaState
{
    /// <summary>Audio is muted or video is off.</summary>
    Muted,

    /// <summary>Audio is unmuted or video is on.</summary>
    Unmuted,

    /// <summary>Video track is explicitly off.</summary>
    Off,

    /// <summary>Video track is on.</summary>
    On
}
