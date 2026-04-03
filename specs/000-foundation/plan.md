# Implementation Plan: Foundation & Infrastructure

**Branch**: `000-foundation` | **Date**: 2026-04-03 | **Spec**: [specs/000-foundation/spec.md](../000-foundation/spec.md)
**Input**: Feature specification from `/specs/000-foundation/spec.md` (v1.1 — post-clarification)

## Summary

This plan covers the foundational infrastructure for the Muntada platform: mono-repo scaffolding (ASP.NET Core 8+ backend with Clean Architecture, React/TypeScript frontend, Helm/K8s infra), .NET Aspire 13.2 AppHost as the mandatory local development orchestrator (Docker Compose as fallback), GitHub Actions CI/CD with Docker Hub (docker.io) as the container registry, Kubernetes namespace model (dev/staging/prod), infrastructure provisioning (SQL Server, Redis, RabbitMQ, MinIO, self-hosted LiveKit OSS), and the SharedKernel library providing base entity types, opaque IDs, integration event bus, error handling middleware, and OpenTelemetry instrumentation.

### Changes from Spec Clarification (Session 2026-04-03)

- **Container Registry**: Docker Hub (docker.io) — confirmed as the registry for CI/CD image push and Kubernetes image pull
- **LiveKit**: Self-hosted LiveKit OSS in Kubernetes — contradiction resolved (was "SaaS" in assumptions, corrected to self-hosted per constitution)
- **State Management**: Redux Toolkit (RTK) with RTK Query — "or similar" ambiguity removed from FR-0.8

## Technical Context

