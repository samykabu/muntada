using Microsoft.EntityFrameworkCore;
using Muntada.Identity.Infrastructure;
using Muntada.Rooms.Infrastructure;
using Muntada.SharedKernel.Infrastructure.Middleware;
using Muntada.Tenancy.Application.Services;
using Muntada.Tenancy.Infrastructure;
using Muntada.Tenancy.Infrastructure.Services;

var builder = WebApplication.CreateBuilder(args);

// Add Aspire ServiceDefaults (OpenTelemetry, health checks, resilience, service discovery)
builder.AddServiceDefaults();

// Register controllers from all referenced modules
builder.Services.AddControllers();

// Register MediatR from Identity, Tenancy, and Rooms module assemblies
builder.Services.AddMediatR(cfg =>
    cfg.RegisterServicesFromAssemblies(
        typeof(IdentityDbContext).Assembly,
        typeof(TenancyDbContext).Assembly,
        typeof(RoomsDbContext).Assembly));

// Register Identity DbContext
builder.Services.AddDbContext<IdentityDbContext>(options =>
    options.UseSqlServer(builder.Configuration.GetConnectionString("muntadadb")));

// Register Tenancy DbContext
builder.Services.AddDbContext<TenancyDbContext>(options =>
    options.UseSqlServer(builder.Configuration.GetConnectionString("muntadadb")));

// Register Rooms DbContext
builder.Services.AddDbContext<RoomsDbContext>(options =>
    options.UseSqlServer(builder.Configuration.GetConnectionString("muntadadb")));

// Register Tenancy application services
builder.Services.AddScoped<IPlanLimitService, PlanLimitService>();
builder.Services.AddScoped<ITenantContext, TenantContextAccessor>();
builder.Services.AddScoped<IBrandingService, BrandingService>();
builder.Services.AddScoped<IFeatureToggleService, FeatureToggleService>();
builder.Services.AddScoped<IAlertService, AlertService>();
builder.Services.AddMemoryCache();

// Register SignalR for real-time room participant updates
builder.Services.AddSignalR();

var app = builder.Build();

// Error handling middleware — catches domain exceptions, returns RFC 9457 Problem Details
app.UseMiddleware<ErrorHandlingMiddleware>();

// Map health check endpoints: /health, /health/ready, /health/live
app.MapDefaultEndpoints();

// Map attribute-routed controllers
app.MapControllers();

// Map SignalR hubs
app.MapHub<Muntada.Rooms.Api.Hubs.RoomHub>("/hubs/rooms");

app.MapGet("/", () => "Muntada API is running.");

app.Run();
