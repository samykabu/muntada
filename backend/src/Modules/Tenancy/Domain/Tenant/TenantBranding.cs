using System.Text.RegularExpressions;
using Muntada.SharedKernel.Domain;
using Muntada.SharedKernel.Domain.Exceptions;

namespace Muntada.Tenancy.Domain.Tenant;

/// <summary>
/// Value object representing a tenant's visual branding configuration,
/// including logo, color scheme, and optional custom domain.
/// </summary>
public sealed partial class TenantBranding : ValueObject
{
    /// <summary>
    /// Gets the URL of the tenant's logo, or <c>null</c> if not set.
    /// </summary>
    public string? LogoUrl { get; }

    /// <summary>
    /// Gets the primary brand color as a hex string (e.g. "#FF5733"), or <c>null</c> if not set.
    /// </summary>
    public string? PrimaryColor { get; }

    /// <summary>
    /// Gets the secondary brand color as a hex string (e.g. "#C70039"), or <c>null</c> if not set.
    /// </summary>
    public string? SecondaryColor { get; }

    /// <summary>
    /// Gets the tenant's custom domain (e.g. "discuss.example.com"), or <c>null</c> if not set.
    /// </summary>
    public string? CustomDomain { get; }

    /// <summary>
    /// Gets an empty branding instance with all properties set to <c>null</c>.
    /// </summary>
    public static TenantBranding Empty { get; } = new(null, null, null, null);

    private TenantBranding(string? logoUrl, string? primaryColor, string? secondaryColor, string? customDomain)
    {
        LogoUrl = logoUrl;
        PrimaryColor = primaryColor;
        SecondaryColor = secondaryColor;
        CustomDomain = customDomain;
    }

    /// <summary>
    /// Creates a new <see cref="TenantBranding"/> instance after validating hex color formats.
    /// </summary>
    /// <param name="logoUrl">Optional URL of the tenant's logo.</param>
    /// <param name="primaryColor">Optional primary brand color in hex format (#RRGGBB).</param>
    /// <param name="secondaryColor">Optional secondary brand color in hex format (#RRGGBB).</param>
    /// <param name="customDomain">Optional custom domain for the tenant.</param>
    /// <returns>A valid <see cref="TenantBranding"/> instance.</returns>
    /// <exception cref="ValidationException">Thrown when a provided hex color is invalid.</exception>
    public static TenantBranding Create(
        string? logoUrl,
        string? primaryColor,
        string? secondaryColor,
        string? customDomain)
    {
        ValidateHexColor(primaryColor, nameof(PrimaryColor));
        ValidateHexColor(secondaryColor, nameof(SecondaryColor));

        return new TenantBranding(logoUrl, primaryColor, secondaryColor, customDomain);
    }

    /// <inheritdoc />
    protected override IEnumerable<object?> GetEqualityComponents()
    {
        yield return LogoUrl;
        yield return PrimaryColor;
        yield return SecondaryColor;
        yield return CustomDomain;
    }

    private static void ValidateHexColor(string? color, string propertyName)
    {
        if (color is not null && !HexColorRegex().IsMatch(color))
            throw new ValidationException(propertyName,
                $"'{color}' is not a valid hex color. Expected format: #RRGGBB (e.g. #FF5733).");
    }

    [GeneratedRegex(@"^#[0-9A-Fa-f]{6}$")]
    private static partial Regex HexColorRegex();
}
