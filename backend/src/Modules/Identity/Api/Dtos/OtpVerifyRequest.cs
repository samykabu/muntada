namespace Muntada.Identity.Api.Dtos;

/// <summary>
/// Request DTO for verifying a phone OTP code.
/// </summary>
/// <param name="ChallengeId">The unique identifier of the OTP challenge to verify.</param>
/// <param name="Code">The 6-digit OTP code entered by the user.</param>
public sealed record OtpVerifyRequest(Guid ChallengeId, string Code);
