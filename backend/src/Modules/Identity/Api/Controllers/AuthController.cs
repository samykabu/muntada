using MediatR;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Muntada.Identity.Api.Dtos;
using Muntada.Identity.Application.Commands;

namespace Muntada.Identity.Api.Controllers;

/// <summary>
/// Handles authentication-related operations: registration, email verification,
/// login, password reset, and resending verification emails.
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
    /// Authenticates a user with email and password credentials.
    /// On success, returns a JWT access token and sets a refresh token as an HTTP-only cookie.
    /// </summary>
    /// <param name="request">The login request containing email and password.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>200 OK with the access token, expiry, token type, and user ID.</returns>
    [HttpPost("login")]
    [ProducesResponseType(typeof(LoginResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> Login(
        [FromBody] LoginRequest request,
        CancellationToken cancellationToken)
    {
        var userAgent = Request.Headers.UserAgent.ToString();
        var ipAddress = HttpContext.Connection.RemoteIpAddress?.ToString() ?? "unknown";

        var command = new LoginCommand(request.Email, request.Password, userAgent, ipAddress);
        var result = await _sender.Send(command, cancellationToken);

        // Set refresh token as HTTP-only cookie
        Response.Cookies.Append("refresh_token", result.RefreshToken, new CookieOptions
        {
            HttpOnly = true,
            Secure = Request.IsHttps,
            SameSite = SameSiteMode.Strict,
            Path = "/api/v1/identity/auth",
            MaxAge = TimeSpan.FromDays(30),
        });

        var response = new LoginResponse(
            AccessToken: result.AccessToken,
            ExpiresIn: 900, // 15 minutes in seconds
            TokenType: "Bearer",
            UserId: result.UserId);

        return Ok(response);
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

    /// <summary>
    /// Initiates a password reset by sending a reset link to the user's email.
    /// Always returns success to prevent email enumeration (FR-018).
    /// </summary>
    /// <param name="request">The forgot password request containing the email address.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>200 OK with a boolean (always true).</returns>
    [HttpPost("forgot-password")]
    [ProducesResponseType(typeof(bool), StatusCodes.Status200OK)]
    public async Task<IActionResult> ForgotPassword(
        [FromBody] ForgotPasswordRequest request,
        CancellationToken cancellationToken)
    {
        var command = new ForgotPasswordCommand(request.Email);
        var result = await _sender.Send(command, cancellationToken);
        return Ok(result);
    }

    /// <summary>
    /// Resets a user's password using a valid reset token received via email.
    /// Revokes all active sessions on success.
    /// </summary>
    /// <param name="request">The reset password request containing the token and new password.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>200 OK with a boolean indicating success.</returns>
    [HttpPost("reset-password")]
    [ProducesResponseType(typeof(bool), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> ResetPassword(
        [FromBody] ResetPasswordRequest request,
        CancellationToken cancellationToken)
    {
        var command = new ResetPasswordCommand(request.Token, request.NewPassword, request.ConfirmNewPassword);
        var result = await _sender.Send(command, cancellationToken);
        return Ok(result);
    }
}