**Language/Version**: C# / .NET 8+ (backend), TypeScript 5.x / React 18+ (frontend), Helm 3 / YAML (infrastructure)
**Primary Dependencies**: ASP.NET Core 8, Entity Framework Core 8, MassTransit (RabbitMQ), FluentValidation, Serilog, OpenTelemetry SDK, .NET Aspire 13.2, xUnit, Moq (backend); React 18, Vite, Redux Toolkit + RTK Query, Axios, ESLint, Jest, Playwright (frontend)
**Storage**: SQL Server (per-module schemas), Redis (cache/session), RabbitMQ (messaging), MinIO (S3-compatible objects)
**Testing**: xUnit + FluentAssertions (C# unit/integration), Jest + React Testing Library (frontend unit), Playwright (E2E/integration)
**Target Platform**: Self-managed Kubernetes (Linux containers), GCC region
**Project Type**: Web application (modular monolith backend + SPA frontend)
**Performance Goals**: Health checks pass within 30 seconds, Docker images < 500MB frontend / < 800MB backend, local setup < 10 minutes
**Constraints**: GCC data residency, Saudi PDPL compliance, TLS at ingress, no hardcoded secrets, Docker Hub as container registry
**Scale/Scope**: Foundation for 13 feature modules, 3 Kubernetes environments (dev/staging/prod)
**Container Registry**: Docker Hub (docker.io) — images tagged with git SHA + `latest`
**LiveKit Mode**: Self-hosted LiveKit OSS in Kubernetes (not SaaS)
**Dev Orchestration**: .NET Aspire 13.2 (mandatory), Docker Compose (fallback only)

## Constitution Check

*GATE: Must pass before Phase 0 research. Re-check after Phase 1 design.*

| Principle | Status | Notes |
|-----------|--------|-------|
| I. Modular Monolith Discipline | PASS | Mono-repo with per-module NuGet projects and SQL schemas |
| II. Server-Authoritative State | N/A | No state machines in foundation epic |
| III. API-First | PASS | Health check endpoints defined, API structure scaffolded |
| IV. Test-First | PASS | xUnit for unit tests, Playwright for integration/E2E per constitution |
| V. Invite-Only Security | N/A | No auth in foundation (deferred to Identity epic) |
| VI. Observability from Day One | PASS | OpenTelemetry via Aspire ServiceDefaults (dev) + Jaeger (staging/prod) |
| VII. Explicit Over Implicit | PASS | Opaque IDs, integration events, idempotency patterns in kernel |
| VIII. Clean Code & Documentation | PASS | Clean Architecture layers enforced, XML docs required on all public C# APIs |
| IX. Component Reusability | N/A | No frontend components in foundation (scaffolding only) |
| X. AI-Safe DB Migrations | PASS | Migrations via `dotnet ef migrations add` only, documented in runbooks |
| XI. Comprehensive Testing | PASS | xUnit unit tests mandatory, Playwright for integration, all tests pass before commit |
| XII. Aspire-First Local Dev | PASS | Aspire 13.2 AppHost orchestrates all services; Docker Compose retained as fallback |

**GATE RESULT: PASS** — No violations. Proceeding to Phase 0.

## Project Structure

### Documentation (this feature)

```text
specs/000-foundation/
├── plan.md              # This file
├── research.md          # Phase 0 output
├── data-model.md        # Phase 1 output
├── quickstart.md        # Phase 1 output
├── contracts/           # Phase 1 output (health check API contracts)
├── checklists/          # Quality validation checklist
│   └── requirements.md
└── tasks.md             # Task breakdown
```

### Source Code (repository root)

```text
aspire/
├── Muntada.AppHost/                        # .NET Aspire 13.2 orchestrator (primary local dev entry point)
│   ├── Program.cs                          # Registers all services: API, frontend, SQL, Redis, RabbitMQ, MinIO, LiveKit
│   └── Muntada.AppHost.csproj
├── Muntada.ServiceDefaults/                # Shared Aspire service defaults (OpenTelemetry, health checks, resilience)
│   ├── Extensions.cs
│   └── Muntada.ServiceDefaults.csproj

backend/
├── src/
│   ├── Muntada.Api/                        # ASP.NET Core host (Program.cs, middleware registration)
│   │   ├── Controllers/
│   │   ├── Middleware/
│   │   ├── appsettings.json
│   │   ├── appsettings.Development.json
│   │   ├── appsettings.Production.json
│   │   ├── Dockerfile
│   │   └── Muntada.Api.csproj
│   ├── Muntada.SharedKernel/               # Shared kernel NuGet package
│   │   ├── Domain/
│   │   │   ├── AggregateRoot.cs
│   │   │   ├── Entity.cs
│   │   │   ├── ValueObject.cs
│   │   │   ├── AuditedEntity.cs
│   │   │   ├── AuditLog.cs
│   │   │   ├── IDomainEvent.cs
│   │   │   ├── OpaqueIdGenerator.cs
│   │   │   └── Exceptions/
│   │   │       ├── DomainException.cs
│   │   │       ├── ValidationException.cs
│   │   │       ├── EntityNotFoundException.cs
│   │   │       └── UnauthorizedException.cs
│   │   ├── Application/
│   │   │   ├── IIntegrationEventPublisher.cs
│   │   │   └── Behaviors/
│   │   │       └── ValidationBehavior.cs
│   │   ├── Infrastructure/
│   │   │   ├── IntegrationEventPublisher.cs
│   │   │   ├── Middleware/
│   │   │   │   └── ErrorHandlingMiddleware.cs
│   │   │   └── Telemetry/
│   │   │       ├── TelemetryConfiguration.cs
│   │   │       └── ActivitySourceExtensions.cs
│   │   └── Muntada.SharedKernel.csproj
│   └── Modules/                            # Empty module directories (populated by later epics)
│       └── .gitkeep
├── tests/
│   ├── Muntada.SharedKernel.Tests/
│   │   ├── Domain/
│   │   │   ├── AggregateRootTests.cs
│   │   │   ├── OpaqueIdGeneratorTests.cs
│   │   │   ├── ValueObjectTests.cs
│   │   │   └── ExceptionTests.cs
│   │   ├── Infrastructure/
│   │   │   ├── ErrorHandlingMiddlewareTests.cs
│   │   │   └── IntegrationEventPublisherTests.cs
│   │   ├── BaseTestFixture.cs
│   │   ├── Assertions/
│   │   │   └── DomainAssertions.cs
│   │   └── Muntada.SharedKernel.Tests.csproj
│   └── Muntada.Integration.Tests/
│       ├── HealthCheckTests.cs
│       └── Muntada.Integration.Tests.csproj
├── Muntada.sln

frontend/
├── src/
│   ├── app/                                # App shell, routing, Redux store
│   ├── shared/
│   │   ├── components/                     # Reusable UI components
│   │   ├── hooks/
│   │   ├── api/                            # RTK Query API slices
│   │   └── utils/
│   ├── features/                           # Feature modules (empty, populated later)
│   ├── App.tsx
│   ├── main.tsx
│   └── vite-env.d.ts
├── tests/
│   ├── e2e/                                # Playwright E2E tests
│   └── unit/
├── public/
├── index.html
├── package.json
├── tsconfig.json
├── vite.config.ts
├── playwright.config.ts
├── eslint.config.js
├── Dockerfile
└── .env.local.template

infra/
├── helm/
│   ├── namespaces/
│   ├── sql-server/
│   ├── redis/
│   ├── rabbitmq/
│   ├── minio/
│   ├── livekit/                            # Self-hosted LiveKit OSS
│   └── muntada/                            # Main app Helm chart
├── k8s/
│   └── base/

.github/
├── workflows/
│   ├── ci.yml                              # Lint, test, build, push to Docker Hub
│   └── deploy.yml                          # Helm deploy to K8s

docker-compose.yml                          # Fallback only (Aspire is primary)
docker-compose.test.yml
Makefile
.env.local.template
.gitignore
README.md
CONTRIBUTING.md
docs/
├── architecture/
└── runbooks/
```

**Structure Decision**: `aspire/`, `backend/`, `frontend/`, and `infra/` directories at repository root. Aspire AppHost is the primary local development entry point (Constitution XII). Backend follows Clean Architecture with SharedKernel as an internal NuGet package. Frontend uses Redux Toolkit + RTK Query for state management. Docker Hub (docker.io) for CI/CD image registry. LiveKit is self-hosted in Kubernetes (not SaaS).

## Complexity Tracking

> No violations to justify. All patterns are standard for ASP.NET Core modular monolith + React SPA + Aspire orchestration.
