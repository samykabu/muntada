using Muntada.Rooms.Domain.Occurrence;
using Muntada.SharedKernel.Domain;

namespace Muntada.Rooms.Domain.Recording;

/// <summary>
/// Aggregate root representing a recorded audio/video capture from a room session.
/// Stored in MinIO with metadata in SQL Server. Supports optional transcription.
/// </summary>
public class Recording : AggregateRoot<RecordingId>
{
    /// <summary>Gets the room occurrence this recording belongs to.</summary>
    public RoomOccurrenceId RoomOccurrenceId { get; private set; } = default!;

    /// <summary>Gets the tenant that owns this recording.</summary>
    public string TenantId { get; private set; } = default!;

    /// <summary>Gets the MinIO S3 bucket path for the recording file.</summary>
    public string S3Path { get; private set; } = default!;

    /// <summary>Gets the file size in bytes.</summary>
    public long FileSizeBytes { get; private set; }

    /// <summary>Gets the duration in seconds.</summary>
    public long DurationSeconds { get; private set; }

    /// <summary>Gets the processing status.</summary>
    public RecordingStatus Status { get; private set; }

    /// <summary>Gets the visibility setting for this recording.</summary>
    public RecordingVisibility Visibility { get; private set; }

    /// <summary>Gets the LiveKit egress job ID for status tracking.</summary>
    public string? LiveKitEgressId { get; private set; }

    /// <summary>Gets the collection of transcripts for this recording.</summary>
    public List<Transcript> Transcripts { get; private set; } = new();

    private Recording() { }

    /// <summary>
    /// Creates a new recording in Processing status.
    /// </summary>
    public static Recording Create(
        RoomOccurrenceId occurrenceId,
        string tenantId,
        string s3Path,
        RecordingVisibility visibility,
        string? liveKitEgressId = null)
    {
        return new Recording
        {
            Id = RecordingId.New(),
            RoomOccurrenceId = occurrenceId,
            TenantId = tenantId,
            S3Path = s3Path,
            Status = RecordingStatus.Processing,
            Visibility = visibility,
            LiveKitEgressId = liveKitEgressId,
            CreatedAt = DateTimeOffset.UtcNow,
            UpdatedAt = DateTimeOffset.UtcNow
        };
    }

    /// <summary>
    /// Marks the recording as ready with final metadata.
    /// </summary>
    public void MarkReady(long fileSizeBytes, long durationSeconds)
    {
        if (Status != RecordingStatus.Processing)
            throw new InvalidOperationException("Can only mark a processing recording as ready.");

        FileSizeBytes = fileSizeBytes;
        DurationSeconds = durationSeconds;
        Status = RecordingStatus.Ready;
        UpdatedAt = DateTimeOffset.UtcNow;
    }

    /// <summary>
    /// Marks the recording as failed.
    /// </summary>
    public void MarkFailed()
    {
        Status = RecordingStatus.Failed;
        UpdatedAt = DateTimeOffset.UtcNow;
    }

    /// <summary>
    /// Adds a transcript to this recording.
    /// </summary>
    public void AddTranscript(string language, string s3Path, string textS3Path)
    {
        Transcripts.Add(Transcript.Create(language, s3Path, textS3Path));
        UpdatedAt = DateTimeOffset.UtcNow;
    }
}

/// <summary>
/// Value object representing a transcript of a recording in a specific language.
/// </summary>
public sealed class Transcript
{
    /// <summary>Gets the ISO 639-1 language code.</summary>
    public string Language { get; private set; } = default!;

    /// <summary>Gets the MinIO S3 path for the VTT subtitle file.</summary>
    public string S3Path { get; private set; } = default!;

    /// <summary>Gets the MinIO S3 path for the plain text transcript.</summary>
    public string TextS3Path { get; private set; } = default!;

    /// <summary>Gets the processing status of this transcript.</summary>
    public TranscriptStatus Status { get; private set; }

    /// <summary>Gets the UTC creation timestamp.</summary>
    public DateTimeOffset CreatedAt { get; private set; }

    private Transcript() { }

    /// <summary>Creates a new transcript in Processing status.</summary>
    public static Transcript Create(string language, string s3Path, string textS3Path)
    {
        return new Transcript
        {
            Language = language,
            S3Path = s3Path,
            TextS3Path = textS3Path,
            Status = TranscriptStatus.Processing,
            CreatedAt = DateTimeOffset.UtcNow
        };
    }

    /// <summary>Marks the transcript as ready.</summary>
    public void MarkReady() => Status = TranscriptStatus.Ready;

    /// <summary>Marks the transcript as failed.</summary>
    public void MarkFailed() => Status = TranscriptStatus.Failed;
}
