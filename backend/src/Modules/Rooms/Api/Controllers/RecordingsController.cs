using MediatR;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Muntada.Rooms.Api.Dtos;
using Muntada.Rooms.Api.Filters;
using Muntada.Rooms.Application.Queries;
using Muntada.Rooms.Application.Services;
using Muntada.Rooms.Domain.Recording;
using Muntada.SharedKernel.Domain.Exceptions;

namespace Muntada.Rooms.Api.Controllers;

/// <summary>
/// REST API controller for accessing room recording metadata and download URLs.
/// </summary>
[ApiController]
[Route("api/v1/tenants/{tenantId}/room-occurrences/{occurrenceId}/recording")]
[ServiceFilter(typeof(RoomTenantValidationFilter))]
public class RecordingsController : ControllerBase
{
    private readonly ISender _sender;
    private readonly IRecordingService _recordingService;

    /// <summary>
    /// Initializes a new instance of the <see cref="RecordingsController"/> class.
    /// </summary>
    public RecordingsController(ISender sender, IRecordingService recordingService)
    {
        _sender = sender;
        _recordingService = recordingService;
    }

    /// <summary>
    /// Gets recording metadata and pre-signed download URLs for a room occurrence.
    /// Returns the most recent recording with its transcripts.
    /// </summary>
    /// <param name="tenantId">The tenant identifier.</param>
    /// <param name="occurrenceId">The room occurrence identifier.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>The recording metadata with download URL.</returns>
    [HttpGet]
    [ProducesResponseType(typeof(RecordingResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetRecording(
        [FromRoute] string tenantId,
        [FromRoute] string occurrenceId,
        CancellationToken cancellationToken)
    {
        var query = new GetRecordingQuery(tenantId, occurrenceId);
        var recording = await _sender.Send(query, cancellationToken);

        if (recording is null)
            throw new EntityNotFoundException(nameof(Recording), $"recording for occurrence {occurrenceId}");

        // Generate pre-signed URL for ready recordings
        string? downloadUrl = null;
        if (recording.Status == RecordingStatus.Ready)
        {
            downloadUrl = await _recordingService.GetPresignedDownloadUrlAsync(
                recording.S3Path, cancellationToken: cancellationToken);
        }

        var response = new RecordingResponse(
            recording.Id.Value,
            recording.RoomOccurrenceId.Value,
            recording.TenantId,
            recording.S3Path,
            recording.FileSizeBytes,
            recording.DurationSeconds,
            recording.Status.ToString(),
            recording.Visibility.ToString(),
            recording.LiveKitEgressId,
            downloadUrl,
            recording.Transcripts.Select(t => new TranscriptResponse(
                t.Language,
                t.S3Path,
                t.TextS3Path,
                t.Status.ToString(),
                t.CreatedAt)).ToList(),
            recording.CreatedAt,
            recording.UpdatedAt);

        return Ok(response);
    }
}
