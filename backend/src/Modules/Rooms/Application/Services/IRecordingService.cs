namespace Muntada.Rooms.Application.Services;

/// <summary>
/// Abstraction for recording operations.
/// Implementations integrate with LiveKit egress and MinIO storage.
/// </summary>
public interface IRecordingService
{
    /// <summary>
    /// Starts a recording for a room occurrence via LiveKit egress.
    /// </summary>
    /// <param name="occurrenceId">The room occurrence to record.</param>
    /// <param name="tenantId">The owning tenant's identifier.</param>
    /// <param name="s3Path">The MinIO S3 path for the recording file.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>The LiveKit egress job ID.</returns>
    Task<string> StartRecordingAsync(
        string occurrenceId,
        string tenantId,
        string s3Path,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Stops an active recording via LiveKit egress.
    /// </summary>
    /// <param name="egressId">The LiveKit egress job ID to stop.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    Task StopRecordingAsync(
        string egressId,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Generates a pre-signed download URL for a recording stored in MinIO.
    /// </summary>
    /// <param name="s3Path">The S3 path of the recording.</param>
    /// <param name="expiresInMinutes">URL validity period in minutes (default 60).</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>The pre-signed download URL.</returns>
    Task<string> GetPresignedDownloadUrlAsync(
        string s3Path,
        int expiresInMinutes = 60,
        CancellationToken cancellationToken = default);
}
