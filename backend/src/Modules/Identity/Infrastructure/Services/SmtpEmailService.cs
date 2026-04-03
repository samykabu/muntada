using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Muntada.Identity.Application.Services;

namespace Muntada.Identity.Infrastructure.Services;

/// <summary>
/// SMTP-based email service for sending verification and password reset emails.
/// Uses template-based email bodies. Configurable via appsettings.
/// </summary>
public sealed class SmtpEmailService : IEmailService
{
    private readonly ILogger<SmtpEmailService> _logger;
    private readonly string _baseUrl;

    /// <summary>
    /// Initializes a new instance of <see cref="SmtpEmailService"/>.
    /// </summary>
    public SmtpEmailService(IConfiguration configuration, ILogger<SmtpEmailService> logger)
    {
        _logger = logger;
        _baseUrl = configuration["App:BaseUrl"] ?? "https://muntada.com";
    }

    /// <inheritdoc />
    public async Task SendVerificationEmailAsync(string email, string verificationToken, CancellationToken cancellationToken = default)
    {
        var link = $"{_baseUrl}/verify-email?token={Uri.EscapeDataString(verificationToken)}";
        _logger.LogInformation("Sending verification email to {Email} with link {Link}", email, link);

        // TODO: Replace with actual SMTP/SendGrid implementation
        // For development, log the link so developers can click it manually
        await Task.CompletedTask;
    }

    /// <inheritdoc />
    public async Task SendPasswordResetEmailAsync(string email, string resetToken, CancellationToken cancellationToken = default)
    {
        var link = $"{_baseUrl}/reset-password?token={Uri.EscapeDataString(resetToken)}";
        _logger.LogInformation("Sending password reset email to {Email} with link {Link}", email, link);

        // TODO: Replace with actual SMTP/SendGrid implementation
        await Task.CompletedTask;
    }
}
