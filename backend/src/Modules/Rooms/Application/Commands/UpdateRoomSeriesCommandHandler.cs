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
/// Command to update a room series recurrence pattern and regenerate future occurrences.
/// </summary>
/// <param name="TenantId">The owning tenant's identifier.</param>
/// <param name="SeriesId">The series to update.</param>
/// <param name="RecurrenceRule">Updated RRULE (null to keep current).</param>
/// <param name="EndsAt">Updated end date (null to keep current).</param>
/// <param name="ModeratorUserId">The default moderator for newly generated occurrences.</param>
public sealed record UpdateRoomSeriesCommand(
    string TenantId,
    string SeriesId,
    string? RecurrenceRule,
    DateTimeOffset? EndsAt,
    string ModeratorUserId) : IRequest<RoomSeries>;

/// <summary>
/// Handles <see cref="UpdateRoomSeriesCommand"/> — updates recurrence and regenerates
/// future occurrences that have not yet started.
/// </summary>
public sealed class UpdateRoomSeriesCommandHandler : IRequestHandler<UpdateRoomSeriesCommand, RoomSeries>
{
    private readonly RoomsDbContext _db;
    private readonly IRecurrenceService _recurrenceService;

    /// <summary>The number of days ahead to generate occurrences.</summary>
    private const int GenerateAheadDays = 30;

    /// <summary>
    /// Initializes a new instance of the <see cref="UpdateRoomSeriesCommandHandler"/> class.
    /// </summary>
    public UpdateRoomSeriesCommandHandler(RoomsDbContext db, IRecurrenceService recurrenceService)
    {
        _db = db;
        _recurrenceService = recurrenceService;
    }

    /// <inheritdoc />
    public async Task<RoomSeries> Handle(UpdateRoomSeriesCommand request, CancellationToken cancellationToken)
    {
        var seriesId = new RoomSeriesId(request.SeriesId);
        var series = await _db.RoomSeries
            .FirstOrDefaultAsync(s => s.Id == seriesId && s.TenantId == request.TenantId, cancellationToken);

        if (series is null)
            throw new EntityNotFoundException(nameof(RoomSeries), request.SeriesId);

        series.UpdateRecurrence(request.RecurrenceRule, request.EndsAt);
        series.IncrementVersion();

        // Remove future Draft/Scheduled occurrences that have not yet started
        var now = DateTimeOffset.UtcNow;
        var futureOccurrences = await _db.RoomOccurrences
            .Where(o => o.RoomSeriesId == seriesId
                && o.ScheduledAt > now
                && (o.Status == RoomOccurrenceStatus.Draft || o.Status == RoomOccurrenceStatus.Scheduled))
            .ToListAsync(cancellationToken);

        _db.RoomOccurrences.RemoveRange(futureOccurrences);

        // Regenerate occurrences with updated recurrence
        var templateId = series.TemplateId;
        var template = await _db.RoomTemplates
            .AsNoTracking()
            .FirstOrDefaultAsync(t => t.Id == templateId, cancellationToken);

        if (template is not null)
        {
            var generateUntil = now.AddDays(GenerateAheadDays);
            var occurrenceDates = _recurrenceService.GenerateOccurrences(
                series.RecurrenceRule,
                series.OrganizerTimeZoneId,
                now,
                series.EndsAt,
                generateUntil);

            foreach (var scheduledAt in occurrenceDates)
            {
                var occurrence = RoomOccurrence.CreateFromSeries(
                    series.TenantId,
                    seriesId,
                    series.Title,
                    scheduledAt,
                    series.OrganizerTimeZoneId,
                    template.Settings,
                    series.CreatedBy);

                occurrence.AssignModeratorAndSchedule(request.ModeratorUserId);
                _db.RoomOccurrences.Add(occurrence);
            }
        }

        await _db.SaveChangesAsync(cancellationToken);

        return series;
    }
}
