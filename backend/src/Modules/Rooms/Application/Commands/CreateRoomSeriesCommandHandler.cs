using MediatR;
using Microsoft.EntityFrameworkCore;
using Muntada.Rooms.Application.Services;
using Muntada.Rooms.Domain.Occurrence;
using Muntada.Rooms.Domain.Series;
using Muntada.Rooms.Domain.Template;
using Muntada.Rooms.Infrastructure;
using Muntada.SharedKernel.Domain.Exceptions;

namespace Muntada.Rooms.Application.Commands;

/// <summary>
/// Command to create a new recurring room series with initial occurrence generation.
/// </summary>
/// <param name="TenantId">The owning tenant's identifier.</param>
/// <param name="TemplateId">The template to use for default room settings.</param>
/// <param name="Title">The series title (3-200 chars).</param>
/// <param name="RecurrenceRule">The iCal RRULE string.</param>
/// <param name="OrganizerTimeZoneId">IANA timezone identifier for DST-correct scheduling.</param>
/// <param name="StartsAt">The UTC start date of the series.</param>
/// <param name="EndsAt">The optional UTC end date of the series.</param>
/// <param name="ModeratorUserId">The default moderator for generated occurrences.</param>
/// <param name="CreatedBy">The user creating the series.</param>
public sealed record CreateRoomSeriesCommand(
    string TenantId,
    string TemplateId,
    string Title,
    string RecurrenceRule,
    string OrganizerTimeZoneId,
    DateTimeOffset StartsAt,
    DateTimeOffset? EndsAt,
    string ModeratorUserId,
    string CreatedBy) : IRequest<RoomSeries>;

/// <summary>
/// Handles <see cref="CreateRoomSeriesCommand"/> — creates the series, validates the RRULE,
/// generates initial occurrences 30 days ahead using <see cref="IRecurrenceService"/>.
/// </summary>
public sealed class CreateRoomSeriesCommandHandler : IRequestHandler<CreateRoomSeriesCommand, RoomSeries>
{
    private readonly RoomsDbContext _db;
    private readonly IRecurrenceService _recurrenceService;

    /// <summary>The number of days ahead to generate initial occurrences.</summary>
    private const int GenerateAheadDays = 30;

    /// <summary>
    /// Initializes a new instance of the <see cref="CreateRoomSeriesCommandHandler"/> class.
    /// </summary>
    public CreateRoomSeriesCommandHandler(RoomsDbContext db, IRecurrenceService recurrenceService)
    {
        _db = db;
        _recurrenceService = recurrenceService;
    }

    /// <inheritdoc />
    public async Task<RoomSeries> Handle(CreateRoomSeriesCommand request, CancellationToken cancellationToken)
    {
        // Validate template exists for this tenant
        var templateId = new RoomTemplateId(request.TemplateId);
        var template = await _db.RoomTemplates
            .AsNoTracking()
            .FirstOrDefaultAsync(t => t.Id == templateId && t.TenantId == request.TenantId, cancellationToken);

        if (template is null)
            throw new EntityNotFoundException(nameof(RoomTemplate), request.TemplateId);

        // Create the series
        var series = RoomSeries.Create(
            request.TenantId,
            templateId,
            request.Title,
            request.RecurrenceRule,
            request.OrganizerTimeZoneId,
            request.StartsAt,
            request.EndsAt,
            request.CreatedBy);

        _db.RoomSeries.Add(series);

        // Generate initial occurrences 30 days ahead
        var generateUntil = DateTimeOffset.UtcNow.AddDays(GenerateAheadDays);
        var occurrenceDates = _recurrenceService.GenerateOccurrences(
            request.RecurrenceRule,
            request.OrganizerTimeZoneId,
            request.StartsAt,
            request.EndsAt,
            generateUntil);

        foreach (var scheduledAt in occurrenceDates)
        {
            var occurrence = RoomOccurrence.CreateFromSeries(
                request.TenantId,
                series.Id,
                request.Title,
                scheduledAt,
                request.OrganizerTimeZoneId,
                template.Settings,
                request.CreatedBy);

            occurrence.AssignModeratorAndSchedule(request.ModeratorUserId);
            _db.RoomOccurrences.Add(occurrence);
        }

        await _db.SaveChangesAsync(cancellationToken);

        return series;
    }
}
