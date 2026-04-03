namespace Muntada.Identity.Application.Services;

/// <summary>
/// Service for sending identity-related emails (verification, password reset).
/// Implementation is infrastructure-specific (SMTP, SendGrid, etc.).
/// </summary>
public interface IEmailService
{
    /// <summary>
    /// Sends an email verification link to the user.
    /// </summary>
    /// <param name="email">The recipient email address.</param>
    /// <param name="verificationToken">The plaintext verification token (included in the link).</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    Task SendVerificationEmailAsync(string email, string verificationToken, CancellationToken cancellationToken = default);

    /// <summary>
    /// Sends a password reset link to the user.
    /// </summary>
    /// <param name="email">The recipient email address.</param>
    /// <param name="resetToken">The plaintext reset token (included in the link).</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    Task SendPasswordResetEmailAsync(string email, string resetToken, CancellationToken cancellationToken = default);
}
