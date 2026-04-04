using Amazon.S3;
using Amazon.S3.Model;
using Microsoft.Extensions.Logging;

namespace Muntada.Tenancy.Infrastructure.Services;

/// <summary>
/// S3-compatible object storage wrapper for MinIO operations.
/// Provides upload, download, and delete functionality against a configured bucket.
/// </summary>
public class MinIoStorageService
{
    private readonly IAmazonS3 _s3Client;
    private readonly string _bucketName;
    private readonly ILogger<MinIoStorageService> _logger;

    /// <summary>
    /// Initializes a new instance of <see cref="MinIoStorageService"/>.
    /// </summary>
    /// <param name="s3Client">The S3-compatible client (configured for MinIO).</param>
    /// <param name="bucketName">The name of the storage bucket.</param>
    /// <param name="logger">Logger for storage operations.</param>
    public MinIoStorageService(
        IAmazonS3 s3Client,
        string bucketName,
        ILogger<MinIoStorageService> logger)
    {
        _s3Client = s3Client;
        _bucketName = bucketName;
        _logger = logger;
    }

    /// <summary>
    /// Uploads an object to the storage bucket.
    /// </summary>
    /// <param name="key">The object key (path) within the bucket.</param>
    /// <param name="stream">The content stream to upload.</param>
    /// <param name="contentType">The MIME content type of the object.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>The URL of the uploaded object.</returns>
    public async Task<string> UploadAsync(
        string key,
        Stream stream,
        string contentType,
        CancellationToken cancellationToken = default)
    {
        var request = new PutObjectRequest
        {
            BucketName = _bucketName,
            Key = key,
            InputStream = stream,
            ContentType = contentType
        };

        await _s3Client.PutObjectAsync(request, cancellationToken);

        var url = $"{_s3Client.Config.ServiceURL}/{_bucketName}/{key}";

        _logger.LogDebug("Uploaded object to {Key} in bucket {Bucket}", key, _bucketName);

        return url;
    }

    /// <summary>
    /// Downloads an object from the storage bucket.
    /// </summary>
    /// <param name="key">The object key (path) within the bucket.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>The response stream containing the object data.</returns>
    public async Task<Stream> DownloadAsync(
        string key,
        CancellationToken cancellationToken = default)
    {
        var request = new GetObjectRequest
        {
            BucketName = _bucketName,
            Key = key
        };

        var response = await _s3Client.GetObjectAsync(request, cancellationToken);

        _logger.LogDebug("Downloaded object from {Key} in bucket {Bucket}", key, _bucketName);

        return response.ResponseStream;
    }

    /// <summary>
    /// Deletes an object from the storage bucket.
    /// </summary>
    /// <param name="key">The object key (path) within the bucket.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    public async Task DeleteAsync(
        string key,
        CancellationToken cancellationToken = default)
    {
        var request = new DeleteObjectRequest
        {
            BucketName = _bucketName,
            Key = key
        };

        await _s3Client.DeleteObjectAsync(request, cancellationToken);

        _logger.LogDebug("Deleted object {Key} from bucket {Bucket}", key, _bucketName);
    }
}
