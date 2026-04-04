namespace Muntada.Rooms.Domain.Recording;

/// <summary>
/// Represents the processing status of a transcript.
/// </summary>
public enum TranscriptStatus
{
    /// <summary>Transcription is in progress.</summary>
    Processing,

    /// <summary>Transcript is ready for viewing.</summary>
    Ready,

    /// <summary>Transcription processing failed.</summary>
    Failed
}
