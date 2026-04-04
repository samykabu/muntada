using MediatR;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Muntada.Rooms.Api.Dtos;
using Muntada.Rooms.Application.Commands;
using Muntada.Rooms.Application.Queries;
using Muntada.Rooms.Domain.Invite;

namespace Muntada.Rooms.Api.Controllers;

/// <summary>
/// REST API controller for managing room invitations.
/// Supports creating, listing, revoking invites, and joining rooms via invite token.
/// </summary>
[ApiController]
[Route("api/v1/tenants/{tenantId}/room-occurrences/{occurrenceId}/invites")]
public class InvitesController : ControllerBase
{
    private readonly ISender _sender;

    /// <summary>
    /// Initializes a new instance of the <see cref="InvitesController"/> class.
    /// </summary>
    public InvitesController(ISender sender)
    {
        _sender = sender;
    }

    /// <summary>
    /// Creates one or more invites for a room occurrence.
    /// </summary>
    /// <param name="tenantId">The tenant identifier.</param>
    /// <param name="occurrenceId">The room occurrence identifier.</param>
    /// <param name="request">The batch invite creation request.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>The list of created invites.</returns>
    [HttpPost]
    [ProducesResponseType(typeof(List<RoomInviteResponse>), StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> CreateInvites(
        [FromRoute] string tenantId,
        [FromRoute] string occurrenceId,
        [FromBody] CreateRoomInvitesRequest request,
        CancellationToken cancellationToken)
    {
        var userId = GetCurrentUserId();

        var inviteRequests = request.Invites
            .Select(i => new InviteRequest(
                i.Email,
                i.UserId,
                Enum.Parse<RoomInviteType>(i.InviteType, ignoreCase: true)))
            .ToList();

        var command = new GenerateRoomInviteCommand(
            occurrenceId,
            tenantId,
            inviteRequests,
            userId);

        var invites = await _sender.Send(command, cancellationToken);

        var response = invites.Select(MapToResponse).ToList();
        return StatusCode(StatusCodes.Status201Created, response);
    }

    /// <summary>
    /// Lists invites for a room occurrence with optional status filter and pagination.
    /// </summary>
    [HttpGet]
    [ProducesResponseType(typeof(PagedResponse<RoomInviteResponse>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> ListInvites(
        [FromRoute] string tenantId,
        [FromRoute] string occurrenceId,
        [FromQuery] RoomInviteStatus? status = null,
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 20,
        CancellationToken cancellationToken = default)
    {
        var query = new ListRoomInvitesQuery(occurrenceId, tenantId, status, page, pageSize);
        var result = await _sender.Send(query, cancellationToken);

        var response = new PagedResponse<RoomInviteResponse>(
            result.Items.Select(MapToResponse).ToList(),
            result.TotalCount,
            result.Page,
            result.PageSize);

        return Ok(response);
    }

    /// <summary>
    /// Revokes a pending invite, invalidating its token immediately.
    /// </summary>
    /// <param name="tenantId">The tenant identifier.</param>
    /// <param name="occurrenceId">The room occurrence identifier (unused, included for route consistency).</param>
    /// <param name="inviteId">The invite identifier to revoke.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    [HttpDelete("{inviteId}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> RevokeInvite(
        [FromRoute] string tenantId,
        [FromRoute] string occurrenceId,
        [FromRoute] string inviteId,
        CancellationToken cancellationToken)
    {
        var command = new RevokeRoomInviteCommand(inviteId, tenantId);
        await _sender.Send(command, cancellationToken);
        return NoContent();
    }

    /// <summary>
    /// Joins a room occurrence using an invite token.
    /// </summary>
    /// <param name="tenantId">The tenant identifier.</param>
    /// <param name="occurrenceId">The room occurrence identifier.</param>
    /// <param name="request">The join request containing the invite token.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>The join response with participant details.</returns>
    [HttpPost("join")]
    [ProducesResponseType(typeof(JoinRoomResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> JoinRoom(
        [FromRoute] string tenantId,
        [FromRoute] string occurrenceId,
        [FromBody] JoinRoomRequest request,
        CancellationToken cancellationToken)
    {
        var command = new JoinRoomCommand(
            occurrenceId,
            request.Token,
            request.UserId,
            request.DisplayName);

        var result = await _sender.Send(command, cancellationToken);

        var response = new JoinRoomResponse(
            result.ParticipantState.Id.Value,
            result.Occurrence.Id.Value,
            result.ParticipantState.DisplayName,
            result.ParticipantState.Role.ToString(),
            result.Occurrence.Status.ToString());

        return Ok(response);
    }

    private static RoomInviteResponse MapToResponse(RoomInvite invite)
    {
        return new RoomInviteResponse(
            invite.Id.Value,
            invite.RoomOccurrenceId.Value,
            invite.InvitedEmail,
            invite.InvitedUserId,
            invite.InviteToken,
            invite.Status.ToString(),
            invite.InviteType.ToString(),
            invite.InvitedBy,
            invite.CreatedAt,
            invite.ExpiresAt);
    }

    private string GetCurrentUserId()
    {
        return User?.FindFirst("sub")?.Value
            ?? User?.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value
            ?? "anonymous";
    }
}
