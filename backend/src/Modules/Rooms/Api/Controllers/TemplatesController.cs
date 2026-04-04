using MediatR;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Muntada.Rooms.Api.Dtos;
using Muntada.Rooms.Application.Commands;
using Muntada.Rooms.Application.Queries;
using Muntada.Rooms.Domain.Template;

namespace Muntada.Rooms.Api.Controllers;

/// <summary>
/// REST API controller for room template management.
/// Requires Admin or Owner tenant role for all operations.
/// </summary>
[ApiController]
[Route("api/v1/tenants/{tenantId}/room-templates")]
public class TemplatesController : ControllerBase
{
    private readonly ISender _sender;

    /// <summary>
    /// Initializes a new instance of the <see cref="TemplatesController"/> class.
    /// </summary>
    public TemplatesController(ISender sender)
    {
        _sender = sender;
    }

    /// <summary>
    /// Creates a new room template.
    /// </summary>
    /// <param name="tenantId">The tenant identifier.</param>
    /// <param name="request">The template creation request.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>The created template.</returns>
    [HttpPost]
    [ProducesResponseType(typeof(RoomTemplateResponse), StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    public async Task<IActionResult> CreateTemplate(
        [FromRoute] string tenantId,
        [FromBody] CreateRoomTemplateRequest request,
        CancellationToken cancellationToken)
    {
        var userId = GetCurrentUserId();

        var command = new CreateRoomTemplateCommand(
            tenantId,
            request.Name,
            request.Description,
            request.Settings.MaxParticipants,
            request.Settings.AllowGuestAccess,
            request.Settings.AllowRecording,
            request.Settings.AllowTranscription,
            request.Settings.DefaultTranscriptionLanguage,
            request.Settings.AutoStartRecording,
            userId);

        var template = await _sender.Send(command, cancellationToken);

        return CreatedAtAction(
            nameof(GetTemplate),
            new { tenantId, templateId = template.Id.Value },
            MapToResponse(template));
    }

    /// <summary>
    /// Gets a single room template by ID.
    /// </summary>
    [HttpGet("{templateId}")]
    [ProducesResponseType(typeof(RoomTemplateResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetTemplate(
        [FromRoute] string tenantId,
        [FromRoute] string templateId,
        CancellationToken cancellationToken)
    {
        var query = new GetRoomTemplateQuery(tenantId, templateId);
        var template = await _sender.Send(query, cancellationToken);
        return Ok(MapToResponse(template));
    }

    /// <summary>
    /// Lists room templates for a tenant with pagination.
    /// </summary>
    [HttpGet]
    [ProducesResponseType(typeof(PagedResponse<RoomTemplateResponse>), StatusCodes.Status200OK)]
    public async Task<IActionResult> ListTemplates(
        [FromRoute] string tenantId,
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 20,
        CancellationToken cancellationToken = default)
    {
        var query = new ListRoomTemplatesQuery(tenantId, page, pageSize);
        var result = await _sender.Send(query, cancellationToken);

        var response = new PagedResponse<RoomTemplateResponse>(
            result.Items.Select(MapToResponse).ToList(),
            result.TotalCount,
            result.Page,
            result.PageSize);

        return Ok(response);
    }

    /// <summary>
    /// Updates a room template (all fields except name).
    /// </summary>
    [HttpPatch("{templateId}")]
    [ProducesResponseType(typeof(RoomTemplateResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> UpdateTemplate(
        [FromRoute] string tenantId,
        [FromRoute] string templateId,
        [FromBody] UpdateRoomTemplateRequest request,
        CancellationToken cancellationToken)
    {
        var command = new UpdateRoomTemplateCommand(
            tenantId,
            templateId,
            request.Description,
            request.Settings.MaxParticipants,
            request.Settings.AllowGuestAccess,
            request.Settings.AllowRecording,
            request.Settings.AllowTranscription,
            request.Settings.DefaultTranscriptionLanguage,
            request.Settings.AutoStartRecording);

        var template = await _sender.Send(command, cancellationToken);
        return Ok(MapToResponse(template));
    }

    private static RoomTemplateResponse MapToResponse(RoomTemplate template)
    {
        return new RoomTemplateResponse(
            template.Id.Value,
            template.TenantId,
            template.Name,
            template.Description,
            new RoomSettingsDto(
                template.Settings.MaxParticipants,
                template.Settings.AllowGuestAccess,
                template.Settings.AllowRecording,
                template.Settings.AllowTranscription,
                template.Settings.DefaultTranscriptionLanguage,
                template.Settings.AutoStartRecording),
            template.CreatedBy,
            template.CreatedAt,
            template.UpdatedAt);
    }

    private string GetCurrentUserId()
    {
        return User?.FindFirst("sub")?.Value
            ?? User?.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value
            ?? "anonymous";
    }
}
