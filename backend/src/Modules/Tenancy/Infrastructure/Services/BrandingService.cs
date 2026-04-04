using Microsoft.Extensions.Logging;
using Muntada.Tenancy.Application.Services;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.Processing;

namespace Muntada.Tenancy.Infrastructure.Services;

/// <summary>
/// Implements <see cref="IBrandingService"/> by resizing uploaded logos to standard
/// size variants (32, 64, 128, 256 pixels) and storing them in MinIO via S3-compatible API.
/// </summary>
public class BrandingService : IBrandingService
{
    private static readonly int[] LogoSizes = [32, 64, 128, 256];

    private readonly MinIoStorageService _storageService;
    private readonly ILogger<BrandingService> _logger;

    /// <summary>
    /// Initializes a new instance of <see cref="BrandingService"/>.
    /// </summary>
    /// <param name="storageService">The MinIO storage service for object operations.</param>
    /// <param name="logger">Logger for branding operations.</param>
    public BrandingService(
        MinIoStorageService storageService,
        ILogger<BrandingService> logger)
    {
        _storageService = storageService;
        _logger = logger;
    }

    /// <inheritdoc />
    public async Task<LogoUploadResult> UploadLogoAsync(
        Guid tenantId,
        Stream logoStream,
        string contentType,
        CancellationToken cancellationToken = default)
    {
        using var image = await Image.LoadAsync(logoStream, cancellationToken);

        var urls = new Dictionary<int, string>();

        foreach (var size in LogoSizes)
        {
            using var resized = image.Clone(ctx => ctx.Resize(size, size));
            using var ms = new MemoryStream();

            await resized.SaveAsPngAsync(ms, cancellationToken);
            ms.Position = 0;

            var key = $"tenants/{tenantId}/logo-{size}.png";
            var url = await _storageService.UploadAsync(key, ms, "image/png", cancellationToken);
            urls[size] = url;
        }

        _logger.LogInformation(
            "Uploaded logo variants for tenant {TenantId}: {Sizes}",
            tenantId, string.Join(", ", LogoSizes.Select(s => $"{s}px")));

        return new LogoUploadResult(
            LogoUrl: urls[256],
            Logo32Url: urls[32],
            Logo64Url: urls[64],
            Logo128Url: urls[128],
            Logo256Url: urls[256]);
    }
}
