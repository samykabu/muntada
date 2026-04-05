namespace Muntada.Rooms.Application.Services;

/// <summary>
/// Service for generating occurrence dates from iCal recurrence rules (RRULE).
/// Handles timezone-aware date generation for recurring room series.
/// </summary>
public interface IRecurrenceService
{
    /// <summary>
    /// Generates a list of occurrence dates from an iCal RRULE.
    /// </summary>
    /// <param name="rrule">The iCal RRULE string (e.g., "FREQ=WEEKLY;BYDAY=MO,WE,FR").</param>
    /// <param name="timeZoneId">The IANA timezone identifier for DST-correct generation.</param>
    /// <param name="startsAt">The UTC start date of the series (first occurrence >= this).</param>
    /// <param name="endsAt">The optional UTC end date of the series.</param>
    /// <param name="generateUntil">Generate occurrences up to this UTC date (rolling horizon).</param>
    /// <returns>A list of UTC occurrence dates.</returns>
    List<DateTimeOffset> GenerateOccurrences(
        string rrule,
        string timeZoneId,
        DateTimeOffset startsAt,
        DateTimeOffset? endsAt,
        DateTimeOffset generateUntil);
}
