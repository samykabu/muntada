using Microsoft.AspNetCore.Http;

namespace Muntada.Identity.Infrastructure.RateLimiting;

/// <summary>
/// Defines rate limit policies for Identity module endpoints.
/// </summary>
public static class RateLimitPolicies
{
    private static readonly List<RateLimitPolicy> Policies = new()
    {
        new("/api/v1/identity/auth/login", "POST", 5, TimeSpan.FromMinutes(15),
            ctx => $"login:{ctx.Request.Headers["X-Forwarded-For"].FirstOrDefault() ?? ctx.Connection.RemoteIpAddress?.ToString() ?? "unknown"}"),
        new("/api/v1/identity/auth/otp/challenge", "POST", 3, TimeSpan.FromMinutes(15),
            ctx => $"otp:{ctx.Request.Headers["X-Forwarded-For"].FirstOrDefault() ?? ctx.Connection.RemoteIpAddress?.ToString() ?? "unknown"}"),
        new("/api/v1/identity/auth/forgot-password", "POST", 3, TimeSpan.FromHours(1),
            ctx => $"reset:{ctx.Request.Headers["X-Forwarded-For"].FirstOrDefault() ?? ctx.Connection.RemoteIpAddress?.ToString() ?? "unknown"}"),
        new("/api/v1/identity/magic-links", "POST", 10, TimeSpan.FromDays(1),
            ctx => $"magic-link:{ctx.Request.Headers["X-Forwarded-For"].FirstOrDefault() ?? ctx.Connection.RemoteIpAddress?.ToString() ?? "unknown"}"),
    };

    /// <summary>
    /// Gets the rate limit policy for the given path and method, or null if none applies.
    /// </summary>
    public static RateLimitPolicy? GetPolicy(string path, string method)
    {
        return Policies.FirstOrDefault(p =>
            path.StartsWith(p.PathPrefix, StringComparison.OrdinalIgnoreCase)
            && string.Equals(p.Method, method, StringComparison.OrdinalIgnoreCase));
    }
}

/// <summary>
/// Defines a rate limit policy for an endpoint.
/// </summary>
/// <param name="PathPrefix">The URL path prefix to match.</param>
/// <param name="Method">The HTTP method to match.</param>
/// <param name="MaxAttempts">Maximum allowed attempts in the window.</param>
/// <param name="Window">The sliding window duration.</param>
/// <param name="ExtractKey">Function to extract the rate limit key from the HTTP context.</param>
public sealed record RateLimitPolicy(
    string PathPrefix,
    string Method,
    int MaxAttempts,
    TimeSpan Window,
    Func<HttpContext, string> ExtractKey);
