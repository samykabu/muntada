namespace Muntada.Rooms.Domain.Occurrence;

/// <summary>
/// Represents the lifecycle status of a <see cref="RoomOccurrence"/>.
/// Transitions are enforced by a state machine.
/// </summary>
public enum RoomOccurrenceStatus
{
    /// <summary>Room created but not yet scheduled (needs moderator/time).</summary>
    Draft,

    /// <summary>Room has a scheduled time and moderator assigned.</summary>
    Scheduled,

    /// <summary>Room is active with at least one participant connected.</summary>
    Live,

    /// <summary>Moderator disconnected; countdown active before auto-end.</summary>
    Grace,

    /// <summary>Room session has concluded.</summary>
    Ended,

    /// <summary>Room retained per policy; pending deletion.</summary>
    Archived
}
