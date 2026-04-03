namespace Muntada.Identity.Api.Dtos;

/// <summary>
/// Response DTO returned after successful user login.
/// </summary>
/// <param name="AccessToken">The signed JWT access token.</param>
/// <param name="ExpiresIn">The token lifetime in seconds.</param>
/// <param name="TokenType">The token type (always "Bearer").</param>
/// <param name="UserId">The unique identifier of the authenticated user.</param>
public sealed record LoginResponse(string AccessToken, int ExpiresIn, string TokenType, string UserId);
