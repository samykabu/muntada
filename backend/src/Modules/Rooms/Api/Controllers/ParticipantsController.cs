using MediatR;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Muntada.Rooms.Api.Dtos;
using Muntada.Rooms.Api.Filters;
using Muntada.Rooms.Application.Queries;

namespace Muntada.Rooms.Api.Controllers;

/// <summary>
/// REST API controller for querying room participants and analytics.
/// For live rooms, participant data comes from Redis; for ended rooms, from SQL.
/// </summary>
[ApiController]
[Route("api/v1/tenants/{tenantId}/room-occurrences/{occurrenceId}")]
[ServiceFilter(typeof(RoomTenantValidationFilter))]
public class ParticipantsController : ControllerBase
{
    private readonly ISender _sender;

    /// <summary>
    /// Initializes a new instance of the <see cref="ParticipantsController"/> class.
    /// </summary>
    public ParticipantsController(ISender sender)
    {
        _sender = sender;
    }

    /// <summary>
    /// Lists participants for a room occurrence.
    /// Returns real-time data from Redis for live rooms, or historical data from SQL for ended rooms.
    /// </summary>
    /// <param name="tenantId">The tenant identifier.</param>
    /// <param name="occurrenceId">The room occurrence identifier.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>The list of participants.</returns>
    [HttpGet("participants")]
    [ProducesResponseType(typeof(List<ParticipantResponse>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> ListParticipants(
        [FromRoute] string tenantId,
        [FromRoute] string occurrenceId,
        CancellationToken cancellationToken)
    {
        var query = new ListParticipantsQuery(occurrenceId, tenantId);
        var participants = await _sender.Send(query, cancellationToken);

        var items = participants.Select(p => new ParticipantResponse(
            p.ParticipantId,
            p.UserId,
            p.DisplayName,
            p.Role.ToString(),
            p.AudioState.ToString(),
            p.VideoState.ToString(),
            p.JoinedAt,
            p.LeftAt)).ToList();

        return Ok(new { participants = items, totalCount = items.Count });
    }

    /// <summary>
    /// Gets analytics for a room occurrence including total participants,
    /// peak concurrency, dwell times, and media participation rates.
    /// </summary>
    /// <param name="tenantId">The tenant identifier.</param>
    /// <param name="occurrenceId">The room occurrence identifier.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>The room analytics data.</returns>
    [HttpGet("analytics")]
    [ProducesResponseType(typeof(RoomAnalyticsResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetAnalytics(
        [FromRoute] string tenantId,
        [FromRoute] string occurrenceId,
        CancellationToken cancellationToken)
    {
        var query = new GetRoomAnalyticsQuery(occurrenceId, tenantId);
        var analytics = await _sender.Send(query, cancellationToken);

        var response = new RoomAnalyticsResponse(
            analytics.TotalParticipants,
            analytics.PeakConcurrent,
            analytics.AverageTimeDwellSeconds,
            analytics.AudioParticipationRate,
            analytics.VideoParticipationRate,
            analytics.DwellTimes.Select(d => new ParticipantDwellTimeDto(
                d.ParticipantId,
                d.DisplayName,
                d.DwellTimeSeconds)).ToList());

        return Ok(response);
    }
}
