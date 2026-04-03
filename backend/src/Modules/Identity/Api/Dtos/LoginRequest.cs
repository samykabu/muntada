namespace Muntada.Identity.Api.Dtos;

/// <summary>
/// Request DTO for user login with email and password.
/// </summary>
/// <param name="Email">The email address to authenticate with.</param>
/// <param name="Password">The plaintext password.</param>
public sealed record LoginRequest(string Email, string Password);
