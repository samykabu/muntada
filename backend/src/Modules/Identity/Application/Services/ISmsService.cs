namespace Muntada.Identity.Application.Services;

/// <summary>
/// Service for sending SMS messages (OTP codes).
/// Implementation is infrastructure-specific (Twilio, AWS SNS, etc.).
/// </summary>
public interface ISmsService
{
    /// <summary>
    /// Sends an OTP code via SMS to the given phone number.
    /// Includes retry logic (max 3 retries with exponential backoff).
    /// </summary>
    /// <param name="phoneNumber">The recipient phone number in E.164 format.</param>
    /// <param name="code">The 6-digit OTP code to send.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns><c>true</c> if the SMS was delivered; <c>false</c> if all retries failed.</returns>
    Task<bool> SendOtpCodeAsync(string phoneNumber, string code, CancellationToken cancellationToken = default);
}
