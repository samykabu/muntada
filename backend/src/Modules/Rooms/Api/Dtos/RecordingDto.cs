namespace Muntada.Rooms.Api.Dtos;

/// <summary>
/// Response DTO for a recording with optional pre-signed download URL.
/// </summary>
public sealed record RecordingResponse(
    string Id,
    string RoomOccurrenceId,
    string TenantId,
    string S3Path,
    long FileSizeBytes,
    long DurationSeconds,
    string Status,
    string Visibility,
    string? LiveKitEgressId,
    string? DownloadUrl,
    List<TranscriptResponse> Transcripts,
    DateTimeOffset CreatedAt,
    DateTimeOffset UpdatedAt);

/// <summary>
/// Response DTO for a transcript associated with a recording.
/// </summary>
public sealed record TranscriptResponse(
    string Language,
    string S3Path,
    string TextS3Path,
    string Status,
    DateTimeOffset CreatedAt);
