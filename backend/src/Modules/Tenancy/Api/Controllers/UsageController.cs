using MediatR;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Muntada.Tenancy.Api.Dtos;
using Muntada.Tenancy.Application.Queries;

namespace Muntada.Tenancy.Api.Controllers;

/// <summary>
/// Handles tenant usage tracking and reporting operations:
/// retrieving current usage metrics and historical usage snapshots.
/// </summary>
[ApiController]
[Route("api/v1")]
public sealed class UsageController : ControllerBase
{
    private readonly ISender _sender;

    /// <summary>
    /// Initializes a new instance of <see cref="UsageController"/>.
    /// </summary>
    /// <param name="sender">MediatR sender for dispatching queries.</param>
    public UsageController(ISender sender)
    {
        _sender = sender;
    }

    /// <summary>
    /// Retrieves the current usage metrics for a specific tenant,
    /// including percentage used and threshold status for each resource.
    /// </summary>
    /// <param name="tenantId">The identifier of the tenant.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>200 OK with usage metrics, or 404 Not Found if no active plan exists.</returns>
    [HttpGet("tenants/{tenantId:guid}/usage")]
    [ProducesResponseType(typeof(UsageResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetTenantUsage(
        Guid tenantId,
        CancellationToken cancellationToken)
    {
        var result = await _sender.Send(new GetTenantUsageQuery(tenantId), cancellationToken);

        if (result is null)
            return NotFound();

        var response = new UsageResponse(
            TenantId: result.TenantId,
            PlanName: result.PlanName,
            Metrics: result.Metrics.Select(m => new UsageMetricResponse(
                Resource: m.Resource,
                Current: m.Current,
                Limit: m.Limit,
                Unit: m.Unit,
                PercentUsed: m.PercentUsed,
                ThresholdStatus: m.ThresholdStatus)).ToList());

        return Ok(response);
    }

    /// <summary>
    /// Retrieves daily usage snapshots for a tenant over a specified number of days.
    /// Defaults to 30 days, maximum 90 days.
    /// </summary>
    /// <param name="tenantId">The identifier of the tenant.</param>
    /// <param name="days">The number of days of history to return (default 30, max 90).</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>200 OK with the daily usage snapshots.</returns>
    [HttpGet("tenants/{tenantId:guid}/usage/history")]
    [ProducesResponseType(typeof(UsageHistoryResponse), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetUsageHistory(
        Guid tenantId,
        [FromQuery] int days = 30,
        CancellationToken cancellationToken = default)
    {
        var result = await _sender.Send(new GetUsageHistoryQuery(tenantId, days), cancellationToken);

        var response = new UsageHistoryResponse(
            TenantId: result.TenantId,
            FromDate: result.FromDate,
            ToDate: result.ToDate,
            Snapshots: result.Snapshots.Select(s => new UsageSnapshotResponse(
                SnapshotDate: s.SnapshotDate,
                RoomsCreated: s.RoomsCreated,
                RoomsCreatedMonth: s.RoomsCreatedMonth,
                PeakParticipants: s.PeakParticipants,
                StorageUsedGB: s.StorageUsedGB,
                RecordingHoursUsed: s.RecordingHoursUsed)).ToList());

        return Ok(response);
    }
}
