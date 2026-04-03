namespace Muntada.Identity.Api.Dtos;

/// <summary>
/// Response DTO for a successful token refresh.
/// </summary>
/// <param name="AccessToken">The newly generated signed JWT access token.</param>
/// <param name="ExpiresIn">The number of seconds until the access token expires.</param>
/// <param name="TokenType">The token type (always "Bearer").</param>
public sealed record RefreshTokenResponse(string AccessToken, int ExpiresIn, string TokenType);
