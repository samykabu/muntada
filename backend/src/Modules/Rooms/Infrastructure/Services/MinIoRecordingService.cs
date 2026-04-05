using Amazon.S3;
using Amazon.S3.Model;
using Microsoft.Extensions.Logging;
using Muntada.Rooms.Application.Services;

namespace Muntada.Rooms.Infrastructure.Services;

/// <summary>
/// MinIO/S3-based implementation of <see cref="IRecordingService"/>.
/// Uses AWSSDK.S3 for pre-signed URL generation. LiveKit egress calls are stubbed
/// pending LiveKit SDK integration.
/// </summary>
public class MinIoRecordingService : IRecordingService
{
    private readonly IAmazonS3 _s3Client;
    private readonly ILogger<MinIoRecordingService> _logger;
    private const string BucketName = "muntada-recordings";

    /// <summary>
    /// Initializes a new instance of the <see cref="MinIoRecordingService"/> class.
    /// </summary>
    /// <param name="s3Client">The S3 client configured for MinIO.</param>
    /// <param name="logger">The logger instance.</param>
    public MinIoRecordingService(IAmazonS3 s3Client, ILogger<MinIoRecordingService> logger)
    {
        _s3Client = s3Client;
        _logger = logger;
    }

    /// <inheritdoc />
    public Task<string> StartRecordingAsync(
        string occurrenceId,
        string tenantId,
        string s3Path,
        CancellationToken cancellationToken = default)
    {
        // Stub: In production, this would call LiveKit Egress API to start room composite recording
        // and configure it to upload directly to the MinIO S3 bucket.
        var egressId = $"egress-{Guid.NewGuid():N}";
        _logger.LogInformation(
            "Starting recording for occurrence {OccurrenceId} (tenant {TenantId}). Egress ID: {EgressId}, S3Path: {S3Path}",
            occurrenceId, tenantId, egressId, s3Path);
        return Task.FromResult(egressId);
    }

    /// <inheritdoc />
    public Task StopRecordingAsync(string egressId, CancellationToken cancellationToken = default)
    {
        // Stub: In production, this would call LiveKit Egress API to stop the egress job.
        _logger.LogInformation("Stopping recording with egress ID: {EgressId}", egressId);
        return Task.CompletedTask;
    }

    /// <inheritdoc />
    public Task<string> GetPresignedDownloadUrlAsync(
        string s3Path,
        int expiresInMinutes = 60,
        CancellationToken cancellationToken = default)
    {
        var request = new GetPreSignedUrlRequest
        {
            BucketName = BucketName,
            Key = s3Path,
            Expires = DateTime.UtcNow.AddMinutes(expiresInMinutes),
            Verb = HttpVerb.GET
        };

        var url = _s3Client.GetPreSignedURL(request);
        _logger.LogDebug("Generated pre-signed URL for {S3Path}, expires in {Minutes} minutes.", s3Path, expiresInMinutes);
        return Task.FromResult(url);
    }
}
