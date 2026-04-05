using Microsoft.Extensions.Logging;
using Muntada.Rooms.Application.Services;

namespace Muntada.Rooms.Infrastructure.Services;

/// <summary>
/// Stub implementation of <see cref="ITranscriptionService"/> for AssemblyAI integration.
/// In production, this would call the AssemblyAI REST API to submit audio files
/// and poll for transcription results.
/// </summary>
public class TranscriptionService : ITranscriptionService
{
    private readonly ILogger<TranscriptionService> _logger;

    /// <summary>
    /// Initializes a new instance of the <see cref="TranscriptionService"/> class.
    /// </summary>
    /// <param name="logger">The logger instance.</param>
    public TranscriptionService(ILogger<TranscriptionService> logger)
    {
        _logger = logger;
    }

    /// <inheritdoc />
    public Task<string> SubmitTranscriptionAsync(
        string recordingS3Path,
        string language,
        CancellationToken cancellationToken = default)
    {
        // Stub: In production, this would:
        // 1. Generate a pre-signed URL for the recording
        // 2. Submit the URL to AssemblyAI's transcription API
        // 3. Return the AssemblyAI transcript ID
        var jobId = $"txn-{Guid.NewGuid():N}";
        _logger.LogInformation(
            "Submitted transcription for {RecordingPath} (language: {Language}). Job ID: {JobId}",
            recordingS3Path, language, jobId);
        return Task.FromResult(jobId);
    }

    /// <inheritdoc />
    public Task<TranscriptionStatusResult> GetTranscriptionStatusAsync(
        string transcriptionJobId,
        CancellationToken cancellationToken = default)
    {
        // Stub: In production, this would poll AssemblyAI's API for the transcript status
        // and return the VTT and text content when ready.
        _logger.LogInformation(
            "Checking transcription status for job {JobId}.", transcriptionJobId);

        return Task.FromResult(new TranscriptionStatusResult(
            JobId: transcriptionJobId,
            Status: "Processing",
            VttContent: null,
            TextContent: null));
    }
}
