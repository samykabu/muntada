using MediatR;
using Microsoft.EntityFrameworkCore;
using Muntada.Identity.Application.Services;
using Muntada.Identity.Domain.Events;
using Muntada.Identity.Domain.Session;
using Muntada.Identity.Domain.User;
using Muntada.Identity.Infrastructure;
using Muntada.SharedKernel.Application;

namespace Muntada.Identity.Application.Commands;

/// <summary>
/// Handles <see cref="LoginCommand"/> by verifying credentials, creating a session
/// and refresh token, generating a JWT access token, and publishing a login event.
/// </summary>
public sealed class LoginCommandHandler : IRequestHandler<LoginCommand, LoginResult>
{
    private static readonly TimeSpan SessionLifetime = TimeSpan.FromDays(30);

    private readonly IdentityDbContext _dbContext;
    private readonly ITokenService _tokenService;
    private readonly IIntegrationEventPublisher _eventPublisher;

    /// <summary>
    /// Initializes a new instance of <see cref="LoginCommandHandler"/>.
    /// </summary>
    /// <param name="dbContext">The Identity module database context.</param>
    /// <param name="tokenService">Service for generating JWT and refresh tokens.</param>
    /// <param name="eventPublisher">Publisher for integration events.</param>
    public LoginCommandHandler(
        IdentityDbContext dbContext,
        ITokenService tokenService,
        IIntegrationEventPublisher eventPublisher)
    {
        _dbContext = dbContext;
        _tokenService = tokenService;
        _eventPublisher = eventPublisher;
    }

    /// <summary>
    /// Handles user login: validates credentials, creates session and refresh token,
    /// generates a JWT access token, and publishes the <see cref="UserLoggedInEvent"/>.
    /// </summary>
    /// <param name="request">The login command.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>The login result containing the access token and user ID.</returns>
    /// <exception cref="FluentValidation.ValidationException">
    /// Thrown with a generic message when credentials are invalid or account is not active (FR-018).
    /// </exception>
    public async Task<LoginResult> Handle(LoginCommand request, CancellationToken cancellationToken)
    {
        // Normalize email
        var normalizedEmail = request.Email.Trim().ToLowerInvariant();

        // Find user by email (include owned Email VO)
        var user = await _dbContext.Set<User>()
            .FirstOrDefaultAsync(u => u.Email.Value == normalizedEmail, cancellationToken);

        // Generic error to prevent email enumeration (FR-018)
        if (user is null || !user.PasswordHash.Verify(request.Password))
        {
            throw new FluentValidation.ValidationException(
                [new FluentValidation.Results.ValidationFailure("", "Invalid email or password.")]);
        }

        // Verify user is active
        if (user.Status != UserStatus.Active)
        {
            throw new FluentValidation.ValidationException(
                [new FluentValidation.Results.ValidationFailure("", "Account is not active.")]);
        }

        // Generate refresh token plaintext and hash it
        var refreshTokenPlaintext = _tokenService.GenerateRefreshToken();
        var refreshTokenHash = BCrypt.Net.BCrypt.HashPassword(refreshTokenPlaintext);

        // Create refresh token entity (initially unbound)
        var refreshToken = RefreshToken.Create(Guid.Empty, refreshTokenHash, SessionLifetime);

        // Create device info and session
        var deviceInfo = new DeviceInfo(request.UserAgent, request.IpAddress);
        var session = Session.Create(user.Id, deviceInfo, refreshToken.Id, SessionLifetime);

        // Bind the refresh token to the session
        refreshToken.BindToSession(session.Id);

        // Generate JWT access token
        var accessToken = _tokenService.GenerateAccessToken(user.Id.ToString(), tenantId: null, scopes: []);

        // Record login on user aggregate
        user.RecordLogin();

        // Persist session and refresh token
        _dbContext.Set<Session>().Add(session);
        _dbContext.Set<RefreshToken>().Add(refreshToken);
        await _dbContext.SaveChangesAsync(cancellationToken);

        // Publish integration event
        var @event = UserLoggedInEvent.Create(user.Id, session.Id);
        await _eventPublisher.PublishAsync(@event, cancellationToken);

        return new LoginResult(accessToken, refreshTokenPlaintext, user.Id.ToString());
    }
}
