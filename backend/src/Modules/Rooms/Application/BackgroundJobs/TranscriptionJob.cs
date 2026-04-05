using MassTransit;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Muntada.Rooms.Application.Services;
using Muntada.Rooms.Domain.Recording;
using Muntada.Rooms.Infrastructure;

namespace Muntada.Rooms.Application.BackgroundJobs;

/// <summary>
/// Message contract for requesting transcription of a recording.
/// Published to RabbitMQ when a recording is ready and transcription is enabled.
/// </summary>
/// <param name="RecordingId">The recording to transcribe.</param>
/// <param name="Language">The ISO 639-1 language code for transcription.</param>
public sealed record TranscriptionRequest(
    string RecordingId,
    string Language);

/// <summary>
/// MassTransit consumer that processes transcription requests.
/// Submits recordings to the transcription service and stores results.
/// </summary>
public sealed class TranscriptionJob : IConsumer<TranscriptionRequest>
{
    private readonly RoomsDbContext _db;
    private readonly ITranscriptionService _transcriptionService;
    private readonly ILogger<TranscriptionJob> _logger;

    /// <summary>
    /// Initializes a new instance of the <see cref="TranscriptionJob"/> class.
    /// </summary>
    /// <param name="db">The rooms database context.</param>
    /// <param name="transcriptionService">The transcription service.</param>
    /// <param name="logger">The logger instance.</param>
    public TranscriptionJob(
        RoomsDbContext db,
        ITranscriptionService transcriptionService,
        ILogger<TranscriptionJob> logger)
    {
        _db = db;
        _transcriptionService = transcriptionService;
        _logger = logger;
    }

    /// <inheritdoc />
    public async Task Consume(ConsumeContext<TranscriptionRequest> context)
    {
        var message = context.Message;

        _logger.LogInformation(
            "Processing transcription request for recording {RecordingId} (language: {Language}).",
            message.RecordingId, message.Language);

        var recordingId = new RecordingId(message.RecordingId);
        var recording = await _db.Recordings
            .FirstOrDefaultAsync(r => r.Id == recordingId, context.CancellationToken);

        if (recording is null)
        {
            _logger.LogWarning("Recording {RecordingId} not found, skipping transcription.", message.RecordingId);
            return;
        }

        if (recording.Status != RecordingStatus.Ready)
        {
            _logger.LogWarning(
                "Recording {RecordingId} is in {Status} status, expected Ready. Skipping transcription.",
                message.RecordingId, recording.Status);
            return;
        }

        try
        {
            // Submit to transcription service
            var jobId = await _transcriptionService.SubmitTranscriptionAsync(
                recording.S3Path,
                message.Language,
                context.CancellationToken);

            // Create transcript entry in Processing state
            var vttS3Path = $"{recording.S3Path.Replace(".webm", "")}-{message.Language}.vtt";
            var textS3Path = $"{recording.S3Path.Replace(".webm", "")}-{message.Language}.txt";

            recording.AddTranscript(message.Language, vttS3Path, textS3Path);
            await _db.SaveChangesAsync(context.CancellationToken);

            _logger.LogInformation(
                "Transcription submitted for recording {RecordingId}. External job ID: {JobId}",
                message.RecordingId, jobId);
        }
        catch (Exception ex)
        {
            _logger.LogError(
                ex,
                "Failed to submit transcription for recording {RecordingId}.",
                message.RecordingId);
            throw;
        }
    }
}
