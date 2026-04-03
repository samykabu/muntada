using System.Security.Cryptography;
using System.Text;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Muntada.Identity.Domain.EmailVerification;
using Muntada.Identity.Domain.Events;
using Muntada.Identity.Domain.User;
using Muntada.Identity.Infrastructure;
using Muntada.SharedKernel.Application;

namespace Muntada.Identity.Application.Commands;

/// <summary>
/// Command to verify a user's email address using a verification token.
/// </summary>
/// <param name="Token">The plaintext verification token received via email.</param>
public sealed record VerifyEmailCommand(string Token) : IRequest<bool>;

/// <summary>
/// Handles <see cref="VerifyEmailCommand"/> by validating the token,
/// activating the user, and publishing a verification event.
/// </summary>
public sealed class VerifyEmailCommandHandler : IRequestHandler<VerifyEmailCommand, bool>
{
    private readonly IdentityDbContext _dbContext;
    private readonly IIntegrationEventPublisher _eventPublisher;

    /// <summary>
    /// Initializes a new instance of <see cref="VerifyEmailCommandHandler"/>.
    /// </summary>
    /// <param name="dbContext">The Identity module database context.</param>
    /// <param name="eventPublisher">Publisher for integration events.</param>
    public VerifyEmailCommandHandler(
        IdentityDbContext dbContext,
        IIntegrationEventPublisher eventPublisher)
    {
        _dbContext = dbContext;
        _eventPublisher = eventPublisher;
    }

    /// <summary>
    /// Handles email verification: hashes the incoming token, looks up the verification record,
    /// marks it as used, activates the user, and publishes <see cref="UserEmailVerifiedEvent"/>.
    /// </summary>
    /// <param name="request">The verify email command.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns><c>true</c> if verification succeeded; <c>false</c> if the token is invalid or expired.</returns>
    public async Task<bool> Handle(VerifyEmailCommand request, CancellationToken cancellationToken)
    {
        // Hash the incoming token to match the stored hash
        var tokenHash = Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(request.Token)));

        var verificationToken = await _dbContext.Set<EmailVerificationToken>()
            .FirstOrDefaultAsync(t => t.TokenHash == tokenHash, cancellationToken);

        if (verificationToken is null || !verificationToken.IsValid())
            return false;

        // Mark token as used
        verificationToken.MarkUsed();

        // Activate the user
        var user = await _dbContext.Set<User>()
            .FirstOrDefaultAsync(u => u.Id == verificationToken.UserId, cancellationToken);

        if (user is null)
            return false;

        user.Activate();

        await _dbContext.SaveChangesAsync(cancellationToken);

        // Publish integration event
        var @event = UserEmailVerifiedEvent.Create(user.Id);
        await _eventPublisher.PublishAsync(@event, cancellationToken);

        return true;
    }
}
