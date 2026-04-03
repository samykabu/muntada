using System.Security.Claims;
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
    /// Refreshes an access token using the refresh token stored in the HTTP-only cookie.
    /// </summary>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>200 OK with a new access token.</returns>
    [HttpPost("auth/refresh")]
    [ProducesResponseType(typeof(RefreshTokenResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<IActionResult> RefreshToken(
        CancellationToken cancellationToken)
    {
        var refreshTokenValue = Request.Cookies["refresh_token"];
        if (string.IsNullOrEmpty(refreshTokenValue))
            return Unauthorized();

        var userAgent = HttpContext.Request.Headers.UserAgent.ToString();
        var ipAddress = HttpContext.Connection.RemoteIpAddress?.ToString() ?? "unknown";

        var command = new RefreshTokenCommand(refreshTokenValue, userAgent, ipAddress);
        var result = await _sender.Send(command, cancellationToken);

        return Ok(new RefreshTokenResponse(result.AccessToken, ExpiresIn: 900, TokenType: "Bearer"));
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
        var userId = GetUserId();
        // SessionId may be included as a claim in the JWT; fallback to Guid.Empty if absent
        var sessionIdClaim = User.FindFirst("sid")?.Value;
        var sessionId = sessionIdClaim is not null ? Guid.Parse(sessionIdClaim) : Guid.Empty;

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
        var userId = GetUserId();

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
    /// Revokes a specific session by its ID.
    /// </summary>
    /// <param name="id">The session ID to revoke.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>200 OK with the result.</returns>
    [HttpDelete("sessions/{id:guid}")]
    [ProducesResponseType(typeof(bool), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> RevokeSession(
        Guid id,
        CancellationToken cancellationToken)
    {
        var userId = GetUserId();

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

        var userId = GetUserId();
        var sessionIdClaim = User.FindFirst("sid")?.Value;
        var currentSessionId = sessionIdClaim is not null ? Guid.Parse(sessionIdClaim) : Guid.Empty;

        var command = new RevokeAllOtherSessionsCommand(currentSessionId, userId);
        var count = await _sender.Send(command, cancellationToken);
        return Ok(count);
    }

    /// <summary>
    /// Extracts the authenticated user's ID from JWT claims.
    /// </summary>
    private Guid GetUserId() =>
        Guid.Parse(
            User.FindFirst(ClaimTypes.NameIdentifier)?.Value
            ?? User.FindFirst("sub")?.Value
            ?? throw new UnauthorizedAccessException());
}
