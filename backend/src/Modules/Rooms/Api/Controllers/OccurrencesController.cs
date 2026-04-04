using MediatR;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Muntada.Rooms.Api.Dtos;
using Muntada.Rooms.Api.Filters;
using Muntada.Rooms.Application.Commands;
using Muntada.Rooms.Application.Queries;
using Muntada.Rooms.Domain.Occurrence;

namespace Muntada.Rooms.Api.Controllers;

/// <summary>
/// REST API controller for room occurrence management.
/// Supports creating standalone rooms, listing with filters, and single-occurrence overrides.
/// </summary>
/// <remarks>
/// TODO: All PATCH/POST endpoints require Admin/Owner authorization check via middleware.
/// </remarks>
[ApiController]
[Route("api/v1/tenants/{tenantId}/room-occurrences")]
[ServiceFilter(typeof(RoomTenantValidationFilter))]
public class OccurrencesController : ControllerBase
{
    private readonly ISender _sender;

    /// <summary>
    /// Initializes a new instance of the <see cref="OccurrencesController"/> class.
    /// </summary>
    public OccurrencesController(ISender sender)
    {
        _sender = sender;
    }

    /// <summary>
    /// Creates a new standalone room occurrence.
    /// </summary>
    /// <param name="tenantId">The tenant identifier.</param>
    /// <param name="request">The occurrence creation request.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>The created occurrence.</returns>
    [HttpPost]
    [ProducesResponseType(typeof(RoomOccurrenceResponse), StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> CreateOccurrence(
        [FromRoute] string tenantId,
        [FromBody] CreateRoomOccurrenceRequest request,
        CancellationToken cancellationToken)
    {
        var userId = GetCurrentUserId();

        var command = new CreateRoomOccurrenceCommand(
            tenantId,
            request.Title,
            request.ScheduledAt,
            request.OrganizerTimeZoneId,
            request.ModeratorUserId,
            request.Settings.MaxParticipants,
            request.Settings.AllowGuestAccess,
            request.Settings.AllowRecording,
            request.Settings.AllowTranscription,
            request.Settings.DefaultTranscriptionLanguage,
            request.Settings.AutoStartRecording,
            request.GracePeriodSeconds,
            userId);

        var occurrence = await _sender.Send(command, cancellationToken);

        return CreatedAtAction(
            nameof(GetOccurrence),
            new { tenantId, occurrenceId = occurrence.Id.Value },
            MapToResponse(occurrence));
    }

    /// <summary>
    /// Gets a single room occurrence by ID.
    /// </summary>
    [HttpGet("{occurrenceId}")]
    [ProducesResponseType(typeof(RoomOccurrenceResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetOccurrence(
        [FromRoute] string tenantId,
        [FromRoute] string occurrenceId,
        CancellationToken cancellationToken)
    {
        var query = new GetRoomOccurrenceQuery(tenantId, occurrenceId);
        var occurrence = await _sender.Send(query, cancellationToken);
        return Ok(MapToResponse(occurrence));
    }

    /// <summary>
    /// Lists room occurrences for a tenant with optional filters and pagination.
    /// </summary>
    [HttpGet]
    [ProducesResponseType(typeof(PagedResponse<RoomOccurrenceResponse>), StatusCodes.Status200OK)]
    public async Task<IActionResult> ListOccurrences(
        [FromRoute] string tenantId,
        [FromQuery] DateTimeOffset? fromDate = null,
        [FromQuery] DateTimeOffset? toDate = null,
        [FromQuery] RoomOccurrenceStatus? status = null,
        [FromQuery] string? seriesId = null,
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 20,
        CancellationToken cancellationToken = default)
    {
        var query = new ListRoomOccurrencesQuery(tenantId, fromDate, toDate, status, seriesId, page, pageSize);
        var result = await _sender.Send(query, cancellationToken);

        var response = new PagedResponse<RoomOccurrenceResponse>(
            result.Items.Select(MapToResponse).ToList(),
            result.TotalCount,
            result.Page,
            result.PageSize);

        return Ok(response);
    }

    /// <summary>
    /// Updates a room occurrence (single-occurrence override for title, settings, or cancellation).
    /// </summary>
    [HttpPatch("{occurrenceId}")]
    [ProducesResponseType(typeof(RoomOccurrenceResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> UpdateOccurrence(
        [FromRoute] string tenantId,
        [FromRoute] string occurrenceId,
        [FromBody] UpdateRoomOccurrenceRequest request,
        CancellationToken cancellationToken)
    {
        var command = new UpdateRoomOccurrenceCommand(
            tenantId,
            occurrenceId,
            request.Title,
            request.Settings?.MaxParticipants,
            request.Settings?.AllowGuestAccess,
            request.Settings?.AllowRecording,
            request.Settings?.AllowTranscription,
            request.Settings?.DefaultTranscriptionLanguage,
            request.Settings?.AutoStartRecording,
            request.Settings is not null,
            request.IsCancelled);

        var occurrence = await _sender.Send(command, cancellationToken);
        return Ok(MapToResponse(occurrence));
    }

    /// <summary>
    /// Assigns or changes the moderator for a room occurrence.
    /// Room must be in Draft or Scheduled status.
    /// </summary>
    /// <param name="tenantId">The tenant identifier.</param>
    /// <param name="occurrenceId">The room occurrence identifier.</param>
    /// <param name="request">The moderator assignment request.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>The updated occurrence.</returns>
    [HttpPatch("{occurrenceId}/moderator")]
    [ProducesResponseType(typeof(RoomOccurrenceResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> AssignModerator(
        [FromRoute] string tenantId,
        [FromRoute] string occurrenceId,
        [FromBody] AssignModeratorRequest request,
        CancellationToken cancellationToken)
    {
        var command = new AssignModeratorCommand(occurrenceId, tenantId, request.ModeratorUserId);
        var occurrence = await _sender.Send(command, cancellationToken);
        return Ok(MapToResponse(occurrence));
    }

    /// <summary>
    /// Hands over moderator control to another user during the Grace period.
    /// Transitions the room from Grace back to Live.
    /// </summary>
    /// <param name="tenantId">The tenant identifier.</param>
    /// <param name="occurrenceId">The room occurrence identifier.</param>
    /// <param name="request">The handover request.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>The updated occurrence.</returns>
    [HttpPost("{occurrenceId}/moderator/handover")]
    [ProducesResponseType(typeof(RoomOccurrenceResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> HandoverModerator(
        [FromRoute] string tenantId,
        [FromRoute] string occurrenceId,
        [FromBody] HandoverModeratorRequest request,
        CancellationToken cancellationToken)
    {
        var command = new HandoverModeratorCommand(occurrenceId, tenantId, request.ToUserId);
        var occurrence = await _sender.Send(command, cancellationToken);
        return Ok(MapToResponse(occurrence));
    }

    private static RoomOccurrenceResponse MapToResponse(RoomOccurrence occurrence)
    {
        ModeratorDto? moderatorDto = null;
        if (occurrence.ModeratorAssignment is not null)
        {
            moderatorDto = new ModeratorDto(
                occurrence.ModeratorAssignment.UserId,
                occurrence.ModeratorAssignment.AssignedAt,
                occurrence.ModeratorAssignment.DisconnectedAt);
        }

        return new RoomOccurrenceResponse(
            occurrence.Id.Value,
            occurrence.TenantId,
            occurrence.RoomSeriesId?.Value,
            occurrence.Title,
            occurrence.ScheduledAt,
            occurrence.OrganizerTimeZoneId,
            occurrence.LiveStartedAt,
            occurrence.LiveEndedAt,
            occurrence.Status.ToString(),
            moderatorDto,
            new RoomSettingsDto(
                occurrence.Settings.MaxParticipants,
                occurrence.Settings.AllowGuestAccess,
                occurrence.Settings.AllowRecording,
                occurrence.Settings.AllowTranscription,
                occurrence.Settings.DefaultTranscriptionLanguage,
                occurrence.Settings.AutoStartRecording),
            occurrence.GracePeriodSeconds,
            occurrence.GraceStartedAt,
            occurrence.IsCancelled,
            occurrence.CreatedBy,
            occurrence.CreatedAt,
            occurrence.UpdatedAt);
    }

    private string GetCurrentUserId()
    {
        return User?.FindFirst("sub")?.Value
            ?? User?.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value
            ?? "anonymous";
    }
}
