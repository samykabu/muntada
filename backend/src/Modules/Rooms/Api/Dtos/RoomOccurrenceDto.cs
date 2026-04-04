namespace Muntada.Rooms.Api.Dtos;

/// <summary>
/// Response DTO for a room occurrence.
/// </summary>
public sealed record RoomOccurrenceResponse(
    string Id,
    string TenantId,
    string? RoomSeriesId,
    string Title,
    DateTimeOffset ScheduledAt,
    string OrganizerTimeZoneId,
    DateTimeOffset? LiveStartedAt,
    DateTimeOffset? LiveEndedAt,
    string Status,
    ModeratorDto? Moderator,
    RoomSettingsDto Settings,
    int GracePeriodSeconds,
    DateTimeOffset? GraceStartedAt,
    bool IsCancelled,
    string CreatedBy,
    DateTimeOffset CreatedAt,
    DateTimeOffset UpdatedAt);

/// <summary>
/// DTO representing the moderator assignment for a room occurrence.
/// </summary>
public sealed record ModeratorDto(
    string UserId,
    DateTimeOffset AssignedAt,
    DateTimeOffset? DisconnectedAt);

/// <summary>
/// Request DTO for creating a standalone room occurrence.
/// </summary>
public sealed record CreateRoomOccurrenceRequest(
    string Title,
    DateTimeOffset ScheduledAt,
    string OrganizerTimeZoneId,
    string ModeratorUserId,
    RoomSettingsDto Settings,
    int GracePeriodSeconds = 300);

/// <summary>
/// Request DTO for updating a room occurrence (single-occurrence override).
/// </summary>
public sealed record UpdateRoomOccurrenceRequest(
    string? Title,
    RoomSettingsDto? Settings,
    bool? IsCancelled);

/// <summary>
/// Request DTO for assigning or changing a room moderator.
/// </summary>
public sealed record AssignModeratorRequest(
    string ModeratorUserId);

/// <summary>
/// Request DTO for handing over moderator control to another user.
/// </summary>
public sealed record HandoverModeratorRequest(
    string ToUserId);
