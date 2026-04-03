using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Diagnostics.HealthChecks;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.ServiceDiscovery;
using OpenTelemetry;
using OpenTelemetry.Metrics;
using OpenTelemetry.Trace;

namespace Microsoft.Extensions.Hosting;

/// <summary>
/// Provides Aspire ServiceDefaults extensions for the Muntada platform.
/// Configures OpenTelemetry tracing/metrics, health check endpoints,
/// service discovery, and HTTP client resilience for all backend services.
/// </summary>
/// <remarks>
/// Every backend service MUST call <see cref="AddServiceDefaults{TBuilder}"/>
/// in its Program.cs to ensure consistent observability and resilience.
/// Constitution VI (Observability from Day One) and XII (Aspire-First).
/// </remarks>
public static class Extensions
{
    private const string HealthEndpointPath = "/health";
    private const string ReadinessEndpointPath = "/health/ready";
    private const string LivenessEndpointPath = "/health/live";

    /// <summary>
    /// Adds common Aspire service defaults: OpenTelemetry, health checks,
    /// service discovery, and HTTP client resilience.
    /// </summary>
    /// <typeparam name="TBuilder">The host application builder type.</typeparam>
    /// <param name="builder">The host application builder.</param>
    /// <returns>The builder for chaining.</returns>
    public static TBuilder AddServiceDefaults<TBuilder>(this TBuilder builder)
        where TBuilder : IHostApplicationBuilder
    {
        builder.ConfigureOpenTelemetry();
        builder.AddDefaultHealthChecks();

        builder.Services.AddServiceDiscovery();

        builder.Services.ConfigureHttpClientDefaults(http =>
        {
            // Resilience: retry, circuit breaker, timeout (Constitution VII: Explicit Over Implicit)
            http.AddStandardResilienceHandler();

            // Service discovery: connection strings injected by Aspire (FR-0.19)
            http.AddServiceDiscovery();
        });

        return builder;
    }

    /// <summary>
    /// Configures OpenTelemetry tracing, metrics, and logging.
    /// Traces are exported to the Aspire Dashboard in development
    /// and to Jaeger via OTLP in staging/production.
    /// </summary>
    /// <typeparam name="TBuilder">The host application builder type.</typeparam>
    /// <param name="builder">The host application builder.</param>
    /// <returns>The builder for chaining.</returns>
    public static TBuilder ConfigureOpenTelemetry<TBuilder>(this TBuilder builder)
        where TBuilder : IHostApplicationBuilder
    {
        builder.Logging.AddOpenTelemetry(logging =>
        {
            logging.IncludeFormattedMessage = true;
            logging.IncludeScopes = true;
        });

        builder.Services.AddOpenTelemetry()
            .WithMetrics(metrics =>
            {
                metrics.AddAspNetCoreInstrumentation()
                    .AddHttpClientInstrumentation()
                    .AddRuntimeInstrumentation();
            })
            .WithTracing(tracing =>
            {
                tracing.AddSource(builder.Environment.ApplicationName)
                    .AddAspNetCoreInstrumentation(options =>
                        // Exclude health check endpoints from tracing to reduce noise
                        options.Filter = context =>
                            !context.Request.Path.StartsWithSegments(HealthEndpointPath)
                            && !context.Request.Path.StartsWithSegments(ReadinessEndpointPath)
                            && !context.Request.Path.StartsWithSegments(LivenessEndpointPath)
                    )
                    .AddHttpClientInstrumentation();
            });

        builder.AddOpenTelemetryExporters();

        return builder;
    }

    /// <summary>
    /// Configures OTLP exporter for OpenTelemetry. In development, Aspire Dashboard
    /// receives traces automatically. In production, set OTEL_EXPORTER_OTLP_ENDPOINT.
    /// </summary>
    private static TBuilder AddOpenTelemetryExporters<TBuilder>(this TBuilder builder)
        where TBuilder : IHostApplicationBuilder
    {
        var useOtlpExporter = !string.IsNullOrWhiteSpace(
            builder.Configuration["OTEL_EXPORTER_OTLP_ENDPOINT"]);

        if (useOtlpExporter)
        {
            builder.Services.AddOpenTelemetry().UseOtlpExporter();
        }

        return builder;
    }

    /// <summary>
    /// Adds default health checks including a self-check for liveness
    /// and a readiness tag for dependency checks added by individual services.
    /// </summary>
    /// <typeparam name="TBuilder">The host application builder type.</typeparam>
    /// <param name="builder">The host application builder.</param>
    /// <returns>The builder for chaining.</returns>
    public static TBuilder AddDefaultHealthChecks<TBuilder>(this TBuilder builder)
        where TBuilder : IHostApplicationBuilder
    {
        builder.Services.AddHealthChecks()
            .AddCheck("self", () => HealthCheckResult.Healthy(), ["live"]);

        return builder;
    }

    /// <summary>
    /// Maps health check endpoints per the Muntada API contract:
    /// <list type="bullet">
    ///   <item><description>GET /health — overall health (all checks)</description></item>
    ///   <item><description>GET /health/ready — readiness probe (dependency checks)</description></item>
    ///   <item><description>GET /health/live — liveness probe (self-check only)</description></item>
    /// </list>
    /// </summary>
    /// <param name="app">The web application.</param>
    /// <returns>The application for chaining.</returns>
    public static WebApplication MapDefaultEndpoints(this WebApplication app)
    {
        // Overall health: all checks must pass
        app.MapHealthChecks(HealthEndpointPath);

        // Readiness: all checks must pass (ready to accept traffic)
        app.MapHealthChecks(ReadinessEndpointPath, new HealthCheckOptions
        {
            Predicate = _ => true
        });

        // Liveness: only self-check (process alive, not deadlocked)
        app.MapHealthChecks(LivenessEndpointPath, new HealthCheckOptions
        {
            Predicate = r => r.Tags.Contains("live")
        });

        return app;
    }
}
