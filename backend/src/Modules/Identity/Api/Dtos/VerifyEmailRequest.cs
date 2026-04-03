namespace Muntada.Identity.Api.Dtos;

/// <summary>
/// Request DTO for email verification.
/// </summary>
/// <param name="Token">The plaintext verification token received via email.</param>
public sealed record VerifyEmailRequest(string Token);
