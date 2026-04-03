namespace Muntada.Identity.Api.Dtos;

/// <summary>
/// Response DTO representing a user session.
/// </summary>
/// <param name="SessionId">The unique identifier of the session.</param>
/// <param name="DeviceUserAgent">The User-Agent string of the session's device.</param>
/// <param name="DeviceIpAddress">The IP address of the session's device.</param>
/// <param name="DeviceCountry">The country code from GeoIP lookup (may be null).</param>
/// <param name="CreatedAt">The UTC timestamp when the session was created.</param>
/// <param name="LastActivityAt">The UTC timestamp of the last activity on this session.</param>
/// <param name="IsCurrent">Whether this is the current session making the request.</param>
public sealed record SessionResponse(
    Guid SessionId,
    string DeviceUserAgent,
    string DeviceIpAddress,
    string? DeviceCountry,
    DateTimeOffset CreatedAt,
    DateTimeOffset? LastActivityAt,
    bool IsCurrent);
