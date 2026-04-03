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

// MinIO — S3-compatible object storage for recordings, files, artifacts
var minio = builder.AddContainer("minio", "minio/minio", "latest")
    .WithArgs("server", "/data", "--console-address", ":9001")
    .WithEnvironment("MINIO_ROOT_USER", "minioadmin")
    .WithEnvironment("MINIO_ROOT_PASSWORD", "minioadmin")
    .WithVolume("muntada-minio-data", "/data")
    .WithHttpEndpoint(port: 9000, targetPort: 9000, name: "s3")
    .WithHttpEndpoint(port: 9001, targetPort: 9001, name: "console")
    .WithLifetime(ContainerLifetime.Persistent);

// LiveKit — self-hosted OSS media server (Constitution: not SaaS, GCC data residency)
var livekit = builder.AddContainer("livekit", "livekit/livekit-server", "latest")
    .WithEnvironment("LIVEKIT_KEYS", "devkey: devsecret")
    .WithHttpEndpoint(port: 7880, targetPort: 7880, name: "http")
    .WithEndpoint(port: 7881, targetPort: 7881, name: "rtc-tcp", scheme: "tcp")
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
    .WithReference(minio.GetEndpoint("s3"))
    .WithReference(livekit.GetEndpoint("http"))
    .WaitFor(sqlServer)
    .WaitFor(redis)
    .WaitFor(rabbitmq)
    .WaitFor(minio)
    .WaitFor(livekit);

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
