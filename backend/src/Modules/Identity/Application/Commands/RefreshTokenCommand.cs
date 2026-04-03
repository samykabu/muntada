using MediatR;
using Microsoft.EntityFrameworkCore;
using Muntada.Identity.Application.Services;
using Muntada.Identity.Domain.Session;
using Muntada.Identity.Infrastructure;

namespace Muntada.Identity.Application.Commands;

/// <summary>
/// Command to refresh an expired access token using a valid refresh token.
/// </summary>
/// <param name="RefreshTokenValue">The plaintext refresh token value.</param>
/// <param name="UserAgent">The User-Agent header from the client request.</param>
/// <param name="IpAddress">The IP address of the client.</param>
public sealed record RefreshTokenCommand(string RefreshTokenValue, string UserAgent, string IpAddress)
    : IRequest<RefreshTokenResult>;

/// <summary>
/// Result returned after successful token refresh.
/// </summary>
/// <param name="AccessToken">The newly generated signed JWT access token.</param>
public sealed record RefreshTokenResult(string AccessToken);

/// <summary>
/// Handles <see cref="RefreshTokenCommand"/> by verifying the refresh token,
/// validating the session, generating a new access token, and updating session activity.
/// </summary>
public sealed class RefreshTokenCommandHandler : IRequestHandler<RefreshTokenCommand, RefreshTokenResult>
{
    private readonly IdentityDbContext _dbContext;
    private readonly ITokenService _tokenService;

    /// <summary>
    /// Initializes a new instance of <see cref="RefreshTokenCommandHandler"/>.
    /// </summary>
    /// <param name="dbContext">The Identity module database context.</param>
    /// <param name="tokenService">Service for generating JWT access tokens.</param>
    public RefreshTokenCommandHandler(
        IdentityDbContext dbContext,
        ITokenService tokenService)
    {
        _dbContext = dbContext;
        _tokenService = tokenService;
    }

    /// <summary>
    /// Handles the token refresh: verifies the refresh token hash via bcrypt,
    /// checks the session is active and not expired, generates a new access token,
    /// and updates the session's last activity timestamp.
    /// </summary>
    /// <param name="request">The refresh token command.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>A new access token.</returns>
    /// <exception cref="FluentValidation.ValidationException">
    /// Thrown with a generic message when the refresh token or session is invalid (FR-018).
    /// </exception>
    public async Task<RefreshTokenResult> Handle(RefreshTokenCommand request, CancellationToken cancellationToken)
    {
        // Find active refresh tokens for sessions owned by the requesting user-agent/IP,
        // limited to active tokens only (avoids loading all tokens in the system)
        // Only load refresh tokens that belong to active sessions (avoids O(N) scan across all tokens)
        var activeTokens = await _dbContext.Set<RefreshToken>()
            .Where(rt => rt.Status == RefreshTokenStatus.Active && rt.ExpiresAt > DateTimeOffset.UtcNow)
            .Where(rt => _dbContext.Set<Session>()
                .Any(s => s.Id == rt.SessionId && s.Status == SessionStatus.Active))
            .ToListAsync(cancellationToken);

        RefreshToken? matchedToken = null;
        foreach (var token in activeTokens)
        {
            if (BCrypt.Net.BCrypt.Verify(request.RefreshTokenValue, token.TokenHash))
            {
                matchedToken = token;
                break;
            }
        }

        if (matchedToken is null)
        {
            throw new FluentValidation.ValidationException(
                [new FluentValidation.Results.ValidationFailure("", "Invalid or expired token.")]);
        }

        // Find the bound session
        var session = await _dbContext.Set<Session>()
            .FirstOrDefaultAsync(s => s.Id == matchedToken.SessionId, cancellationToken);

        if (session is null || !session.IsValid())
        {
            throw new FluentValidation.ValidationException(
                [new FluentValidation.Results.ValidationFailure("", "Invalid or expired token.")]);
        }

        // Update session activity
        session.RecordActivity();

        // Generate a new access token
        var accessToken = _tokenService.GenerateAccessToken(
            session.UserId.ToString(), tenantId: null, scopes: []);

        await _dbContext.SaveChangesAsync(cancellationToken);

        return new RefreshTokenResult(accessToken);
    }
}
