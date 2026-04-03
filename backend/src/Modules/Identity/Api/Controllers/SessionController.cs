using MediatR;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Muntada.Identity.Api.Dtos;
using Muntada.Identity.Application.Commands;
using Muntada.Identity.Application.Queries;

namespace Muntada.Identity.Api.Controllers;

/// <summary>
/// Handles session management operations: token refresh, logout,
/// listing sessions, and revoking sessions.
/// </summary>
[ApiController]
[Route("api/v1/identity")]
public sealed class SessionController : ControllerBase
{
    private readonly ISender _sender;

    /// <summary>
    /// Initializes a new instance of <see cref="SessionController"/>.
    /// </summary>
    /// <param name="sender">MediatR sender for dispatching commands and queries.</param>
    public SessionController(ISender sender)
    {
        _sender = sender;
    }

    /// <summary>
    /// Refreshes an access token using a valid refresh token.
    /// </summary>
    /// <param name="request">The refresh token request containing the refresh token value.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>200 OK with a new access token.</returns>
    [HttpPost("auth/refresh")]
    [ProducesResponseType(typeof(RefreshTokenResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<IActionResult> RefreshToken(
        [FromBody] RefreshTokenRequest request,
        CancellationToken cancellationToken)
    {
        var userAgent = HttpContext.Request.Headers.UserAgent.ToString();
        var ipAddress = HttpContext.Connection.RemoteIpAddress?.ToString() ?? "unknown";

        var command = new RefreshTokenCommand(request.RefreshToken, userAgent, ipAddress);
        var result = await _sender.Send(command, cancellationToken);

        return Ok(new RefreshTokenResponse(result.AccessToken, ExpiresIn: 3600, TokenType: "Bearer"));
    }

    /// <summary>
    /// Logs out the current user by revoking their session.
    /// </summary>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>200 OK if logout succeeded.</returns>
    [HttpPost("auth/logout")]
    [ProducesResponseType(typeof(bool), StatusCodes.Status200OK)]
    public async Task<IActionResult> Logout(CancellationToken cancellationToken)
    {
        // TODO: Extract SessionId and UserId from authenticated user claims
        var sessionId = Guid.Empty;
        var userId = Guid.Empty;

        var command = new LogoutCommand(sessionId, userId);
        var result = await _sender.Send(command, cancellationToken);
        return Ok(result);
    }

    /// <summary>
    /// Lists all active sessions for the authenticated user.
    /// </summary>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>200 OK with a list of session DTOs.</returns>
    [HttpGet("sessions")]
    [ProducesResponseType(typeof(List<SessionResponse>), StatusCodes.Status200OK)]
    public async Task<IActionResult> ListSessions(CancellationToken cancellationToken)
    {
        // TODO: Extract UserId from authenticated user claims
        var userId = Guid.Empty;

        var query = new ListSessionsQuery(userId);
        var sessions = await _sender.Send(query, cancellationToken);

        var response = sessions.Select(s => new SessionResponse(
            s.SessionId,
            s.DeviceUserAgent,
            s.DeviceIpAddress,
            DeviceCountry: null,
            s.CreatedAt,
            s.LastActivityAt,
            s.IsCurrent)).ToList();

        return Ok(response);
    }

    /// <summary>
    /// Revokes a specific session by ID, or revokes all other sessions
    /// when the <paramref name="exceptCurrent"/> query parameter is true.
    /// </summary>
    /// <param name="id">The session ID to revoke.</param>
    /// <param name="exceptCurrent">When true, revokes all sessions except the current one.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>200 OK with the result.</returns>
    [HttpDelete("sessions/{id:guid}")]
    [ProducesResponseType(typeof(bool), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> RevokeSession(
        Guid id,
        CancellationToken cancellationToken)
    {
        // TODO: Extract UserId from authenticated user claims
        var userId = Guid.Empty;

        var command = new RevokeSessionCommand(id, userId);
        var result = await _sender.Send(command, cancellationToken);

        if (!result)
            return NotFound();

        return Ok(result);
    }

    /// <summary>
    /// Revokes all sessions except the current one.
    /// </summary>
    /// <param name="exceptCurrent">Must be true to invoke this endpoint.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>200 OK with the number of revoked sessions.</returns>
    [HttpDelete("sessions")]
    [ProducesResponseType(typeof(int), StatusCodes.Status200OK)]
    public async Task<IActionResult> RevokeAllOtherSessions(
        [FromQuery] bool exceptCurrent,
        CancellationToken cancellationToken)
    {
        if (!exceptCurrent)
            return BadRequest("The exceptCurrent query parameter must be true.");

        // TODO: Extract SessionId and UserId from authenticated user claims
        var currentSessionId = Guid.Empty;
        var userId = Guid.Empty;

        var command = new RevokeAllOtherSessionsCommand(currentSessionId, userId);
        var count = await _sender.Send(command, cancellationToken);
        return Ok(count);
    }
}
