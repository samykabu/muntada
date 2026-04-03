namespace Muntada.Identity.Application.Services;

/// <summary>
/// Service for generating and validating JWT access tokens and opaque refresh tokens.
/// </summary>
public interface ITokenService
{
    /// <summary>
    /// Generates a JWT access token for the given user and tenant context.
    /// </summary>
    /// <param name="userId">The user's opaque identifier.</param>
    /// <param name="tenantId">The tenant context (nullable for pre-tenant selection).</param>
    /// <param name="scopes">The permissions granted in this token.</param>
    /// <returns>The signed JWT string.</returns>
    string GenerateAccessToken(string userId, string? tenantId, IEnumerable<string> scopes);

    /// <summary>
    /// Generates an opaque refresh token string (plaintext — caller must hash before storage).
    /// </summary>
    /// <returns>A cryptographically secure opaque token string.</returns>
    string GenerateRefreshToken();

    /// <summary>
    /// Validates a JWT access token and extracts claims.
    /// </summary>
    /// <param name="token">The JWT string.</param>
    /// <returns>The extracted claims if valid; null if invalid or expired.</returns>
    TokenClaims? ValidateAccessToken(string token);
}

/// <summary>
/// Represents the claims extracted from a validated JWT access token.
/// </summary>
/// <param name="UserId">The user's opaque identifier (sub claim).</param>
/// <param name="TenantId">The tenant context (aud claim).</param>
/// <param name="Scopes">The granted permissions (scope claim).</param>
/// <param name="ExpiresAt">The token expiration time.</param>
public sealed record TokenClaims(
    string UserId,
    string? TenantId,
    IReadOnlyList<string> Scopes,
    DateTimeOffset ExpiresAt);
