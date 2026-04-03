namespace Muntada.Identity.Api.Dtos;

/// <summary>
/// Request DTO for initiating a password reset.
/// </summary>
/// <param name="Email">The email address to send the password reset link to.</param>
public sealed record ForgotPasswordRequest(string Email);
