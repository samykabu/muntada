using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Muntada.Rooms.Application.Services;
using Muntada.Rooms.Domain.Occurrence;
using Muntada.Rooms.Domain.Series;
using Muntada.Rooms.Infrastructure;

namespace Muntada.Rooms.Application.BackgroundJobs;

/// <summary>
/// Background job that runs hourly to generate room occurrences for all active series.
/// Generates occurrences 30 days ahead. Idempotent — skips dates that already have an occurrence.
/// </summary>
public sealed class OccurrenceGenerationJob : BackgroundService
{
    private readonly IServiceScopeFactory _scopeFactory;
    private readonly ILogger<OccurrenceGenerationJob> _logger;

    /// <summary>The interval between job runs.</summary>
    private static readonly TimeSpan RunInterval = TimeSpan.FromHours(1);

    /// <summary>The number of days ahead to generate occurrences.</summary>
    private const int GenerateAheadDays = 30;

    /// <summary>
    /// Initializes a new instance of the <see cref="OccurrenceGenerationJob"/> class.
    /// </summary>
    public OccurrenceGenerationJob(IServiceScopeFactory scopeFactory, ILogger<OccurrenceGenerationJob> logger)
    {
        _scopeFactory = scopeFactory;
        _logger = logger;
    }

    /// <inheritdoc />
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                await GenerateOccurrencesAsync(stoppingToken);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error generating occurrences in background job.");
            }

            await Task.Delay(RunInterval, stoppingToken);
        }
    }

    private async Task GenerateOccurrencesAsync(CancellationToken cancellationToken)
    {
        using var scope = _scopeFactory.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<RoomsDbContext>();
        var recurrenceService = scope.ServiceProvider.GetRequiredService<IRecurrenceService>();

        var activeSeries = await db.RoomSeries
            .Where(s => s.Status == RoomSeriesStatus.Active)
            .ToListAsync(cancellationToken);

        _logger.LogInformation("Occurrence generation job processing {Count} active series.", activeSeries.Count);

        var now = DateTimeOffset.UtcNow;
        var generateUntil = now.AddDays(GenerateAheadDays);

        foreach (var series in activeSeries)
        {
            try
            {
                // Load the template for settings
                var template = await db.RoomTemplates
                    .AsNoTracking()
                    .FirstOrDefaultAsync(t => t.Id == series.TemplateId, cancellationToken);

                if (template is null)
                {
                    _logger.LogWarning("Template {TemplateId} not found for series {SeriesId}. Skipping.",
                        series.TemplateId.Value, series.Id.Value);
                    continue;
                }

                // Get existing occurrence dates for this series to avoid duplicates
                var existingDates = await db.RoomOccurrences
                    .Where(o => o.RoomSeriesId == series.Id && o.ScheduledAt >= now)
                    .Select(o => o.ScheduledAt)
                    .ToListAsync(cancellationToken);

                var existingDateSet = new HashSet<DateTimeOffset>(existingDates);

                var occurrenceDates = recurrenceService.GenerateOccurrences(
                    series.RecurrenceRule,
                    series.OrganizerTimeZoneId,
                    now,
                    series.EndsAt,
                    generateUntil);

                var newCount = 0;
                foreach (var scheduledAt in occurrenceDates)
                {
                    if (existingDateSet.Contains(scheduledAt))
                        continue;

                    var occurrence = RoomOccurrence.CreateFromSeries(
                        series.TenantId,
                        series.Id,
                        series.Title,
                        scheduledAt,
                        series.OrganizerTimeZoneId,
                        template.Settings,
                        series.CreatedBy);

                    db.RoomOccurrences.Add(occurrence);
                    newCount++;
                }

                if (newCount > 0)
                {
                    _logger.LogInformation("Generated {Count} new occurrences for series {SeriesId}.",
                        newCount, series.Id.Value);
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error generating occurrences for series {SeriesId}.", series.Id.Value);
            }
        }

        await db.SaveChangesAsync(cancellationToken);
    }
}
