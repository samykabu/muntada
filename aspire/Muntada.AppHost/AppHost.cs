var builder = DistributedApplication.CreateBuilder(args);

// =============================================================================
// Infrastructure Resources — provisioned as containers by Aspire
// =============================================================================

// SQL Server with per-module schemas (Constitution I: Modular Monolith)
var sqlServer = builder.AddSqlServer("sql")
    .WithDataVolume("muntada-sql-data")
    .WithLifetime(ContainerLifetime.Persistent);

var database = sqlServer.AddDatabase("muntadadb");

// Redis for caching, sessions, and SignalR backplane
var redis = builder.AddRedis("redis")
    .WithDataVolume("muntada-redis-data")
    .WithLifetime(ContainerLifetime.Persistent);

// RabbitMQ for async messaging (MassTransit integration events)
var rabbitmq = builder.AddRabbitMQ("messaging")
    .WithDataVolume("muntada-rabbitmq-data")
    .WithManagementPlugin()
    .WithLifetime(ContainerLifetime.Persistent);

// =============================================================================
// Application Services
// =============================================================================

// Backend API — references all infrastructure resources via service discovery
// Connection strings are injected by Aspire (no hardcoded values per FR-0.19)
var api = builder.AddProject<Projects.Muntada_Api>("api")
    .WithReference(database)
    .WithReference(redis)
    .WithReference(rabbitmq)
    .WaitFor(sqlServer)
    .WaitFor(redis)
    .WaitFor(rabbitmq);

// Frontend SPA — Vite dev server with proxy to backend API
// Uses Aspire.Hosting.JavaScript (Aspire 13.2+, replaces deprecated Aspire.Hosting.NodeJs)
var frontend = builder.AddJavaScriptApp("frontend", "../../frontend")
    .WithReference(api)
    .WithHttpEndpoint(port: 3000, env: "PORT")
    .WithExternalHttpEndpoints();

// =============================================================================
// Build and Run
// =============================================================================

builder.Build().Run();
