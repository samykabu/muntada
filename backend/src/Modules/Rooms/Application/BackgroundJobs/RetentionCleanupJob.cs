using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Muntada.Rooms.Domain.Occurrence;
using Muntada.Rooms.Infrastructure;

namespace Muntada.Rooms.Application.BackgroundJobs;

/// <summary>
/// Background job that archives ended rooms after the retention period expires.
/// Runs daily, scanning for rooms in <see cref="RoomOccurrenceStatus.Ended"/> status
/// that have exceeded the retention window.
/// </summary>
public sealed class RetentionCleanupJob : BackgroundService
{
    private readonly IServiceScopeFactory _scopeFactory;
    private readonly ILogger<RetentionCleanupJob> _logger;

    /// <summary>The interval between job runs (daily).</summary>
    private static readonly TimeSpan RunInterval = TimeSpan.FromHours(24);

    /// <summary>
    /// The default retention period after which ended rooms are archived.
    /// Rooms that ended more than this many days ago will be transitioned to Archived.
    /// </summary>
    private const int RetentionDays = 90;

    /// <summary>
    /// Initializes a new instance of the <see cref="RetentionCleanupJob"/> class.
    /// </summary>
    public RetentionCleanupJob(IServiceScopeFactory scopeFactory, ILogger<RetentionCleanupJob> logger)
    {
        _scopeFactory = scopeFactory;
        _logger = logger;
    }

    /// <inheritdoc />
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        // Initial delay to avoid running immediately on startup
        await Task.Delay(TimeSpan.FromMinutes(5), stoppingToken);

        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                await ArchiveEndedRoomsAsync(stoppingToken);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error in retention cleanup job.");
            }

            await Task.Delay(RunInterval, stoppingToken);
        }
    }

    private async Task ArchiveEndedRoomsAsync(CancellationToken cancellationToken)
    {
        using var scope = _scopeFactory.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<RoomsDbContext>();

        var cutoff = DateTimeOffset.UtcNow.AddDays(-RetentionDays);

        var endedRooms = await db.RoomOccurrences
            .Where(o => o.Status == RoomOccurrenceStatus.Ended
                && o.LiveEndedAt.HasValue
                && o.LiveEndedAt.Value <= cutoff)
            .ToListAsync(cancellationToken);

        if (endedRooms.Count == 0)
        {
            _logger.LogDebug("Retention cleanup: no rooms to archive.");
            return;
        }

        _logger.LogInformation("Retention cleanup: archiving {Count} ended rooms older than {Days} days.",
            endedRooms.Count, RetentionDays);

        foreach (var room in endedRooms)
        {
            try
            {
                room.Archive();
                room.IncrementVersion();
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Could not archive room {OccurrenceId}.", room.Id.Value);
            }
        }

        await db.SaveChangesAsync(cancellationToken);

        _logger.LogInformation("Retention cleanup completed. Archived {Count} rooms.", endedRooms.Count);
    }
}
