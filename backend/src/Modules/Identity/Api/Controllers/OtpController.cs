using MediatR;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Muntada.Identity.Api.Dtos;
using Muntada.Identity.Application.Commands;

namespace Muntada.Identity.Api.Controllers;

/// <summary>
/// Handles phone OTP authentication operations: generating challenges
/// and verifying OTP codes.
/// </summary>
[ApiController]
[Route("api/v1/identity/auth/otp")]
public sealed class OtpController : ControllerBase
{
    private readonly ISender _sender;

    /// <summary>
    /// Initializes a new instance of <see cref="OtpController"/>.
    /// </summary>
    /// <param name="sender">MediatR sender for dispatching commands.</param>
    public OtpController(ISender sender)
    {
        _sender = sender;
    }

    /// <summary>
    /// Generates a new OTP challenge and sends the code via SMS to the specified phone number.
    /// </summary>
    /// <param name="request">The challenge request containing the phone number.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>200 OK with the challenge ID for subsequent verification.</returns>
    [HttpPost("challenge")]
    [ProducesResponseType(typeof(OtpChallengeResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> GenerateChallenge(
        [FromBody] OtpChallengeRequest request,
        CancellationToken cancellationToken)
    {
        var command = new GenerateOtpChallengeCommand(request.PhoneNumber);
        var result = await _sender.Send(command, cancellationToken);

        return Ok(new OtpChallengeResponse(result.ChallengeId));
    }

    /// <summary>
    /// Verifies an OTP code against a previously generated challenge.
    /// On success, creates a session and returns an access token.
    /// </summary>
    /// <param name="request">The verification request containing the challenge ID and code.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>200 OK with the login result containing access token and user ID.</returns>
    [HttpPost("verify")]
    [ProducesResponseType(typeof(LoginResult), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> VerifyOtp(
        [FromBody] OtpVerifyRequest request,
        CancellationToken cancellationToken)
    {
        var userAgent = HttpContext.Request.Headers.UserAgent.ToString();
        var ipAddress = HttpContext.Connection.RemoteIpAddress?.ToString() ?? "unknown";

        var command = new VerifyOtpCommand(request.ChallengeId, request.Code, userAgent, ipAddress);
        var result = await _sender.Send(command, cancellationToken);

        return Ok(result);
    }
}
