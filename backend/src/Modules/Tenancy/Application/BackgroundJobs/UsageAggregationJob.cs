using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Muntada.Tenancy.Application.Services;
using Muntada.Tenancy.Domain.Tenant;
using Muntada.Tenancy.Domain.Usage;
using Muntada.Tenancy.Infrastructure;

namespace Muntada.Tenancy.Application.BackgroundJobs;

/// <summary>
/// Daily background job that aggregates usage metrics for each active tenant
/// into <see cref="TenantUsageSnapshot"/> records, caches them (TODO: Redis integration),
/// and checks usage thresholds to send alerts via <see cref="IAlertService"/>.
/// </summary>
public class UsageAggregationJob
{
    private readonly TenancyDbContext _dbContext;
    private readonly IPlanLimitService _planLimitService;
    private readonly IAlertService _alertService;
    private readonly ILogger<UsageAggregationJob> _logger;

    /// <summary>
    /// Initializes a new instance of <see cref="UsageAggregationJob"/>.
    /// </summary>
    /// <param name="dbContext">The Tenancy module database context.</param>
    /// <param name="planLimitService">Service for checking tenant plan limits and current usage.</param>
    /// <param name="alertService">Service for sending usage threshold alerts.</param>
    /// <param name="logger">Logger instance.</param>
    public UsageAggregationJob(
        TenancyDbContext dbContext,
        IPlanLimitService planLimitService,
        IAlertService alertService,
        ILogger<UsageAggregationJob> logger)
    {
        _dbContext = dbContext;
        _planLimitService = planLimitService;
        _alertService = alertService;
        _logger = logger;
    }

    /// <summary>
    /// Executes the daily usage aggregation for all active tenants.
    /// For each tenant: aggregates usage into a snapshot, stores in the database,
    /// and checks thresholds to trigger alerts.
    /// </summary>
    /// <param name="cancellationToken">Cancellation token.</param>
    public async Task ExecuteAsync(CancellationToken cancellationToken = default)
    {
        var today = DateOnly.FromDateTime(DateTime.UtcNow);

        var activeTenants = await _dbContext.Tenants
            .AsNoTracking()
            .Where(t => t.Status == TenantStatus.Active)
            .Select(t => t.Id)
            .ToListAsync(cancellationToken);

        if (activeTenants.Count == 0)
        {
            _logger.LogInformation("No active tenants found for usage aggregation");
            return;
        }

        _logger.LogInformation("Starting usage aggregation for {Count} active tenants", activeTenants.Count);

        foreach (var tenantId in activeTenants)
        {
            try
            {
                await AggregateForTenantAsync(tenantId, today, cancellationToken);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex,
                    "Failed to aggregate usage for tenant {TenantId}", tenantId);
            }
        }

        await _dbContext.SaveChangesAsync(cancellationToken);

        _logger.LogInformation("Completed usage aggregation for {Count} tenants", activeTenants.Count);
    }

    /// <summary>
    /// Aggregates usage for a single tenant: collects metrics from limit checks,
    /// creates a snapshot record, and sends threshold alerts if necessary.
    /// </summary>
    private async Task AggregateForTenantAsync(Guid tenantId, DateOnly today, CancellationToken ct)
    {
        // Check if a snapshot already exists for today
        var existingSnapshot = await _dbContext.TenantUsageSnapshots
            .AnyAsync(s => s.TenantId == tenantId && s.SnapshotDate == today, ct);

        if (existingSnapshot)
        {
            _logger.LogDebug("Snapshot already exists for tenant {TenantId} on {Date}", tenantId, today);
            return;
        }

        // Collect current usage via plan limit checks
        var roomsCheck = await _planLimitService.CheckLimitAsync(tenantId, "rooms", ct);
        var participantsCheck = await _planLimitService.CheckLimitAsync(tenantId, "participants", ct);
        var storageCheck = await _planLimitService.CheckLimitAsync(tenantId, "storage", ct);
        var recordingCheck = await _planLimitService.CheckLimitAsync(tenantId, "recording", ct);

        // Create usage snapshot
        var snapshot = TenantUsageSnapshot.Create(
            tenantId: tenantId,
            snapshotDate: today,
            roomsCreated: 0, // TODO: Get from Rooms module when cross-module queries are available
            roomsCreatedMonth: (int)roomsCheck.CurrentUsage,
            peakParticipants: (int)participantsCheck.CurrentUsage,
            storageUsedGb: storageCheck.CurrentUsage,
            recordingHoursUsed: recordingCheck.CurrentUsage);

        _dbContext.TenantUsageSnapshots.Add(snapshot);

        // TODO: Cache snapshot in Redis for fast reads

        // Check thresholds and send alerts
        await CheckAndAlertAsync(tenantId, "rooms", roomsCheck, ct);
        await CheckAndAlertAsync(tenantId, "storage", storageCheck, ct);
        await CheckAndAlertAsync(tenantId, "recording", recordingCheck, ct);

        _logger.LogDebug("Created usage snapshot for tenant {TenantId} on {Date}", tenantId, today);
    }

    /// <summary>
    /// Checks whether the tenant's usage for a resource is at a warning (80%),
    /// critical (95%), or exceeded (100%) threshold and sends an alert if so.
    /// </summary>
    private async Task CheckAndAlertAsync(
        Guid tenantId,
        string resource,
        LimitCheckResult limitCheck,
        CancellationToken ct)
    {
        if (limitCheck.Limit <= 0)
            return; // Unlimited resource, no alert needed

        var percentUsed = (int)(limitCheck.CurrentUsage * 100 / limitCheck.Limit);

        if (percentUsed >= 80)
        {
            await _alertService.SendThresholdAlertAsync(tenantId, resource, percentUsed, ct);
        }
    }
}
