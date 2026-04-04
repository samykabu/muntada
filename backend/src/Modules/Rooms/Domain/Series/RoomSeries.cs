using Muntada.Rooms.Domain.Template;
using Muntada.SharedKernel.Domain;

namespace Muntada.Rooms.Domain.Series;

/// <summary>
/// Aggregate root representing a recurring room series.
/// Generates <see cref="Occurrence.RoomOccurrence"/> instances based on an iCal recurrence rule.
/// Stores the organizer's timezone for DST-correct occurrence generation.
/// </summary>
public class RoomSeries : AggregateRoot<RoomSeriesId>
{
    /// <summary>Gets the tenant that owns this series.</summary>
    public string TenantId { get; private set; } = default!;

    /// <summary>Gets the template used for default room settings.</summary>
    public RoomTemplateId TemplateId { get; private set; } = default!;

    /// <summary>Gets the series title displayed on occurrences.</summary>
    public string Title { get; private set; } = default!;

    /// <summary>Gets the iCal RRULE recurrence pattern.</summary>
    public string RecurrenceRule { get; private set; } = default!;

    /// <summary>Gets the IANA timezone identifier for DST-correct scheduling.</summary>
    public string OrganizerTimeZoneId { get; private set; } = default!;

    /// <summary>Gets the UTC start date of the series (first occurrence >= this).</summary>
    public DateTimeOffset StartsAt { get; private set; }

    /// <summary>Gets the optional UTC end date of the series.</summary>
    public DateTimeOffset? EndsAt { get; private set; }

    /// <summary>Gets the current status of the series.</summary>
    public RoomSeriesStatus Status { get; private set; }

    /// <summary>Gets the identifier of the user who created this series.</summary>
    public string CreatedBy { get; private set; } = default!;

    private RoomSeries() { }

    /// <summary>
    /// Creates a new recurring room series.
    /// </summary>
    public static RoomSeries Create(
        string tenantId,
        RoomTemplateId templateId,
        string title,
        string recurrenceRule,
        string organizerTimeZoneId,
        DateTimeOffset startsAt,
        DateTimeOffset? endsAt,
        string createdBy)
    {
        if (string.IsNullOrWhiteSpace(title) || title.Length < 3 || title.Length > 200)
            throw new SharedKernel.Domain.Exceptions.ValidationException(
                "Validation", "Series title must be between 3 and 200 characters.");

        if (string.IsNullOrWhiteSpace(recurrenceRule))
            throw new SharedKernel.Domain.Exceptions.ValidationException(
                "Validation", "Recurrence rule (RRULE) is required.");

        if (string.IsNullOrWhiteSpace(organizerTimeZoneId))
            throw new SharedKernel.Domain.Exceptions.ValidationException(
                "Validation", "Organizer timezone is required.");

        if (endsAt.HasValue && endsAt.Value <= startsAt)
            throw new SharedKernel.Domain.Exceptions.ValidationException(
                "Validation", "Series end date must be after start date.");

        return new RoomSeries
        {
            Id = RoomSeriesId.New(),
            TenantId = tenantId,
            TemplateId = templateId,
            Title = title,
            RecurrenceRule = recurrenceRule,
            OrganizerTimeZoneId = organizerTimeZoneId,
            StartsAt = startsAt,
            EndsAt = endsAt,
            Status = RoomSeriesStatus.Active,
            CreatedBy = createdBy,
            CreatedAt = DateTimeOffset.UtcNow,
            UpdatedAt = DateTimeOffset.UtcNow
        };
    }

    /// <summary>
    /// Updates the series recurrence pattern and/or end date.
    /// Only affects future occurrences.
    /// </summary>
    public void UpdateRecurrence(string? recurrenceRule, DateTimeOffset? endsAt)
    {
        if (Status != RoomSeriesStatus.Active)
            throw new InvalidOperationException("Cannot modify an ended series.");

        if (recurrenceRule is not null)
        {
            if (string.IsNullOrWhiteSpace(recurrenceRule))
                throw new SharedKernel.Domain.Exceptions.ValidationException(
                "Validation", "Recurrence rule (RRULE) cannot be empty.");
            RecurrenceRule = recurrenceRule;
        }

        if (endsAt.HasValue && endsAt.Value <= StartsAt)
            throw new SharedKernel.Domain.Exceptions.ValidationException(
                "Validation", "Series end date must be after start date.");

        EndsAt = endsAt;
        UpdatedAt = DateTimeOffset.UtcNow;
    }

    /// <summary>
    /// Ends the series, preventing further occurrence generation.
    /// </summary>
    public void End()
    {
        if (Status != RoomSeriesStatus.Active)
            throw new InvalidOperationException("Series is already ended.");

        Status = RoomSeriesStatus.Ended;
        UpdatedAt = DateTimeOffset.UtcNow;
    }
}
