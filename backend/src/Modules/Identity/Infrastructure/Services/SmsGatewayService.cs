using Microsoft.Extensions.Logging;
using Muntada.Identity.Application.Services;

namespace Muntada.Identity.Infrastructure.Services;

/// <summary>
/// SMS gateway service with retry logic (max 3 retries, exponential backoff).
/// Default implementation logs the OTP code for development use.
/// Replace with Twilio/AWS SNS in production.
/// </summary>
public sealed class SmsGatewayService : ISmsService
{
    private const int MaxRetries = 3;
    private readonly ILogger<SmsGatewayService> _logger;

    /// <summary>
    /// Initializes a new instance of <see cref="SmsGatewayService"/>.
    /// </summary>
    public SmsGatewayService(ILogger<SmsGatewayService> logger)
    {
        _logger = logger;
    }

    /// <inheritdoc />
    public async Task<bool> SendOtpCodeAsync(string phoneNumber, string code, CancellationToken cancellationToken = default)
    {
        for (var attempt = 1; attempt <= MaxRetries; attempt++)
        {
            try
            {
                // TODO: Replace with actual SMS gateway (Twilio, AWS SNS)
                // SECURITY: Never log the actual OTP code in production (FR-018/SC-009)
                _logger.LogInformation(
                    "SMS OTP sent to {PhoneNumber} (attempt {Attempt}/{MaxRetries})",
                    phoneNumber, attempt, MaxRetries);

                // Simulate successful send
                await Task.CompletedTask;
                return true;
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex,
                    "SMS delivery failed for {PhoneNumber} (attempt {Attempt}/{MaxRetries})",
                    phoneNumber, attempt, MaxRetries);

                if (attempt < MaxRetries)
                {
                    var delay = TimeSpan.FromSeconds(Math.Pow(2, attempt));
                    await Task.Delay(delay, cancellationToken);
                }
            }
        }

        _logger.LogError("SMS delivery failed after {MaxRetries} attempts for {PhoneNumber}",
            MaxRetries, phoneNumber);
        return false;
    }
}
