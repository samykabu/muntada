namespace Muntada.Rooms.Api.Dtos;

/// <summary>
/// SignalR payload sent when a participant joins a room.
/// </summary>
public sealed record ParticipantJoinedPayload(string ParticipantId, string DisplayName, string Role);

/// <summary>
/// SignalR payload sent when a participant leaves a room.
/// </summary>
public sealed record ParticipantLeftPayload(string ParticipantId, string? UserId, DateTimeOffset LeftAt);

/// <summary>
/// SignalR payload sent when a participant's media state changes.
/// </summary>
public sealed record ParticipantMediaChangedPayload(string ParticipantId, string AudioState, string VideoState);

/// <summary>
/// SignalR payload sent when a room's status changes.
/// </summary>
public sealed record RoomStatusChangedPayload(string OccurrenceId, string Status, DateTimeOffset? GraceStartedAt, DateTimeOffset? GraceExpiresAt);

/// <summary>
/// SignalR payload sent when a room's moderator changes.
/// </summary>
public sealed record ModeratorChangedPayload(string OccurrenceId, string NewModeratorUserId, string NewModeratorName);

/// <summary>
/// SignalR payload sent when a room's recording status changes.
/// </summary>
public sealed record RecordingStatusChangedPayload(string OccurrenceId, bool IsRecording);
