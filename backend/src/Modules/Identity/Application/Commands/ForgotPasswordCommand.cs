using System.Security.Cryptography;
using System.Text;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Muntada.Identity.Application.Services;
using Muntada.Identity.Domain.PasswordReset;
using Muntada.Identity.Domain.User;
using Muntada.Identity.Infrastructure;

namespace Muntada.Identity.Application.Commands;

/// <summary>
/// Command to initiate a password reset by sending a reset link to the user's email.
/// Always returns success to prevent email enumeration (FR-018).
/// </summary>
/// <param name="Email">The email address to send the password reset link to.</param>
public sealed record ForgotPasswordCommand(string Email) : IRequest<bool>;

/// <summary>
/// Handles <see cref="ForgotPasswordCommand"/> by finding the user, generating a reset token,
/// and sending a password reset email. Returns a generic response regardless of whether
/// the user exists to prevent email enumeration.
/// </summary>
public sealed class ForgotPasswordCommandHandler : IRequestHandler<ForgotPasswordCommand, bool>
{
    private static readonly TimeSpan TokenExpiry = TimeSpan.FromHours(1);

    private readonly IdentityDbContext _dbContext;
    private readonly IEmailService _emailService;

    /// <summary>
    /// Initializes a new instance of <see cref="ForgotPasswordCommandHandler"/>.
    /// </summary>
    /// <param name="dbContext">The Identity module database context.</param>
    /// <param name="emailService">Service for sending password reset emails.</param>
    public ForgotPasswordCommandHandler(
        IdentityDbContext dbContext,
        IEmailService emailService)
    {
        _dbContext = dbContext;
        _emailService = emailService;
    }

    /// <summary>
    /// Handles the forgot password request: finds the user by email, generates a secure token,
    /// hashes it with SHA-256, creates a <see cref="PasswordResetToken"/>, and sends the reset email.
    /// Always returns <c>true</c> regardless of outcome to prevent email enumeration (FR-018).
    /// </summary>
    /// <param name="request">The forgot password command.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>Always <c>true</c> to prevent email enumeration.</returns>
    public async Task<bool> Handle(ForgotPasswordCommand request, CancellationToken cancellationToken)
    {
        var normalizedEmail = request.Email.Trim().ToLowerInvariant();

        var user = await _dbContext.Set<User>()
            .FirstOrDefaultAsync(u => u.Email.Value == normalizedEmail, cancellationToken);

        // Generic response — no disclosure of whether user exists (FR-018)
        if (user is null || user.Status != UserStatus.Active)
            return true;

        // Generate reset token
        var rawToken = Convert.ToBase64String(RandomNumberGenerator.GetBytes(32));
        var tokenHash = Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(rawToken)));

        var resetToken = PasswordResetToken.Create(user.Id, tokenHash, TokenExpiry);

        _dbContext.Set<PasswordResetToken>().Add(resetToken);
        await _dbContext.SaveChangesAsync(cancellationToken);

        // Send reset email with the raw (unhashed) token
        await _emailService.SendPasswordResetEmailAsync(user.Email.Value, rawToken, cancellationToken);

        return true;
    }
}
