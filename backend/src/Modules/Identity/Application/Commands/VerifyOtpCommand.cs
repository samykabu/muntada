using System.Security.Cryptography;
using System.Text;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Muntada.Identity.Application.Services;
using Muntada.Identity.Domain.Events;
using Muntada.Identity.Domain.Otp;
using Muntada.Identity.Domain.Session;
using Muntada.Identity.Domain.User;
using Muntada.Identity.Infrastructure;
using Muntada.SharedKernel.Application;

namespace Muntada.Identity.Application.Commands;

/// <summary>
/// Command to verify an OTP code against a previously issued challenge.
/// </summary>
/// <param name="ChallengeId">The unique identifier of the OTP challenge.</param>
/// <param name="Code">The 6-digit OTP code entered by the user.</param>
/// <param name="UserAgent">The User-Agent header from the client request.</param>
/// <param name="IpAddress">The IP address of the client.</param>
public sealed record VerifyOtpCommand(Guid ChallengeId, string Code, string UserAgent, string IpAddress)
    : IRequest<LoginResult>;

/// <summary>
/// Handles <see cref="VerifyOtpCommand"/> by verifying the OTP code against the stored hash,
/// creating a session (same pattern as login), and publishing a <see cref="UserLoggedInEvent"/>.
/// </summary>
public sealed class VerifyOtpCommandHandler : IRequestHandler<VerifyOtpCommand, LoginResult>
{
    private static readonly TimeSpan SessionLifetime = TimeSpan.FromDays(30);

    private readonly IdentityDbContext _dbContext;
    private readonly ITokenService _tokenService;
    private readonly IIntegrationEventPublisher _eventPublisher;

    /// <summary>
    /// Initializes a new instance of <see cref="VerifyOtpCommandHandler"/>.
    /// </summary>
    /// <param name="dbContext">The Identity module database context.</param>
    /// <param name="tokenService">Service for generating JWT and refresh tokens.</param>
    /// <param name="eventPublisher">Publisher for integration events.</param>
    public VerifyOtpCommandHandler(
        IdentityDbContext dbContext,
        ITokenService tokenService,
        IIntegrationEventPublisher eventPublisher)
    {
        _dbContext = dbContext;
        _tokenService = tokenService;
        _eventPublisher = eventPublisher;
    }

    /// <summary>
    /// Handles OTP verification: finds the challenge, hashes the incoming code with SHA-256,
    /// compares hashes, checks attempt count, and on success creates a session and access token.
    /// </summary>
    /// <param name="request">The verify OTP command.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>A login result containing the access token and user ID.</returns>
    /// <exception cref="FluentValidation.ValidationException">
    /// Thrown with a generic message when the OTP is invalid (FR-018).
    /// </exception>
    public async Task<LoginResult> Handle(VerifyOtpCommand request, CancellationToken cancellationToken)
    {
        var challenge = await _dbContext.Set<OtpChallenge>()
            .FirstOrDefaultAsync(c => c.Id == request.ChallengeId, cancellationToken);

        if (challenge is null || !challenge.IsValid())
        {
            throw new FluentValidation.ValidationException(
                [new FluentValidation.Results.ValidationFailure("", "Invalid or expired verification code.")]);
        }

        // Hash incoming code with SHA-256 and compare
        var codeHash = Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(request.Code)));

        if (!string.Equals(codeHash, challenge.CodeHash, StringComparison.OrdinalIgnoreCase))
        {
            challenge.IncrementFailedAttempts();
            await _dbContext.SaveChangesAsync(cancellationToken);

            throw new FluentValidation.ValidationException(
                [new FluentValidation.Results.ValidationFailure("", "Invalid or expired verification code.")]);
        }

        // Mark challenge as verified
        challenge.MarkVerified();

        // Find user by phone number
        var user = await _dbContext.Set<User>()
            .FirstOrDefaultAsync(u => u.PhoneNumber != null && u.PhoneNumber.Value == challenge.PhoneNumber, cancellationToken);

        if (user is null || user.Status != UserStatus.Active)
        {
            throw new FluentValidation.ValidationException(
                [new FluentValidation.Results.ValidationFailure("", "Invalid or expired verification code.")]);
        }

        // Create session (same pattern as LoginCommandHandler)
        var refreshTokenPlaintext = _tokenService.GenerateRefreshToken();
        var refreshTokenHash = BCrypt.Net.BCrypt.HashPassword(refreshTokenPlaintext);

        var refreshToken = RefreshToken.Create(Guid.Empty, refreshTokenHash, SessionLifetime);
        var deviceInfo = new DeviceInfo(request.UserAgent, request.IpAddress);
        var session = Session.Create(user.Id, deviceInfo, refreshToken.Id, SessionLifetime);

        refreshToken = RefreshToken.Create(session.Id, refreshTokenHash, SessionLifetime);

        var accessToken = _tokenService.GenerateAccessToken(user.Id.ToString(), tenantId: null, scopes: []);

        user.RecordLogin();

        _dbContext.Set<Session>().Add(session);
        _dbContext.Set<RefreshToken>().Add(refreshToken);
        await _dbContext.SaveChangesAsync(cancellationToken);

        // Publish integration event
        var @event = UserLoggedInEvent.Create(user.Id, session.Id);
        await _eventPublisher.PublishAsync(@event, cancellationToken);

        return new LoginResult(accessToken, user.Id.ToString());
    }
}
