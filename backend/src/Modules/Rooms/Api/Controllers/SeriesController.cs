using MediatR;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Muntada.Rooms.Api.Dtos;
using Muntada.Rooms.Api.Filters;
using Muntada.Rooms.Application.Commands;
using Muntada.Rooms.Application.Queries;
using Muntada.Rooms.Domain.Series;

namespace Muntada.Rooms.Api.Controllers;

/// <summary>
/// REST API controller for recurring room series management.
/// Supports creating, updating, listing, and ending series.
/// </summary>
[ApiController]
[Route("api/v1/tenants/{tenantId}/room-series")]
[ServiceFilter(typeof(RoomTenantValidationFilter))]
public class SeriesController : ControllerBase
{
    private readonly ISender _sender;

    /// <summary>
    /// Initializes a new instance of the <see cref="SeriesController"/> class.
    /// </summary>
    public SeriesController(ISender sender)
    {
        _sender = sender;
    }

    /// <summary>
    /// Creates a new recurring room series with initial occurrence generation.
    /// </summary>
    /// <param name="tenantId">The tenant identifier.</param>
    /// <param name="request">The series creation request.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>The created series.</returns>
    [HttpPost]
    [ProducesResponseType(typeof(RoomSeriesResponse), StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> CreateSeries(
        [FromRoute] string tenantId,
        [FromBody] CreateRoomSeriesRequest request,
        CancellationToken cancellationToken)
    {
        var userId = GetCurrentUserId();

        var command = new CreateRoomSeriesCommand(
            tenantId,
            request.TemplateId,
            request.Title,
            request.RecurrenceRule,
            request.OrganizerTimeZoneId,
            request.StartsAt,
            request.EndsAt,
            request.ModeratorUserId,
            userId);

        var series = await _sender.Send(command, cancellationToken);

        return CreatedAtAction(
            nameof(GetSeries),
            new { tenantId, seriesId = series.Id.Value },
            MapToResponse(series));
    }

    /// <summary>
    /// Gets a single room series by ID.
    /// </summary>
    [HttpGet("{seriesId}")]
    [ProducesResponseType(typeof(RoomSeriesResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetSeries(
        [FromRoute] string tenantId,
        [FromRoute] string seriesId,
        CancellationToken cancellationToken)
    {
        var query = new GetRoomSeriesQuery(tenantId, seriesId);
        var series = await _sender.Send(query, cancellationToken);
        return Ok(MapToResponse(series));
    }

    /// <summary>
    /// Lists room series for a tenant with pagination.
    /// </summary>
    [HttpGet]
    [ProducesResponseType(typeof(PagedResponse<RoomSeriesResponse>), StatusCodes.Status200OK)]
    public async Task<IActionResult> ListSeries(
        [FromRoute] string tenantId,
        [FromQuery] RoomSeriesStatus? status = null,
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 20,
        CancellationToken cancellationToken = default)
    {
        var query = new ListRoomSeriesQuery(tenantId, status, page, pageSize);
        var result = await _sender.Send(query, cancellationToken);

        var response = new PagedResponse<RoomSeriesResponse>(
            result.Items.Select(MapToResponse).ToList(),
            result.TotalCount,
            result.Page,
            result.PageSize);

        return Ok(response);
    }

    /// <summary>
    /// Updates a room series recurrence pattern and regenerates future occurrences.
    /// </summary>
    [HttpPatch("{seriesId}")]
    [ProducesResponseType(typeof(RoomSeriesResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> UpdateSeries(
        [FromRoute] string tenantId,
        [FromRoute] string seriesId,
        [FromBody] UpdateRoomSeriesRequest request,
        CancellationToken cancellationToken)
    {
        var command = new UpdateRoomSeriesCommand(
            tenantId,
            seriesId,
            request.RecurrenceRule,
            request.EndsAt,
            request.ModeratorUserId);

        var series = await _sender.Send(command, cancellationToken);
        return Ok(MapToResponse(series));
    }

    /// <summary>
    /// Ends a room series, preventing further occurrence generation.
    /// </summary>
    [HttpPost("{seriesId}/end")]
    [ProducesResponseType(typeof(RoomSeriesResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> EndSeries(
        [FromRoute] string tenantId,
        [FromRoute] string seriesId,
        CancellationToken cancellationToken)
    {
        var command = new EndRoomSeriesCommand(tenantId, seriesId);
        var series = await _sender.Send(command, cancellationToken);
        return Ok(MapToResponse(series));
    }

    private static RoomSeriesResponse MapToResponse(RoomSeries series)
    {
        return new RoomSeriesResponse(
            series.Id.Value,
            series.TenantId,
            series.TemplateId.Value,
            series.Title,
            series.RecurrenceRule,
            series.OrganizerTimeZoneId,
            series.StartsAt,
            series.EndsAt,
            series.Status.ToString(),
            series.CreatedBy,
            series.CreatedAt,
            series.UpdatedAt);
    }

    private string GetCurrentUserId()
    {
        return User?.FindFirst("sub")?.Value
            ?? User?.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value
            ?? "anonymous";
    }
}
