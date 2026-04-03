namespace Muntada.Identity.Api.Dtos;

/// <summary>
/// Request DTO for user registration.
/// </summary>
/// <param name="Email">The email address to register with.</param>
/// <param name="Password">The plaintext password.</param>
/// <param name="ConfirmPassword">Must match <paramref name="Password"/>.</param>
public sealed record RegisterRequest(string Email, string Password, string ConfirmPassword);
