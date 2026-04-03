var builder = WebApplication.CreateBuilder(args);

// Add Aspire ServiceDefaults (OpenTelemetry, health checks, resilience, service discovery)
builder.AddServiceDefaults();

var app = builder.Build();

// Map health check endpoints: /health, /health/ready, /health/live
app.MapDefaultEndpoints();

app.MapGet("/", () => "Muntada API is running.");

app.Run();
