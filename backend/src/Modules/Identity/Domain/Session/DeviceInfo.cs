using Muntada.SharedKernel.Domain;

namespace Muntada.Identity.Domain.Session;

/// <summary>
/// Value object capturing device information for session tracking.
/// Includes User-Agent, IP address, and optional geolocation.
/// </summary>
public sealed class DeviceInfo : ValueObject
{
    /// <summary>
    /// Gets the User-Agent string from the client.
    /// </summary>
    public string UserAgent { get; private set; } = null!;

    /// <summary>
    /// Gets the IP address of the client.
    /// </summary>
    public string IpAddress { get; private set; } = null!;

    /// <summary>
    /// Gets the country code from GeoIP lookup (optional).
    /// </summary>
    public string? Country { get; private set; }

    /// <summary>
    /// Gets the timestamp when device info was captured.
    /// </summary>
    public DateTimeOffset DetectedAt { get; private set; }

    private DeviceInfo() { } // EF Core

    /// <summary>
    /// Creates a new <see cref="DeviceInfo"/> instance.
    /// </summary>
    public DeviceInfo(string userAgent, string ipAddress, string? country = null)
    {
        UserAgent = userAgent ?? throw new ArgumentNullException(nameof(userAgent));
        IpAddress = ipAddress ?? throw new ArgumentNullException(nameof(ipAddress));
        Country = country;
        DetectedAt = DateTimeOffset.UtcNow;
    }

    /// <inheritdoc />
    protected override IEnumerable<object?> GetEqualityComponents()
    {
        yield return UserAgent;
        yield return IpAddress;
        yield return Country;
    }
}
