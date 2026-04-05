namespace Muntada.Rooms.Domain.Series;

/// <summary>
/// Represents the lifecycle status of a <see cref="RoomSeries"/>.
/// </summary>
public enum RoomSeriesStatus
{
    /// <summary>The series is active and generating occurrences.</summary>
    Active,

    /// <summary>The series has been ended; no further occurrences are generated.</summary>
    Ended
}
