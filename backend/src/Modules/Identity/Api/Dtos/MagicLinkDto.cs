namespace Muntada.Identity.Api.Dtos;

/// <summary>
/// Response DTO for a created guest magic link.
/// </summary>
/// <param name="LinkId">The unique identifier of the created magic link.</param>
/// <param name="Token">The plaintext token for constructing the magic link URL (returned only once).</param>
public sealed record MagicLinkDto(Guid LinkId, string Token);
