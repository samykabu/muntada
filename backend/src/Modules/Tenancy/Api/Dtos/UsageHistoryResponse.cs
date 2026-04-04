namespace Muntada.Tenancy.Api.Dtos;

/// <summary>
/// Response DTO representing a tenant's daily usage history over a date range.
/// </summary>
/// <param name="TenantId">The identifier of the tenant.</param>
/// <param name="FromDate">The start date of the history range.</param>
/// <param name="ToDate">The end date of the history range.</param>
/// <param name="Snapshots">The daily usage snapshots ordered by date descending.</param>
public sealed record UsageHistoryResponse(
    Guid TenantId,
    DateOnly FromDate,
    DateOnly ToDate,
    IReadOnlyList<UsageSnapshotResponse> Snapshots);

/// <summary>
/// Response DTO representing a single daily usage snapshot.
/// </summary>
/// <param name="SnapshotDate">The date of the snapshot.</param>
/// <param name="RoomsCreated">Number of rooms created on this date.</param>
/// <param name="RoomsCreatedMonth">Cumulative rooms created in the month up to this date.</param>
/// <param name="PeakParticipants">Peak concurrent participants on this date.</param>
/// <param name="StorageUsedGB">Storage used in gigabytes.</param>
/// <param name="RecordingHoursUsed">Recording hours used in the current month.</param>
public sealed record UsageSnapshotResponse(
    DateOnly SnapshotDate,
    int RoomsCreated,
    int RoomsCreatedMonth,
    int PeakParticipants,
    decimal StorageUsedGB,
    decimal RecordingHoursUsed);
