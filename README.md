# Muntada

Private, invite-only virtual meeting rooms for the GCC region.

## Prerequisites

| Tool | Version | Purpose |
|------|---------|---------|
| .NET SDK | 10.0+ | Backend, Aspire AppHost |
| .NET Aspire | 13.2+ | **Primary** local dev orchestrator |
| Docker | 24.0+ | Container runtime (used by Aspire) |
| Node.js | 20 LTS+ | Frontend build |
| npm | 10+ | Package management |
| Make | 4.0+ | Build automation |

## Quick Start

```bash
git clone https://github.com/samykabu/muntada.git
cd muntada
cp .env.local.template .env.local

# Primary method — Aspire (recommended)
dotnet run --project aspire/Muntada.AppHost

# Fallback — Docker Compose
make up
```

**Aspire Dashboard**: http://localhost:18888

## Services

| Service | URL |
|---------|-----|
| Frontend | http://localhost:3000 |
| Backend API | http://localhost:5000 |
| Health Check | http://localhost:5000/health |
| Aspire Dashboard | http://localhost:18888 |
| RabbitMQ Mgmt | http://localhost:15672 |
| MinIO Console | http://localhost:9001 |

## Project Structure

```
muntada/
├── aspire/            # .NET Aspire 13.2 orchestrator
├── backend/           # ASP.NET Core 10 (C#)
│   ├── src/Muntada.Api/           # API host
│   ├── src/Muntada.SharedKernel/  # Shared domain types
│   └── tests/
├── frontend/          # React 19 + TypeScript + Vite
├── infra/helm/        # Kubernetes Helm charts
├── docs/              # Architecture + runbooks
└── specs/             # Feature specifications
```

## Development

```bash
make test          # Run all tests
make test-backend  # Backend tests only (xUnit)
make test-e2e      # Playwright E2E tests
make docker-build  # Build Docker images
make help          # Show all targets
```

## Architecture

- **Backend**: ASP.NET Core 10 modular monolith, Clean Architecture
- **Frontend**: React 19 + Redux Toolkit + RTK Query
- **Dev**: .NET Aspire 13.2 (primary), Docker Compose (fallback)
- **Deploy**: Kubernetes + Helm, Docker Hub registry
- **Data**: SQL Server, Redis, RabbitMQ, MinIO
- **Media**: LiveKit OSS (self-hosted)
- **Observability**: OpenTelemetry, Aspire Dashboard (dev), Jaeger (prod)
