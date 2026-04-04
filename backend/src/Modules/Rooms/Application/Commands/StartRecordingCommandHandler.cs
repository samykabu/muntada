using MediatR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Muntada.Rooms.Application.Services;
using Muntada.Rooms.Domain.Occurrence;
using Muntada.Rooms.Domain.Recording;
using Muntada.Rooms.Infrastructure;
using Muntada.SharedKernel.Domain.Exceptions;

namespace Muntada.Rooms.Application.Commands;

/// <summary>
/// Command to start recording a live room occurrence.
/// Validates the room is live and recording is allowed by the plan.
/// </summary>
/// <param name="OccurrenceId">The room occurrence to record.</param>
/// <param name="TenantId">The owning tenant's identifier.</param>
/// <param name="Visibility">The recording visibility setting.</param>
public sealed record StartRecordingCommand(
    string OccurrenceId,
    string TenantId,
    RecordingVisibility Visibility = RecordingVisibility.Shared) : IRequest<Recording>;

/// <summary>
/// Handles <see cref="StartRecordingCommand"/> — validates room state and plan settings,
/// then creates a Recording entity and initiates egress via <see cref="IRecordingService"/>.
/// </summary>
public sealed class StartRecordingCommandHandler : IRequestHandler<StartRecordingCommand, Recording>
{
    private readonly RoomsDbContext _db;
    private readonly IRecordingService _recordingService;
    private readonly ILogger<StartRecordingCommandHandler> _logger;

    /// <summary>
    /// Initializes a new instance of the <see cref="StartRecordingCommandHandler"/> class.
    /// </summary>
    public StartRecordingCommandHandler(RoomsDbContext db, IRecordingService recordingService, ILogger<StartRecordingCommandHandler> logger)
    {
        _db = db;
        _recordingService = recordingService;
        _logger = logger;
    }

    /// <inheritdoc />
    public async Task<Recording> Handle(StartRecordingCommand request, CancellationToken cancellationToken)
    {
        using var activity = RoomsTelemetry.RecordingOperation(request.OccurrenceId, "pending", "start");
        var occurrenceId = new RoomOccurrenceId(request.OccurrenceId);
        var occurrence = await _db.RoomOccurrences
            .FirstOrDefaultAsync(o => o.Id == occurrenceId && o.TenantId == request.TenantId, cancellationToken);

        if (occurrence is null)
            throw new EntityNotFoundException(nameof(RoomOccurrence), request.OccurrenceId);

        // Validate room is Live
        if (occurrence.Status != RoomOccurrenceStatus.Live)
            throw new ValidationException(
                "Status",
                $"Cannot start recording when room is in '{occurrence.Status}' status. Room must be Live.");

        // Validate plan allows recording
        if (!occurrence.Settings.AllowRecording)
            throw new ValidationException(
                "AllowRecording",
                "Recording is not enabled for this room occurrence.");

        // Check for an existing active recording
        var existingRecording = await _db.Recordings
            .AnyAsync(r => r.RoomOccurrenceId == occurrenceId && r.Status == RecordingStatus.Processing, cancellationToken);

        if (existingRecording)
            throw new ValidationException(
                "Recording",
                "A recording is already in progress for this room occurrence.");

        // Generate S3 path
        var s3Path = $"{request.TenantId}/{request.OccurrenceId}/{DateTimeOffset.UtcNow:yyyyMMddHHmmss}.webm";

        // Start egress via recording service
        var egressId = await _recordingService.StartRecordingAsync(
            request.OccurrenceId, request.TenantId, s3Path, cancellationToken);

        // Create recording entity
        var recording = Recording.Create(
            occurrenceId,
            request.TenantId,
            s3Path,
            request.Visibility,
            egressId);

        _db.Recordings.Add(recording);
        await _db.SaveChangesAsync(cancellationToken);

        activity?.SetTag("rooms.recording_id", recording.Id.Value);
        RoomsLogging.RecordingStarted(_logger, request.OccurrenceId, recording.Id.Value, null);

        return recording;
    }
}
