using Microsoft.Extensions.Logging;
using Muntada.SharedKernel.Application;
using Muntada.Tenancy.Application.Services;
using Muntada.Tenancy.Domain.Events;

namespace Muntada.Tenancy.Infrastructure.Services;

/// <summary>
/// Implements threshold alerting by logging the alert and publishing
/// <see cref="UsageLimitExceededEvent"/> integration events at 95% and 100% usage.
/// </summary>
public sealed class AlertService : IAlertService
{
    private readonly IIntegrationEventPublisher _eventPublisher;
    private readonly ILogger<AlertService> _logger;

    /// <summary>
    /// Initializes a new instance of <see cref="AlertService"/>.
    /// </summary>
    /// <param name="eventPublisher">Publisher for integration events.</param>
    /// <param name="logger">Logger instance.</param>
    public AlertService(
        IIntegrationEventPublisher eventPublisher,
        ILogger<AlertService> logger)
    {
        _eventPublisher = eventPublisher;
        _logger = logger;
    }

    /// <inheritdoc />
    public async Task SendThresholdAlertAsync(Guid tenantId, string resource, int percentUsed, long currentUsage, long limit, CancellationToken ct = default)
    {
        _logger.LogWarning(
            "Usage threshold alert for tenant {TenantId}: {Resource} at {PercentUsed}%",
            tenantId, resource, percentUsed);

        // Publish integration events at critical thresholds (95% and 100%)
        if (percentUsed >= 95)
        {
            await _eventPublisher.PublishAsync(new UsageLimitExceededEvent(
                EventId: Guid.NewGuid(),
                OccurredAt: DateTimeOffset.UtcNow,
                AggregateId: tenantId.ToString(),
                AggregateType: "Tenant",
                Version: 1,
                TenantId: tenantId,
                LimitName: resource,
                CurrentUsage: currentUsage,
                MaxAllowed: limit), ct);

            _logger.LogWarning(
                "Published UsageLimitExceededEvent for tenant {TenantId}: {Resource} at {PercentUsed}% (current: {Current}, limit: {Limit})",
                tenantId, resource, percentUsed, currentUsage, limit);
        }
    }
}
