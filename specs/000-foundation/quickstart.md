# Quickstart: Foundation & Infrastructure

**Feature**: 000-foundation
**Date**: 2026-04-03

---

## Prerequisites

| Tool | Minimum Version | Purpose |
|------|----------------|---------|
| .NET SDK | 8.0+ | Backend build, Aspire AppHost |
| .NET Aspire | 13.2 | **Primary** local dev orchestrator (mandatory) |
| Docker | 24.0+ | Container runtime (used by Aspire for dependencies) |
| Docker Compose | 2.20+ | Fallback orchestration (not primary) |
| Node.js | 20 LTS+ | Frontend build and run |
| npm | 10+ | Frontend package management |
| Make | 4.0+ | Build automation |
| kubectl | 1.27+ | Kubernetes management (optional for local dev) |
| Helm | 3.12+ | Kubernetes chart management (optional for local dev) |
| Git | 2.40+ | Version control |

---

## Local Development Setup

### 1. Clone and Configure

```bash
git clone https://github.com/samykabu/muntada.git
cd muntada
cp .env.local.template .env.local
# Edit .env.local if needed (sensible defaults provided)
```

### 2. Start All Services (Aspire — Primary Method)

```bash
dotnet run --project aspire/Muntada.AppHost
```

This command:
1. Provisions all dependencies via Aspire (SQL Server, Redis, RabbitMQ, MinIO, LiveKit as containers)
2. Starts the backend API with hot-reload
3. Starts the frontend SPA with Vite dev server
4. Opens the Aspire Dashboard with service discovery, health monitoring, and distributed tracing
5. Applies database migrations automatically

**Expected time**: < 10 minutes on first run, < 2 minutes on subsequent runs.

**Aspire Dashboard**: http://localhost:18888 (auto-opens on startup)

### 2b. Start All Services (Docker Compose — Fallback Only)

```bash
make setup
```

Use this fallback only if Aspire is not available. Aspire is the mandatory primary method per Constitution XII.

### 3. Verify Services

| Service | URL | Credentials |
|---------|-----|-------------|
| Frontend | http://localhost:3000 | N/A |
| Backend API | http://localhost:5000 | N/A |
| Swagger UI | http://localhost:5000/swagger | N/A |
| Health Check | http://localhost:5000/health | N/A |
| Aspire Dashboard | http://localhost:18888 | N/A (replaces Jaeger in dev) |
| RabbitMQ Management | http://localhost:15672 | guest / guest |
| MinIO Console | http://localhost:9001 | minioadmin / minioadmin |

### 4. Run Tests

```bash
# All tests
make test

# Backend unit tests only
dotnet test backend/tests/Muntada.SharedKernel.Tests/

# Frontend unit tests only
cd frontend && npm run test:unit

# Playwright E2E tests
cd frontend && npm run test:e2e
```

### 5. Common Makefile Targets

```bash
make setup         # Full initial setup (uses Aspire)
make aspire        # Start Aspire AppHost (primary method)
make up            # Start Docker Compose services (fallback)
make down          # Stop services
make clean         # Remove all containers and volumes
make test          # Run all tests (backend + frontend)
make docker-build  # Build Docker images (push to Docker Hub)
make logs          # Tail all service logs
make help          # Show all available targets
```

---

## Adding a Database Migration

**IMPORTANT**: Never generate migrations with AI tools. Always use the EF Core CLI.

```bash
cd backend/src/Muntada.Api
dotnet ef migrations add <MigrationName> --project ../Modules/<ModuleName>/Infrastructure
```

---

## Project Structure Overview

```
muntada/
├── aspire/            # .NET Aspire 13.2 (primary local dev)
│   ├── Muntada.AppHost/           # Orchestrator entry point
│   └── Muntada.ServiceDefaults/   # Shared OTel, health, resilience
├── backend/           # ASP.NET Core 8+ (C#)
│   ├── src/
│   │   ├── Muntada.Api/           # Host application
│   │   ├── Muntada.SharedKernel/  # Shared base types
│   │   └── Modules/               # Feature modules
│   └── tests/
├── frontend/          # React 18+ (TypeScript, RTK Query)
│   ├── src/
│   └── tests/
├── infra/             # Helm charts + K8s manifests
├── docs/              # Architecture docs + runbooks
├── docker-compose.yml # Fallback only (Aspire is primary)
└── Makefile           # Build automation
```

---

## Troubleshooting

**Docker Compose won't start**: Check Docker is running and ports 1433, 5672, 6379, 9000, 7880 are free.

**Database connection fails**: Ensure SQL Server container is healthy (`docker-compose ps`). Check `.env.local` for correct connection string.

**Frontend can't reach backend**: Verify backend is running on port 5000. Check CORS configuration in `appsettings.Development.json`.

**Traces not appearing in Aspire Dashboard**: Ensure Aspire AppHost is running. ServiceDefaults automatically configures OTLP export to the Aspire Dashboard. No separate Jaeger needed for local development.

**Docker Hub push fails in CI**: Verify `DOCKERHUB_USERNAME` and `DOCKERHUB_TOKEN` secrets are configured in GitHub repository settings.
