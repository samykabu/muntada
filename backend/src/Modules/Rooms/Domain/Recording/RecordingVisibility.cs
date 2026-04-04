namespace Muntada.Rooms.Domain.Recording;

/// <summary>
/// Controls who can access a room recording.
/// </summary>
public enum RecordingVisibility
{
    /// <summary>Only the room organizer can access.</summary>
    Private,

    /// <summary>Room participants can access.</summary>
    Shared,

    /// <summary>Any tenant member can access.</summary>
    Public
}
