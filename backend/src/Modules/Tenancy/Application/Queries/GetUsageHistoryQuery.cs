using MediatR;
using Microsoft.EntityFrameworkCore;
using Muntada.Tenancy.Infrastructure;

namespace Muntada.Tenancy.Application.Queries;

/// <summary>
/// Query to retrieve daily usage snapshots for a tenant over a specified number of days.
/// </summary>
/// <param name="TenantId">The identifier of the tenant whose usage history to retrieve.</param>
/// <param name="Days">The number of days of history to return (default 30, maximum 90).</param>
public sealed record GetUsageHistoryQuery(Guid TenantId, int Days = 30) : IRequest<UsageHistoryResult>;

/// <summary>
/// Result containing the tenant's daily usage snapshots over the requested period.
/// </summary>
/// <param name="TenantId">The identifier of the tenant.</param>
/// <param name="FromDate">The start date of the history range.</param>
/// <param name="ToDate">The end date of the history range.</param>
/// <param name="Snapshots">The daily usage snapshots ordered by date descending.</param>
public sealed record UsageHistoryResult(
    Guid TenantId,
    DateOnly FromDate,
    DateOnly ToDate,
    IReadOnlyList<UsageSnapshotResult> Snapshots);

/// <summary>
/// Represents a single daily usage snapshot in the usage history.
/// </summary>
/// <param name="SnapshotDate">The date of the snapshot.</param>
/// <param name="RoomsCreated">Number of rooms created on this date.</param>
/// <param name="RoomsCreatedMonth">Cumulative rooms created in the month up to this date.</param>
/// <param name="PeakParticipants">Peak concurrent participants on this date.</param>
/// <param name="StorageUsedGB">Storage used in gigabytes.</param>
/// <param name="RecordingHoursUsed">Recording hours used in the current month.</param>
public sealed record UsageSnapshotResult(
    DateOnly SnapshotDate,
    int RoomsCreated,
    int RoomsCreatedMonth,
    int PeakParticipants,
    decimal StorageUsedGB,
    decimal RecordingHoursUsed);

/// <summary>
/// Handles <see cref="GetUsageHistoryQuery"/> by querying the
/// <c>TenantUsageSnapshots</c> table for daily snapshots within the requested range.
/// </summary>
public sealed class GetUsageHistoryQueryHandler : IRequestHandler<GetUsageHistoryQuery, UsageHistoryResult>
{
    private readonly TenancyDbContext _dbContext;

    /// <summary>
    /// Initializes a new instance of <see cref="GetUsageHistoryQueryHandler"/>.
    /// </summary>
    /// <param name="dbContext">The Tenancy module database context.</param>
    public GetUsageHistoryQueryHandler(TenancyDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    /// <summary>
    /// Handles the query by retrieving daily usage snapshots for the specified
    /// tenant within the requested date range (clamped to 1-90 days).
    /// </summary>
    /// <param name="request">The usage history query.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>The usage history result with daily snapshots.</returns>
    public async Task<UsageHistoryResult> Handle(GetUsageHistoryQuery request, CancellationToken cancellationToken)
    {
        var days = Math.Clamp(request.Days, 1, 90);
        var today = DateOnly.FromDateTime(DateTime.UtcNow);
        var fromDate = today.AddDays(-(days - 1));

        var snapshots = await _dbContext.TenantUsageSnapshots
            .AsNoTracking()
            .Where(s => s.TenantId == request.TenantId && s.SnapshotDate >= fromDate && s.SnapshotDate <= today)
            .OrderByDescending(s => s.SnapshotDate)
            .Select(s => new UsageSnapshotResult(
                SnapshotDate: s.SnapshotDate,
                RoomsCreated: s.RoomsCreated,
                RoomsCreatedMonth: s.RoomsCreatedMonth,
                PeakParticipants: s.PeakParticipants,
                StorageUsedGB: s.StorageUsedGB,
                RecordingHoursUsed: s.RecordingHoursUsed))
            .ToListAsync(cancellationToken);

        return new UsageHistoryResult(
            TenantId: request.TenantId,
            FromDate: fromDate,
            ToDate: today,
            Snapshots: snapshots);
    }
}
