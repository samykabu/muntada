namespace Muntada.Tenancy.Application.Services;

/// <summary>
/// Result of a logo upload operation containing URLs for all generated size variants.
/// </summary>
/// <param name="LogoUrl">The URL of the primary (256px) logo.</param>
/// <param name="Logo32Url">The URL of the 32px logo variant.</param>
/// <param name="Logo64Url">The URL of the 64px logo variant.</param>
/// <param name="Logo128Url">The URL of the 128px logo variant.</param>
/// <param name="Logo256Url">The URL of the 256px logo variant.</param>
public sealed record LogoUploadResult(
    string LogoUrl,
    string Logo32Url,
    string Logo64Url,
    string Logo128Url,
    string Logo256Url);

/// <summary>
/// Service for managing tenant branding assets including logo uploads with
/// automatic resizing to multiple size variants (32px, 64px, 128px, 256px).
/// </summary>
public interface IBrandingService
{
    /// <summary>
    /// Uploads a tenant logo, resizes it to standard size variants (32, 64, 128, 256 pixels),
    /// and stores all variants in object storage.
    /// </summary>
    /// <param name="tenantId">The identifier of the tenant.</param>
    /// <param name="logoStream">The logo image stream.</param>
    /// <param name="contentType">The MIME content type of the logo image (e.g. "image/png").</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>A <see cref="LogoUploadResult"/> containing URLs for all generated variants.</returns>
    Task<LogoUploadResult> UploadLogoAsync(
        Guid tenantId,
        Stream logoStream,
        string contentType,
        CancellationToken cancellationToken = default);
}
