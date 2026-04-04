using Muntada.SharedKernel.Domain;

namespace Muntada.Tenancy.Domain.Usage;

/// <summary>
/// Represents a daily aggregated snapshot of a tenant's resource usage.
/// Used for usage tracking, reporting, and threshold alerting.
/// </summary>
public class TenantUsageSnapshot : Entity<Guid>
{
    /// <summary>
    /// Gets the identifier of the tenant this snapshot belongs to.
    /// </summary>
    public Guid TenantId { get; private set; }

    /// <summary>
    /// Gets the date for which this snapshot was aggregated.
    /// </summary>
    public DateOnly SnapshotDate { get; private set; }

    /// <summary>
    /// Gets the total number of rooms created on this date.
    /// </summary>
    public int RoomsCreated { get; private set; }

    /// <summary>
    /// Gets the cumulative number of rooms created in the month up to and including this date.
    /// </summary>
    public int RoomsCreatedMonth { get; private set; }

    /// <summary>
    /// Gets the peak number of concurrent participants observed on this date.
    /// </summary>
    public int PeakParticipants { get; private set; }

    /// <summary>
    /// Gets the total storage used by the tenant in gigabytes.
    /// </summary>
    public decimal StorageUsedGB { get; private set; }

    /// <summary>
    /// Gets the total recording hours used by the tenant in the current month.
    /// </summary>
    public decimal RecordingHoursUsed { get; private set; }

    /// <summary>
    /// Gets the UTC date and time when this snapshot was created.
    /// </summary>
    public DateTime CreatedAt { get; private set; }

    /// <summary>
    /// Private constructor for EF Core materialization.
    /// </summary>
    private TenantUsageSnapshot() { }

    /// <summary>
    /// Creates a new daily usage snapshot for a tenant.
    /// </summary>
    /// <param name="tenantId">The identifier of the tenant.</param>
    /// <param name="snapshotDate">The date for which usage is being aggregated.</param>
    /// <param name="roomsCreated">Number of rooms created on this date.</param>
    /// <param name="roomsCreatedMonth">Cumulative rooms created in the month.</param>
    /// <param name="peakParticipants">Peak concurrent participants on this date.</param>
    /// <param name="storageUsedGb">Storage used in gigabytes.</param>
    /// <param name="recordingHoursUsed">Recording hours used in the current month.</param>
    /// <returns>A new <see cref="TenantUsageSnapshot"/> instance.</returns>
    public static TenantUsageSnapshot Create(
        Guid tenantId,
        DateOnly snapshotDate,
        int roomsCreated,
        int roomsCreatedMonth,
        int peakParticipants,
        decimal storageUsedGb,
        decimal recordingHoursUsed)
    {
        return new TenantUsageSnapshot
        {
            Id = Guid.NewGuid(),
            TenantId = tenantId,
            SnapshotDate = snapshotDate,
            RoomsCreated = roomsCreated,
            RoomsCreatedMonth = roomsCreatedMonth,
            PeakParticipants = peakParticipants,
            StorageUsedGB = storageUsedGb,
            RecordingHoursUsed = recordingHoursUsed,
            CreatedAt = DateTime.UtcNow
        };
    }
}
