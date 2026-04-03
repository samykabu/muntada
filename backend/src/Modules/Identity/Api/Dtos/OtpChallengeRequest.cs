namespace Muntada.Identity.Api.Dtos;

/// <summary>
/// Request DTO for generating a phone OTP challenge.
/// </summary>
/// <param name="PhoneNumber">The phone number in E.164 format to send the OTP to.</param>
public sealed record OtpChallengeRequest(string PhoneNumber);
