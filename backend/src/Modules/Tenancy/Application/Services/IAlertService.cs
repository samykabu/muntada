namespace Muntada.Tenancy.Application.Services;

/// <summary>
/// Service for sending threshold alerts when a tenant's resource usage
/// approaches or exceeds their plan limits.
/// </summary>
public interface IAlertService
{
    /// <summary>
    /// Sends an alert when a tenant's resource usage crosses a threshold.
    /// Publishes a <see cref="Domain.Events.UsageLimitExceededEvent"/> at 95% and 100% usage.
    /// </summary>
    /// <param name="tenantId">The identifier of the tenant.</param>
    /// <param name="resource">The resource type that triggered the alert (e.g. "rooms", "storage").</param>
    /// <param name="percentUsed">The current usage percentage (0-100+).</param>
    /// <param name="currentUsage">The current usage value for the resource.</param>
    /// <param name="limit">The maximum allowed value for the resource.</param>
    /// <param name="ct">Cancellation token.</param>
    /// <returns>A task representing the asynchronous operation.</returns>
    Task SendThresholdAlertAsync(Guid tenantId, string resource, int percentUsed, long currentUsage, long limit, CancellationToken ct = default);
}
