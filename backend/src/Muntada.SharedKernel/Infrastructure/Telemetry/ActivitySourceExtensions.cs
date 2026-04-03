using System.Diagnostics;

namespace Muntada.SharedKernel.Infrastructure.Telemetry;

/// <summary>
/// Convenience extension methods for creating OpenTelemetry spans
/// using the shared <see cref="TelemetryConfiguration.ActivitySource"/>.
/// </summary>
public static class ActivitySourceExtensions
{
    /// <summary>
    /// Starts a new activity (span) with the given operation name.
    /// Returns null if no listener is registered (no-op when tracing is disabled).
    /// </summary>
    /// <param name="source">The activity source.</param>
    /// <param name="operationName">Name of the operation being traced.</param>
    /// <param name="kind">The kind of activity (default: Internal).</param>
    /// <returns>The started activity, or null if no listener.</returns>
    public static Activity? StartOperation(
        this ActivitySource source,
        string operationName,
        ActivityKind kind = ActivityKind.Internal)
    {
        return source.StartActivity(operationName, kind);
    }

    /// <summary>
    /// Starts a new activity with tags for a domain operation.
    /// Includes the aggregate type and ID as span attributes.
    /// </summary>
    /// <param name="source">The activity source.</param>
    /// <param name="operationName">Name of the operation.</param>
    /// <param name="aggregateType">The type of aggregate being operated on.</param>
    /// <param name="aggregateId">The ID of the aggregate.</param>
    /// <returns>The started activity, or null if no listener.</returns>
    public static Activity? StartDomainOperation(
        this ActivitySource source,
        string operationName,
        string aggregateType,
        string aggregateId)
    {
        var activity = source.StartActivity(operationName, ActivityKind.Internal);
        activity?.SetTag("muntada.aggregate.type", aggregateType);
        activity?.SetTag("muntada.aggregate.id", aggregateId);
        return activity;
    }
}
