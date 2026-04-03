namespace Muntada.Identity.Api.Dtos;

/// <summary>
/// Response DTO returned after an OTP challenge is successfully created.
/// </summary>
/// <param name="ChallengeId">The unique identifier of the created OTP challenge.</param>
public sealed record OtpChallengeResponse(Guid ChallengeId);
