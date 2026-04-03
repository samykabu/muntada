using System.Security.Cryptography;
using System.Text;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Muntada.Identity.Domain.Events;
using Muntada.Identity.Domain.PasswordReset;
using Muntada.Identity.Domain.Session;
using Muntada.Identity.Domain.User;
using Muntada.Identity.Infrastructure;
using Muntada.SharedKernel.Application;

namespace Muntada.Identity.Application.Commands;

/// <summary>
/// Command to reset a user's password using a valid reset token.
/// </summary>
/// <param name="Token">The plaintext password reset token received via email.</param>
/// <param name="NewPassword">The new password to set.</param>
/// <param name="ConfirmNewPassword">Confirmation of the new password (must match).</param>
public sealed record ResetPasswordCommand(string Token, string NewPassword, string ConfirmNewPassword)
    : IRequest<bool>;

/// <summary>
/// Handles <see cref="ResetPasswordCommand"/> by validating the token, updating the user's password,
/// marking the token as used, revoking all other sessions, and publishing a <see cref="PasswordChangedEvent"/>.
/// </summary>
public sealed class ResetPasswordCommandHandler : IRequestHandler<ResetPasswordCommand, bool>
{
    private readonly IdentityDbContext _dbContext;
    private readonly IIntegrationEventPublisher _eventPublisher;

    /// <summary>
    /// Initializes a new instance of <see cref="ResetPasswordCommandHandler"/>.
    /// </summary>
    /// <param name="dbContext">The Identity module database context.</param>
    /// <param name="eventPublisher">Publisher for integration events.</param>
    public ResetPasswordCommandHandler(
        IdentityDbContext dbContext,
        IIntegrationEventPublisher eventPublisher)
    {
        _dbContext = dbContext;
        _eventPublisher = eventPublisher;
    }

    /// <summary>
    /// Handles password reset: hashes the incoming token with SHA-256, finds the reset token,
    /// validates it, updates the user's password, marks the token as used, revokes all active
    /// sessions, and publishes a <see cref="PasswordChangedEvent"/>.
    /// </summary>
    /// <param name="request">The reset password command.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns><c>true</c> if the password was reset; <c>false</c> if the token is invalid.</returns>
    /// <exception cref="FluentValidation.ValidationException">
    /// Thrown with a generic message when the token is invalid or passwords do not match (FR-018).
    /// </exception>
    public async Task<bool> Handle(ResetPasswordCommand request, CancellationToken cancellationToken)
    {
        if (request.NewPassword != request.ConfirmNewPassword)
        {
            throw new FluentValidation.ValidationException(
                [new FluentValidation.Results.ValidationFailure("ConfirmNewPassword", "Passwords do not match.")]);
        }

        // Hash incoming token to match stored hash
        var tokenHash = Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(request.Token)));

        var resetToken = await _dbContext.Set<PasswordResetToken>()
            .FirstOrDefaultAsync(t => t.TokenHash == tokenHash, cancellationToken);

        if (resetToken is null || !resetToken.IsValid())
        {
            throw new FluentValidation.ValidationException(
                [new FluentValidation.Results.ValidationFailure("", "Invalid or expired reset token.")]);
        }

        // Find the user
        var user = await _dbContext.Set<User>()
            .FirstOrDefaultAsync(u => u.Id == resetToken.UserId, cancellationToken);

        if (user is null)
        {
            throw new FluentValidation.ValidationException(
                [new FluentValidation.Results.ValidationFailure("", "Invalid or expired reset token.")]);
        }

        // Update user password
        var newPasswordHash = PasswordHash.Create(request.NewPassword);
        user.ChangePassword(newPasswordHash);

        // Mark reset token as used
        resetToken.MarkUsed();

        // Revoke all active sessions for this user
        var activeSessions = await _dbContext.Set<Session>()
            .Where(s => s.UserId == user.Id && s.Status == SessionStatus.Active)
            .ToListAsync(cancellationToken);

        foreach (var session in activeSessions)
        {
            session.Revoke();
        }

        // Revoke all active refresh tokens for revoked sessions
        var sessionIds = activeSessions.Select(s => s.Id).ToList();
        var refreshTokens = await _dbContext.Set<RefreshToken>()
            .Where(rt => sessionIds.Contains(rt.SessionId) && rt.Status == RefreshTokenStatus.Active)
            .ToListAsync(cancellationToken);

        foreach (var token in refreshTokens)
        {
            token.Revoke();
        }

        await _dbContext.SaveChangesAsync(cancellationToken);

        // Publish integration event
        var @event = PasswordChangedEvent.Create(user.Id);
        await _eventPublisher.PublishAsync(@event, cancellationToken);

        return true;
    }
}
