using Microsoft.Extensions.Logging;
using Muntada.Rooms.Application.Services;
using Muntada.Rooms.Domain.Occurrence;

namespace Muntada.Rooms.Infrastructure.Services;

/// <summary>
/// Stub implementation of <see cref="IGracePeriodService"/>.
/// In production, this will use MassTransit ScheduleSend to schedule delayed messages
/// that trigger room end after the grace period expires.
/// </summary>
public sealed class GracePeriodService : IGracePeriodService
{
    private readonly ILogger<GracePeriodService> _logger;

    /// <summary>
    /// Initializes a new instance of the <see cref="GracePeriodService"/> class.
    /// </summary>
    public GracePeriodService(ILogger<GracePeriodService> logger)
    {
        _logger = logger;
    }

    /// <inheritdoc />
    public Task StartGracePeriod(RoomOccurrenceId occurrenceId, int gracePeriodSeconds, CancellationToken cancellationToken = default)
    {
        _logger.LogInformation(
            "Grace period started for occurrence {OccurrenceId} with {Seconds}s timeout. " +
            "TODO: Schedule MassTransit delayed message for auto-end.",
            occurrenceId.Value,
            gracePeriodSeconds);

        // TODO: Use MassTransit ScheduleSend to schedule a GracePeriodExpiredMessage
        // that will be consumed by GracePeriodExpiryJob after gracePeriodSeconds.
        return Task.CompletedTask;
    }

    /// <inheritdoc />
    public Task CancelGracePeriod(RoomOccurrenceId occurrenceId, CancellationToken cancellationToken = default)
    {
        _logger.LogInformation(
            "Grace period cancelled for occurrence {OccurrenceId}. " +
            "TODO: Cancel the scheduled MassTransit delayed message.",
            occurrenceId.Value);

        // TODO: Cancel the previously scheduled MassTransit message using the scheduled message token.
        return Task.CompletedTask;
    }
}
