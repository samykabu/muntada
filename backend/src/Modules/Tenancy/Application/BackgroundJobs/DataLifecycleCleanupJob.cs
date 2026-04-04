using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Muntada.Tenancy.Infrastructure;

namespace Muntada.Tenancy.Application.BackgroundJobs;

/// <summary>
/// Daily background job that enforces data retention policies across all tenants.
/// For each tenant, identifies data past retention periods, soft-deletes with a 7-day
/// grace period, then hard-deletes after grace expires. All deletions are logged for audit.
/// </summary>
/// <remarks>
/// This is a skeleton implementation. Actual data deletion depends on other modules
/// (Recordings, Chat, Files, AuditLog, Activity) which are not yet available.
/// Each module should expose a retention-aware cleanup interface that this job can invoke.
/// </remarks>
public class DataLifecycleCleanupJob
{
    private const int GracePeriodDays = 7;

    private readonly TenancyDbContext _dbContext;
    private readonly ILogger<DataLifecycleCleanupJob> _logger;

    /// <summary>
    /// Initializes a new instance of <see cref="DataLifecycleCleanupJob"/>.
    /// </summary>
    /// <param name="dbContext">The Tenancy module database context.</param>
    /// <param name="logger">Logger for tracking cleanup operations.</param>
    public DataLifecycleCleanupJob(
        TenancyDbContext dbContext,
        ILogger<DataLifecycleCleanupJob> logger)
    {
        _dbContext = dbContext;
        _logger = logger;
    }

    /// <summary>
    /// Executes the data lifecycle cleanup for all active tenants.
    /// Processes each tenant's retention policy and marks or removes expired data.
    /// </summary>
    /// <param name="cancellationToken">Cancellation token.</param>
    public async Task ExecuteAsync(CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("DataLifecycleCleanupJob started at {UtcNow}", DateTime.UtcNow);

        var policies = await _dbContext.RetentionPolicies
            .AsNoTracking()
            .ToListAsync(cancellationToken);

        if (policies.Count == 0)
        {
            _logger.LogInformation("No retention policies found. Skipping cleanup");
            return;
        }

        foreach (var policy in policies)
        {
            try
            {
                await ProcessTenantRetentionAsync(policy.TenantId, policy, cancellationToken);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex,
                    "Error processing retention cleanup for tenant {TenantId}",
                    policy.TenantId);
            }
        }

        _logger.LogInformation(
            "DataLifecycleCleanupJob completed. Processed {Count} tenants",
            policies.Count);
    }

    /// <summary>
    /// Processes retention cleanup for a single tenant based on their retention policy.
    /// </summary>
    /// <param name="tenantId">The tenant identifier.</param>
    /// <param name="policy">The tenant's retention policy.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    private async Task ProcessTenantRetentionAsync(
        Guid tenantId,
        Domain.Retention.RetentionPolicy policy,
        CancellationToken cancellationToken)
    {
        var now = DateTime.UtcNow;

        // Calculate cutoff dates for soft-delete (data past retention period)
        var recordingCutoff = now.AddDays(-policy.RecordingRetentionDays);
        var chatCutoff = now.AddDays(-policy.ChatMessageRetentionDays);
        var fileCutoff = now.AddDays(-policy.FileRetentionDays);
        var auditLogCutoff = now.AddDays(-policy.AuditLogRetentionDays);
        var activityCutoff = now.AddDays(-policy.UserActivityLogRetentionDays);

        // Calculate hard-delete cutoff (soft-deleted data past grace period)
        var hardDeleteCutoff = now.AddDays(-GracePeriodDays);

        _logger.LogInformation(
            "Processing retention for tenant {TenantId}: " +
            "Recordings={RecordingDays}d, Chat={ChatDays}d, Files={FileDays}d, " +
            "AuditLog={AuditDays}d, Activity={ActivityDays}d",
            tenantId,
            policy.RecordingRetentionDays,
            policy.ChatMessageRetentionDays,
            policy.FileRetentionDays,
            policy.AuditLogRetentionDays,
            policy.UserActivityLogRetentionDays);

        // TODO: Phase 1 — Soft-delete expired data
        // When modules are available, invoke their retention cleanup interfaces:
        //   await _recordingCleanupService.SoftDeleteExpiredAsync(tenantId, recordingCutoff, ct);
        //   await _chatCleanupService.SoftDeleteExpiredAsync(tenantId, chatCutoff, ct);
        //   await _fileCleanupService.SoftDeleteExpiredAsync(tenantId, fileCutoff, ct);
        //   await _auditLogCleanupService.SoftDeleteExpiredAsync(tenantId, auditLogCutoff, ct);
        //   await _activityCleanupService.SoftDeleteExpiredAsync(tenantId, activityCutoff, ct);

        // TODO: Phase 2 — Hard-delete soft-deleted data past grace period
        // When modules are available:
        //   await _recordingCleanupService.HardDeleteGracedAsync(tenantId, hardDeleteCutoff, ct);
        //   await _chatCleanupService.HardDeleteGracedAsync(tenantId, hardDeleteCutoff, ct);
        //   await _fileCleanupService.HardDeleteGracedAsync(tenantId, hardDeleteCutoff, ct);
        //   await _activityCleanupService.HardDeleteGracedAsync(tenantId, hardDeleteCutoff, ct);
        //   Note: Audit logs are typically not hard-deleted; only soft-deleted for compliance.

        _logger.LogDebug(
            "Retention cleanup complete for tenant {TenantId}. " +
            "Hard-delete cutoff: {HardDeleteCutoff:O}",
            tenantId, hardDeleteCutoff);

        await Task.CompletedTask;
    }
}
