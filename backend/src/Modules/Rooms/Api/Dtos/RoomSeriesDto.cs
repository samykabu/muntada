namespace Muntada.Rooms.Api.Dtos;

/// <summary>
/// Response DTO for a room series.
/// </summary>
public sealed record RoomSeriesResponse(
    string Id,
    string TenantId,
    string TemplateId,
    string Title,
    string RecurrenceRule,
    string OrganizerTimeZoneId,
    DateTimeOffset StartsAt,
    DateTimeOffset? EndsAt,
    string Status,
    string CreatedBy,
    DateTimeOffset CreatedAt,
    DateTimeOffset UpdatedAt);

/// <summary>
/// Request DTO for creating a recurring room series.
/// </summary>
public sealed record CreateRoomSeriesRequest(
    string TemplateId,
    string Title,
    string RecurrenceRule,
    string OrganizerTimeZoneId,
    DateTimeOffset StartsAt,
    DateTimeOffset? EndsAt,
    string ModeratorUserId);

/// <summary>
/// Request DTO for updating a room series recurrence.
/// </summary>
public sealed record UpdateRoomSeriesRequest(
    string? RecurrenceRule,
    DateTimeOffset? EndsAt,
    string ModeratorUserId);
