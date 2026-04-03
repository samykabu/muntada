namespace Muntada.Identity.Api.Dtos;

/// <summary>
/// Request DTO for resending a verification email.
/// </summary>
/// <param name="Email">The email address to resend the verification to.</param>
public sealed record ResendVerificationRequest(string Email);
