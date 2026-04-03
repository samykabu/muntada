using MediatR;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Muntada.Identity.Api.Dtos;
using Muntada.Identity.Application.Commands;
using Muntada.Identity.Application.Queries;

namespace Muntada.Identity.Api.Controllers;

/// <summary>
/// Handles guest magic link operations: creating, validating, and revoking magic links.
/// </summary>
[ApiController]
[Route("api/v1/identity/magic-links")]
public sealed class MagicLinkController : ControllerBase
{
    private readonly ISender _sender;

    /// <summary>
    /// Initializes a new instance of <see cref="MagicLinkController"/>.
    /// </summary>
    /// <param name="sender">MediatR sender for dispatching commands and queries.</param>
    public MagicLinkController(ISender sender)
    {
        _sender = sender;
    }

    /// <summary>
    /// Creates a new guest magic link for anonymous room access.
    /// </summary>
    /// <param name="request">The request containing the room occurrence ID.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>201 Created with the magic link details including the plaintext token.</returns>
    [HttpPost]
    [ProducesResponseType(typeof(MagicLinkDto), StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> CreateMagicLink(
        [FromBody] CreateMagicLinkRequest request,
        CancellationToken cancellationToken)
    {
        // TODO: Extract UserId from authenticated user claims
        var userId = Guid.Empty;

        var command = new GenerateGuestMagicLinkCommand(request.RoomOccurrenceId, userId);
        var result = await _sender.Send(command, cancellationToken);

        var response = new MagicLinkDto(result.LinkId, result.Token);
        return StatusCode(StatusCodes.Status201Created, response);
    }

    /// <summary>
    /// Validates a guest magic link token and creates a guest session.
    /// </summary>
    /// <param name="token">The plaintext magic link token from the URL.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>200 OK with the guest session details.</returns>
    [HttpGet("validate")]
    [ProducesResponseType(typeof(GuestSessionDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> ValidateMagicLink(
        [FromQuery] string token,
        CancellationToken cancellationToken)
    {
        var userAgent = HttpContext.Request.Headers.UserAgent.ToString();
        var ipAddress = HttpContext.Connection.RemoteIpAddress?.ToString() ?? "unknown";

        var query = new ValidateGuestMagicLinkQuery(token, userAgent, ipAddress);
        var result = await _sender.Send(query, cancellationToken);

        var response = new GuestSessionDto(result.GuestSessionId, result.RoomOccurrenceId);
        return Ok(response);
    }

    /// <summary>
    /// Revokes a guest magic link by ID, preventing further use.
    /// </summary>
    /// <param name="id">The unique identifier of the magic link to revoke.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>200 OK if revoked; 404 Not Found if the link does not exist or is not owned.</returns>
    [HttpDelete("{id:guid}")]
    [ProducesResponseType(typeof(bool), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> RevokeMagicLink(
        Guid id,
        CancellationToken cancellationToken)
    {
        // TODO: Extract UserId from authenticated user claims
        var userId = Guid.Empty;

        var command = new RevokeMagicLinkCommand(id, userId);
        var result = await _sender.Send(command, cancellationToken);

        if (!result)
            return NotFound();

        return Ok(result);
    }
}
