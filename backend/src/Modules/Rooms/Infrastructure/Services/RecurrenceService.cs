using Ical.Net.CalendarComponents;
using Ical.Net.DataTypes;
using Muntada.Rooms.Application.Services;

namespace Muntada.Rooms.Infrastructure.Services;

/// <summary>
/// Implementation of <see cref="IRecurrenceService"/> using the Ical.Net library.
/// Parses RRULE strings and generates timezone-aware occurrence dates.
/// </summary>
public sealed class RecurrenceService : IRecurrenceService
{
    /// <inheritdoc />
    public List<DateTimeOffset> GenerateOccurrences(
        string rrule,
        string timeZoneId,
        DateTimeOffset startsAt,
        DateTimeOffset? endsAt,
        DateTimeOffset generateUntil)
    {
        var effectiveEnd = endsAt.HasValue && endsAt.Value < generateUntil
            ? endsAt.Value
            : generateUntil;

        // Build an iCal event with the RRULE to use Ical.Net's recurrence evaluation
        var calendarEvent = new CalendarEvent
        {
            DtStart = new CalDateTime(startsAt.UtcDateTime, "UTC"),
            DtEnd = new CalDateTime(startsAt.UtcDateTime.AddHours(1), "UTC"),
        };

        // Parse and attach the recurrence rule
        var recurrencePattern = new RecurrencePattern(rrule);
        calendarEvent.RecurrenceRules.Add(recurrencePattern);

        // Get occurrences within the date range
        var searchStart = new CalDateTime(startsAt.UtcDateTime, "UTC");
        var searchEnd = new CalDateTime(effectiveEnd.UtcDateTime, "UTC");
        var occurrences = calendarEvent.GetOccurrences(searchStart, searchEnd);

        var result = new List<DateTimeOffset>();
        foreach (var occurrence in occurrences)
        {
            var utcDate = new DateTimeOffset(occurrence.Period.StartTime.Value, TimeSpan.Zero);
            if (utcDate >= startsAt)
            {
                result.Add(utcDate);
            }
        }

        return result;
    }
}
