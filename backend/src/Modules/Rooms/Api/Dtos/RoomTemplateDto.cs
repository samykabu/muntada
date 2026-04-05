namespace Muntada.Rooms.Api.Dtos;

/// <summary>
/// Response DTO for a room template.
/// </summary>
public sealed record RoomTemplateResponse(
    string Id,
    string TenantId,
    string Name,
    string? Description,
    RoomSettingsDto Settings,
    string CreatedBy,
    DateTimeOffset CreatedAt,
    DateTimeOffset UpdatedAt);

/// <summary>
/// DTO for room settings within a template or occurrence.
/// </summary>
public sealed record RoomSettingsDto(
    int MaxParticipants,
    bool AllowGuestAccess,
    bool AllowRecording,
    bool AllowTranscription,
    string? DefaultTranscriptionLanguage,
    bool AutoStartRecording);

/// <summary>
/// Request DTO for creating a room template.
/// </summary>
public sealed record CreateRoomTemplateRequest(
    string Name,
    string? Description,
    RoomSettingsDto Settings);

/// <summary>
/// Request DTO for updating a room template (name is immutable).
/// </summary>
public sealed record UpdateRoomTemplateRequest(
    string? Description,
    RoomSettingsDto Settings);

/// <summary>
/// Generic paginated response wrapper.
/// </summary>
public sealed record PagedResponse<T>(
    IReadOnlyList<T> Items,
    int TotalCount,
    int Page,
    int PageSize);
