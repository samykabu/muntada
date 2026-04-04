namespace Muntada.Rooms.Application.Services;

/// <summary>
/// Abstraction for transcription operations.
/// Implementations integrate with external transcription providers (e.g., AssemblyAI).
/// </summary>
public interface ITranscriptionService
{
    /// <summary>
    /// Submits a recording for transcription processing.
    /// </summary>
    /// <param name="recordingS3Path">The S3 path of the recording to transcribe.</param>
    /// <param name="language">The ISO 639-1 language code for transcription.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>The external transcription job identifier.</returns>
    Task<string> SubmitTranscriptionAsync(
        string recordingS3Path,
        string language,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets the current status and results of a transcription job.
    /// </summary>
    /// <param name="transcriptionJobId">The external transcription job identifier.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>The transcription status result.</returns>
    Task<TranscriptionStatusResult> GetTranscriptionStatusAsync(
        string transcriptionJobId,
        CancellationToken cancellationToken = default);
}

/// <summary>
/// Represents the status and results of a transcription job.
/// </summary>
/// <param name="JobId">The external transcription job identifier.</param>
/// <param name="Status">The current status (Processing, Ready, Failed).</param>
/// <param name="VttContent">The VTT subtitle content, or null if not yet ready.</param>
/// <param name="TextContent">The plain text transcript, or null if not yet ready.</param>
public sealed record TranscriptionStatusResult(
    string JobId,
    string Status,
    string? VttContent,
    string? TextContent);
