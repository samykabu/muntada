using Muntada.SharedKernel.Infrastructure.Middleware;

var builder = WebApplication.CreateBuilder(args);

// Add Aspire ServiceDefaults (OpenTelemetry, health checks, resilience, service discovery)
builder.AddServiceDefaults();

var app = builder.Build();

// Error handling middleware — catches domain exceptions, returns RFC 9457 Problem Details
app.UseMiddleware<ErrorHandlingMiddleware>();

// Map health check endpoints: /health, /health/ready, /health/live
app.MapDefaultEndpoints();

app.MapGet("/", () => "Muntada API is running.");

app.Run();
