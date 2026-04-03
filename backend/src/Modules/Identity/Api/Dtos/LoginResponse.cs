namespace Muntada.Identity.Api.Dtos;

/// <summary>
/// Response DTO returned after successful user login.
/// The refresh token is not included here — it is set as an HTTP-only cookie.
/// </summary>
/// <param name="AccessToken">The signed JWT access token.</param>
/// <param name="ExpiresIn">The token lifetime in seconds.</param>
/// <param name="TokenType">The token type (always "Bearer").</param>
/// <param name="UserId">The unique identifier of the authenticated user.</param>
public sealed record LoginResponse(string AccessToken, int ExpiresIn, string TokenType, string UserId);
