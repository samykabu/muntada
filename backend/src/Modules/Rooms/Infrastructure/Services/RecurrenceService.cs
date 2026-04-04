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

        // Resolve IANA timezone for DST-correct expansion
        var tz = TimeZoneInfo.FindSystemTimeZoneById(timeZoneId);
        var localStart = TimeZoneInfo.ConvertTimeFromUtc(startsAt.UtcDateTime, tz);

        // Build an iCal event with the RRULE to use Ical.Net's recurrence evaluation
        var calendarEvent = new CalendarEvent
        {
            DtStart = new CalDateTime(localStart, timeZoneId),
            DtEnd = new CalDateTime(localStart.AddHours(1), timeZoneId),
        };

        // Parse and attach the recurrence rule
        var recurrencePattern = new RecurrencePattern(rrule);
        calendarEvent.RecurrenceRules.Add(recurrencePattern);

        // Get occurrences within the date range
        var searchStart = new CalDateTime(localStart, timeZoneId);
        var searchEnd = new CalDateTime(TimeZoneInfo.ConvertTimeFromUtc(effectiveEnd.UtcDateTime, tz), timeZoneId);
        var occurrences = calendarEvent.GetOccurrences(searchStart, searchEnd);

        var result = new List<DateTimeOffset>();
        foreach (var occurrence in occurrences)
        {
            var localDt = occurrence.Period.StartTime.Value;
            var utcDt = TimeZoneInfo.ConvertTimeToUtc(localDt, tz);
            var utcDate = new DateTimeOffset(utcDt, TimeSpan.Zero);
            if (utcDate >= startsAt)
            {
                result.Add(utcDate);
            }
        }

        return result;
    }
}
