using Microsoft.EntityFrameworkCore;
using Muntada.Identity.Infrastructure;
using Muntada.SharedKernel.Infrastructure.Middleware;

var builder = WebApplication.CreateBuilder(args);

// Add Aspire ServiceDefaults (OpenTelemetry, health checks, resilience, service discovery)
builder.AddServiceDefaults();

// Register controllers from all referenced modules
builder.Services.AddControllers();

// Register MediatR from the Identity module assembly
builder.Services.AddMediatR(cfg =>
    cfg.RegisterServicesFromAssembly(typeof(IdentityDbContext).Assembly));

// Register Identity DbContext
builder.Services.AddDbContext<IdentityDbContext>(options =>
    options.UseSqlServer(builder.Configuration.GetConnectionString("muntadadb")));

var app = builder.Build();

// Error handling middleware — catches domain exceptions, returns RFC 9457 Problem Details
app.UseMiddleware<ErrorHandlingMiddleware>();

// Map health check endpoints: /health, /health/ready, /health/live
app.MapDefaultEndpoints();

// Map attribute-routed controllers
app.MapControllers();

app.MapGet("/", () => "Muntada API is running.");

app.Run();
