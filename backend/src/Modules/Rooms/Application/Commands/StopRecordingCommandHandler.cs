using MediatR;
using Microsoft.EntityFrameworkCore;
using Muntada.Rooms.Application.Services;
using Muntada.Rooms.Domain.Occurrence;
using Muntada.Rooms.Domain.Recording;
using Muntada.Rooms.Infrastructure;
using Muntada.SharedKernel.Domain.Exceptions;

namespace Muntada.Rooms.Application.Commands;

/// <summary>
/// Command to stop an active recording for a room occurrence.
/// Stops the LiveKit egress and marks the recording as ready.
/// </summary>
/// <param name="OccurrenceId">The room occurrence being recorded.</param>
/// <param name="TenantId">The owning tenant's identifier.</param>
/// <param name="FileSizeBytes">The final file size in bytes (from egress callback).</param>
/// <param name="DurationSeconds">The recording duration in seconds.</param>
public sealed record StopRecordingCommand(
    string OccurrenceId,
    string TenantId,
    long FileSizeBytes = 0,
    long DurationSeconds = 0) : IRequest<Recording>;

/// <summary>
/// Handles <see cref="StopRecordingCommand"/> — stops the egress and marks the recording as ready.
/// </summary>
public sealed class StopRecordingCommandHandler : IRequestHandler<StopRecordingCommand, Recording>
{
    private readonly RoomsDbContext _db;
    private readonly IRecordingService _recordingService;

    /// <summary>
    /// Initializes a new instance of the <see cref="StopRecordingCommandHandler"/> class.
    /// </summary>
    public StopRecordingCommandHandler(RoomsDbContext db, IRecordingService recordingService)
    {
        _db = db;
        _recordingService = recordingService;
    }

    /// <inheritdoc />
    public async Task<Recording> Handle(StopRecordingCommand request, CancellationToken cancellationToken)
    {
        var occurrenceId = new RoomOccurrenceId(request.OccurrenceId);

        var recording = await _db.Recordings
            .FirstOrDefaultAsync(
                r => r.RoomOccurrenceId == occurrenceId
                    && r.TenantId == request.TenantId
                    && r.Status == RecordingStatus.Processing,
                cancellationToken);

        if (recording is null)
            throw new EntityNotFoundException(
                nameof(Recording),
                $"Active recording for occurrence {request.OccurrenceId}");

        // Stop the egress
        if (!string.IsNullOrEmpty(recording.LiveKitEgressId))
        {
            await _recordingService.StopRecordingAsync(recording.LiveKitEgressId, cancellationToken);
        }

        // Mark recording as ready
        recording.MarkReady(request.FileSizeBytes, request.DurationSeconds);
        recording.IncrementVersion();
        await _db.SaveChangesAsync(cancellationToken);

        return recording;
    }
}
