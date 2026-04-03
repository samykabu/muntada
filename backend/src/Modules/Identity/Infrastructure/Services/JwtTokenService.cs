using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Security.Cryptography;
using Microsoft.Extensions.Configuration;
using Microsoft.IdentityModel.Tokens;
using Muntada.Identity.Application.Services;

namespace Muntada.Identity.Infrastructure.Services;

/// <summary>
/// JWT token service using HS256 (symmetric) with key ID (kid) rotation support.
/// Access tokens are short-lived (15 minutes default, configurable).
/// Refresh tokens are opaque cryptographically secure strings.
/// </summary>
public sealed class JwtTokenService : ITokenService
{
    private readonly string _issuer;
    private readonly int _accessTokenLifetimeMinutes;
    private readonly SigningCredentials _signingCredentials;
    private readonly TokenValidationParameters _validationParameters;

    /// <summary>
    /// Initializes a new instance of <see cref="JwtTokenService"/>.
    /// </summary>
    /// <param name="configuration">Application configuration.</param>
    public JwtTokenService(IConfiguration configuration)
    {
        _issuer = configuration["Jwt:Issuer"] ?? "muntada";
        _accessTokenLifetimeMinutes = int.Parse(configuration["Jwt:AccessTokenLifetimeMinutes"] ?? "15");

        var secretKey = configuration["Jwt:SecretKey"] ?? GenerateDefaultKey();
        var securityKey = new SymmetricSecurityKey(Convert.FromBase64String(secretKey));
        var kid = configuration["Jwt:KeyId"] ?? "default-key-1";

        _signingCredentials = new SigningCredentials(securityKey, SecurityAlgorithms.HmacSha256)
        {
            Key = { KeyId = kid }
        };

        _validationParameters = new TokenValidationParameters
        {
            ValidateIssuer = true,
            ValidIssuer = _issuer,
            ValidateAudience = false,
            ValidateLifetime = true,
            IssuerSigningKey = securityKey,
            ClockSkew = TimeSpan.FromSeconds(30)
        };
    }

    /// <inheritdoc />
    public string GenerateAccessToken(string userId, string? tenantId, IEnumerable<string> scopes)
    {
        var claims = new List<Claim>
        {
            new(JwtRegisteredClaimNames.Sub, userId),
            new(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString()),
            new(JwtRegisteredClaimNames.Iat, DateTimeOffset.UtcNow.ToUnixTimeSeconds().ToString(), ClaimValueTypes.Integer64),
        };

        if (tenantId is not null)
            claims.Add(new Claim(JwtRegisteredClaimNames.Aud, tenantId));

        foreach (var scope in scopes)
            claims.Add(new Claim("scope", scope));

        var token = new JwtSecurityToken(
            issuer: _issuer,
            claims: claims,
            notBefore: DateTime.UtcNow,
            expires: DateTime.UtcNow.AddMinutes(_accessTokenLifetimeMinutes),
            signingCredentials: _signingCredentials);

        return new JwtSecurityTokenHandler().WriteToken(token);
    }

    /// <inheritdoc />
    public string GenerateRefreshToken()
    {
        var bytes = new byte[32];
        RandomNumberGenerator.Fill(bytes);
        return Convert.ToBase64String(bytes);
    }

    /// <inheritdoc />
    public TokenClaims? ValidateAccessToken(string token)
    {
        try
        {
            var handler = new JwtSecurityTokenHandler();
            var principal = handler.ValidateToken(token, _validationParameters, out var validatedToken);

            var userId = principal.FindFirst(JwtRegisteredClaimNames.Sub)?.Value
                ?? principal.FindFirst(ClaimTypes.NameIdentifier)?.Value;

            if (userId is null) return null;

            var tenantId = principal.FindFirst(JwtRegisteredClaimNames.Aud)?.Value;
            var scopes = principal.FindAll("scope").Select(c => c.Value).ToList();
            var expiresAt = validatedToken.ValidTo;

            return new TokenClaims(userId, tenantId, scopes, new DateTimeOffset(expiresAt, TimeSpan.Zero));
        }
        catch
        {
            return null;
        }
    }

    private static string GenerateDefaultKey()
    {
        var bytes = new byte[64];
        RandomNumberGenerator.Fill(bytes);
        return Convert.ToBase64String(bytes);
    }
}
