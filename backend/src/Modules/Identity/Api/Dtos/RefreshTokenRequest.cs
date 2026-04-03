namespace Muntada.Identity.Api.Dtos;

/// <summary>
/// Request DTO for refreshing an access token.
/// </summary>
/// <param name="RefreshToken">The plaintext refresh token value.</param>
public sealed record RefreshTokenRequest(string RefreshToken);
