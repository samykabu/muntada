using MediatR;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Muntada.Identity.Api.Dtos;
using Muntada.Identity.Application.Commands;

namespace Muntada.Identity.Api.Controllers;

/// <summary>
/// Handles authentication-related operations: registration, email verification,
/// and resending verification emails.
/// </summary>
[ApiController]
[Route("api/v1/identity/auth")]
public sealed class AuthController : ControllerBase
{
    private readonly ISender _sender;

    /// <summary>
    /// Initializes a new instance of <see cref="AuthController"/>.
    /// </summary>
    /// <param name="sender">MediatR sender for dispatching commands.</param>
    public AuthController(ISender sender)
    {
        _sender = sender;
    }

    /// <summary>
    /// Registers a new user with email and password credentials.
    /// A verification email is sent upon successful registration.
    /// </summary>
    /// <param name="request">The registration request containing email and password.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>201 Created with the new user's ID and email.</returns>
    [HttpPost("register")]
    [ProducesResponseType(typeof(RegisterUserResult), StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> Register(
        [FromBody] RegisterRequest request,
        CancellationToken cancellationToken)
    {
        var command = new RegisterUserCommand(request.Email, request.Password, request.ConfirmPassword);
        var result = await _sender.Send(command, cancellationToken);
        return StatusCode(StatusCodes.Status201Created, result);
    }

    /// <summary>
    /// Verifies a user's email address using the token received via email.
    /// </summary>
    /// <param name="request">The verification request containing the token.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>200 OK with a boolean indicating success.</returns>
    [HttpPost("verify-email")]
    [ProducesResponseType(typeof(bool), StatusCodes.Status200OK)]
    public async Task<IActionResult> VerifyEmail(
        [FromBody] VerifyEmailRequest request,
        CancellationToken cancellationToken)
    {
        var command = new VerifyEmailCommand(request.Token);
        var result = await _sender.Send(command, cancellationToken);
        return Ok(result);
    }

    /// <summary>
    /// Resends a verification email to the specified address.
    /// Always returns success to prevent email enumeration.
    /// </summary>
    /// <param name="request">The resend request containing the email address.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>200 OK with a boolean (always true).</returns>
    [HttpPost("resend-verification")]
    [ProducesResponseType(typeof(bool), StatusCodes.Status200OK)]
    public async Task<IActionResult> ResendVerification(
        [FromBody] ResendVerificationRequest request,
        CancellationToken cancellationToken)
    {
        var command = new ResendVerificationCommand(request.Email);
        var result = await _sender.Send(command, cancellationToken);
        return Ok(result);
    }
}
