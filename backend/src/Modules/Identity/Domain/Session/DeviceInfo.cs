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
    public string UserAgent { get; }

    /// <summary>
    /// Gets the IP address of the client.
    /// </summary>
    public string IpAddress { get; }

    /// <summary>
    /// Gets the country code from GeoIP lookup (optional).
    /// </summary>
    public string? Country { get; }

    /// <summary>
    /// Gets the timestamp when device info was captured.
    /// </summary>
    public DateTimeOffset DetectedAt { get; }

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
