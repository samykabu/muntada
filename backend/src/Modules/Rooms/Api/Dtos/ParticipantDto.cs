namespace Muntada.Rooms.Api.Dtos;

/// <summary>
/// Response DTO for a room participant.
/// </summary>
public sealed record ParticipantResponse(
    string ParticipantId,
    string? UserId,
    string DisplayName,
    string Role,
    string AudioState,
    string VideoState,
    DateTimeOffset JoinedAt,
    DateTimeOffset? LeftAt);

/// <summary>
/// Response DTO for room analytics data.
/// </summary>
public sealed record RoomAnalyticsResponse(
    int TotalParticipants,
    int PeakConcurrent,
    long AverageTimeDwellSeconds,
    double AudioParticipationRate,
    double VideoParticipationRate,
    List<ParticipantDwellTimeDto> DwellTimes);

/// <summary>
/// DTO for individual participant dwell time within analytics.
/// </summary>
public sealed record ParticipantDwellTimeDto(
    string ParticipantId,
    string DisplayName,
    long DwellTimeSeconds);
