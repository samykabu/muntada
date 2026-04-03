namespace Muntada.Identity.Api.Dtos;

/// <summary>
/// Request DTO for creating a guest magic link.
/// </summary>
/// <param name="RoomOccurrenceId">The room occurrence the magic link will grant access to.</param>
public sealed record CreateMagicLinkRequest(Guid RoomOccurrenceId);
