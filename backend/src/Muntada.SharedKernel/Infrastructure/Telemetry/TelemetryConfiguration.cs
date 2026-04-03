using System.Diagnostics;

namespace Muntada.SharedKernel.Infrastructure.Telemetry;

/// <summary>
/// Provides centralized OpenTelemetry configuration for the Muntada platform.
/// Defines the <see cref="ActivitySource"/> used for custom spans across all modules.
/// In development, traces are exported to the Aspire Dashboard via OTLP.
/// In staging/production, traces are exported to Jaeger.
/// </summary>
public static class TelemetryConfiguration
{
    /// <summary>
    /// The service name used for OpenTelemetry resource identification.
    /// </summary>
    public const string ServiceName = "Muntada";

    /// <summary>
    /// The shared <see cref="ActivitySource"/> for creating custom spans
    /// in Muntada application code. All modules should use this source.
    /// </summary>
    public static readonly ActivitySource ActivitySource = new(ServiceName);
}
