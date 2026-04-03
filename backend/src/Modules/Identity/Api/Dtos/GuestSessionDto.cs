namespace Muntada.Identity.Api.Dtos;

/// <summary>
/// Response DTO for a guest session created from a validated magic link.
/// </summary>
/// <param name="GuestSessionId">The unique identifier of the created guest session.</param>
/// <param name="RoomOccurrenceId">The room occurrence the guest has been granted access to.</param>
public sealed record GuestSessionDto(Guid GuestSessionId, Guid RoomOccurrenceId);
