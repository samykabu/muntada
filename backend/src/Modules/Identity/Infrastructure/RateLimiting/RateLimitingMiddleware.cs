using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;

namespace Muntada.Identity.Infrastructure.RateLimiting;

/// <summary>
/// ASP.NET Core middleware for rate limiting authentication endpoints.
/// Uses a sliding window algorithm with configurable limits per endpoint.
/// State is stored in-memory (Redis integration deferred to infrastructure).
/// </summary>
public sealed class RateLimitingMiddleware
{
    private readonly RequestDelegate _next;
    private readonly ILogger<RateLimitingMiddleware> _logger;
    private readonly IRateLimitStore _store;

    /// <summary>
    /// Initializes a new instance of <see cref="RateLimitingMiddleware"/>.
    /// </summary>
    public RateLimitingMiddleware(RequestDelegate next, ILogger<RateLimitingMiddleware> logger, IRateLimitStore store)
    {
        _next = next;
        _logger = logger;
        _store = store;
    }

    /// <summary>
    /// Processes the request, checking rate limits for configured endpoints.
    /// </summary>
    public async Task InvokeAsync(HttpContext context)
    {
        var policy = RateLimitPolicies.GetPolicy(context.Request.Path, context.Request.Method);
        if (policy is null)
        {
            await _next(context);
            return;
        }

        var key = policy.ExtractKey(context);
        var isAllowed = await _store.IsAllowedAsync(key, policy.MaxAttempts, policy.Window);

        if (!isAllowed)
        {
            _logger.LogWarning("Rate limit exceeded for {Key} on {Path}",
                key, context.Request.Path);

            context.Response.StatusCode = StatusCodes.Status429TooManyRequests;
            context.Response.Headers.RetryAfter = policy.Window.TotalSeconds.ToString();
            await context.Response.WriteAsJsonAsync(new
            {
                type = "https://tools.ietf.org/html/rfc9457",
                title = "Too Many Requests",
                status = 429,
                detail = $"Rate limit exceeded. Try again in {policy.Window.TotalMinutes} minutes."
            });
            return;
        }

        await _next(context);
    }
}

/// <summary>
/// Interface for rate limit state storage (Redis or in-memory).
/// </summary>
public interface IRateLimitStore
{
    /// <summary>
    /// Checks if the request is allowed and increments the counter.
    /// </summary>
    /// <param name="key">The rate limit key (e.g., "login:{email}").</param>
    /// <param name="maxAttempts">Maximum attempts in the window.</param>
    /// <param name="window">The sliding window duration.</param>
    /// <returns><c>true</c> if allowed; <c>false</c> if rate limited.</returns>
    Task<bool> IsAllowedAsync(string key, int maxAttempts, TimeSpan window);
}

/// <summary>
/// In-memory rate limit store for development. Replace with Redis in production.
/// </summary>
public sealed class InMemoryRateLimitStore : IRateLimitStore
{
    private readonly Dictionary<string, List<DateTimeOffset>> _store = new();

    /// <inheritdoc />
    public Task<bool> IsAllowedAsync(string key, int maxAttempts, TimeSpan window)
    {
        lock (_store)
        {
            var now = DateTimeOffset.UtcNow;
            if (!_store.ContainsKey(key))
                _store[key] = new List<DateTimeOffset>();

            _store[key].RemoveAll(t => t < now - window);
            if (_store[key].Count >= maxAttempts)
                return Task.FromResult(false);

            _store[key].Add(now);
            return Task.FromResult(true);
        }
    }
}
