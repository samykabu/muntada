using MassTransit;
using MediatR;
using Microsoft.Extensions.Logging;
using Muntada.Rooms.Application.Commands;
using Muntada.Rooms.Domain.Occurrence;

namespace Muntada.Rooms.Application.BackgroundJobs;

/// <summary>
/// Message representing a grace period expiry for a room occurrence.
/// Published by the grace period scheduler when the timeout elapses.
/// </summary>
public sealed record GracePeriodExpiredMessage
{
    /// <summary>Gets the tenant identifier.</summary>
    public string TenantId { get; init; } = default!;

    /// <summary>Gets the occurrence identifier whose grace period expired.</summary>
    public string OccurrenceId { get; init; } = default!;
}

/// <summary>
/// MassTransit consumer that handles grace period expiry messages.
/// Ends the room if it is still in Grace status when the message is received.
/// </summary>
public sealed class GracePeriodExpiryJob : IConsumer<GracePeriodExpiredMessage>
{
    private readonly ISender _sender;
    private readonly ILogger<GracePeriodExpiryJob> _logger;

    /// <summary>
    /// Initializes a new instance of the <see cref="GracePeriodExpiryJob"/> class.
    /// </summary>
    public GracePeriodExpiryJob(ISender sender, ILogger<GracePeriodExpiryJob> logger)
    {
        _sender = sender;
        _logger = logger;
    }

    /// <inheritdoc />
    public async Task Consume(ConsumeContext<GracePeriodExpiredMessage> context)
    {
        var message = context.Message;
        _logger.LogInformation(
            "Grace period expired for occurrence {OccurrenceId}. Attempting to end room.",
            message.OccurrenceId);

        try
        {
            var command = new TransitionRoomStatusCommand(
                message.TenantId,
                message.OccurrenceId,
                RoomTrigger.EndRoom);

            await _sender.Send(command, context.CancellationToken);

            _logger.LogInformation(
                "Room {OccurrenceId} ended after grace period expiry.",
                message.OccurrenceId);
        }
        catch (Exception ex)
        {
            // If the room is no longer in Grace (moderator reconnected), the transition
            // will fail. This is expected and should not be treated as an error.
            _logger.LogWarning(ex,
                "Could not end room {OccurrenceId} after grace period expiry. " +
                "The room may have already transitioned to a different status.",
                message.OccurrenceId);
        }
    }
}
