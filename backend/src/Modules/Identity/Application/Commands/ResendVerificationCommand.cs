using System.Security.Cryptography;
using System.Text;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Muntada.Identity.Application.Services;
using Muntada.Identity.Domain.EmailVerification;
using Muntada.Identity.Domain.User;
using Muntada.Identity.Infrastructure;

namespace Muntada.Identity.Application.Commands;

/// <summary>
/// Command to resend a verification email to a user who has not yet verified their email address.
/// Always returns <c>true</c> to prevent email enumeration.
/// </summary>
/// <param name="Email">The email address to resend the verification to.</param>
public sealed record ResendVerificationCommand(string Email) : IRequest<bool>;

/// <summary>
/// Handles <see cref="ResendVerificationCommand"/> by invalidating existing tokens,
/// generating a new verification token, and sending a fresh verification email.
/// </summary>
public sealed class ResendVerificationCommandHandler : IRequestHandler<ResendVerificationCommand, bool>
{
    private readonly IdentityDbContext _dbContext;
    private readonly IEmailService _emailService;

    /// <summary>
    /// Initializes a new instance of <see cref="ResendVerificationCommandHandler"/>.
    /// </summary>
    /// <param name="dbContext">The Identity module database context.</param>
    /// <param name="emailService">Service for sending verification emails.</param>
    public ResendVerificationCommandHandler(
        IdentityDbContext dbContext,
        IEmailService emailService)
    {
        _dbContext = dbContext;
        _emailService = emailService;
    }

    /// <summary>
    /// Handles the resend verification request. If the user is not found or is not in
    /// <see cref="UserStatus.Unverified"/> status, returns <c>true</c> without action
    /// to prevent email enumeration (FR-018).
    /// </summary>
    /// <param name="request">The resend verification command.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>Always <c>true</c> to prevent email enumeration.</returns>
    public async Task<bool> Handle(ResendVerificationCommand request, CancellationToken cancellationToken)
    {
        var normalizedEmail = request.Email.Trim().ToLowerInvariant();

        var user = await _dbContext.Set<User>()
            .FirstOrDefaultAsync(u => u.Email.Value == normalizedEmail, cancellationToken);

        // Generic response — no disclosure of whether user exists or their status
        if (user is null || user.Status != UserStatus.Unverified)
            return true;

        // Invalidate any existing pending verification tokens for this user
        var existingTokens = await _dbContext.Set<EmailVerificationToken>()
            .Where(t => t.UserId == user.Id && t.Status == EmailVerificationTokenStatus.Pending)
            .ToListAsync(cancellationToken);

        foreach (var token in existingTokens)
        {
            token.MarkUsed();
        }

        // Generate new token
        var rawToken = Convert.ToBase64String(RandomNumberGenerator.GetBytes(32));
        var tokenHash = Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(rawToken)));
        var verificationToken = EmailVerificationToken.Create(
            user.Id,
            tokenHash,
            TimeSpan.FromHours(24));

        _dbContext.Set<EmailVerificationToken>().Add(verificationToken);
        await _dbContext.SaveChangesAsync(cancellationToken);

        // Send verification email with the raw (unhashed) token
        await _emailService.SendVerificationEmailAsync(user.Email.Value, rawToken, cancellationToken);

        return true;
    }
}
