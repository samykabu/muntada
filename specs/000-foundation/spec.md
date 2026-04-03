# Epic 0: Foundation & Infrastructure

**Version:** 1.1
**Status:** Specification
**Last Updated:** 2026-04-03
**Owner:** Platform Engineering

---

## Overview

This epic establishes the foundational infrastructure, tooling, and shared patterns for the Muntada platform. It covers mono-repo scaffolding, CI/CD automation, Kubernetes namespace management, infrastructure provisioning, and shared kernel libraries that all subsequent modules depend on.

### Scope

- Mono-repo structure (backend: ASP.NET Core 8+, frontend: React/TypeScript, infrastructure: Helm/K8s)
- GitHub Actions CI/CD pipeline (lint, test, build, container push)
- Kubernetes namespace model (dev, staging, production)
- Infrastructure provisioning (SQL Server, Redis, RabbitMQ, MinIO, LiveKit cluster)
- Shared kernel (base entity types, opaque ID generation, audit event base, integration event bus, error handling middleware, OpenTelemetry)
- Environment configuration management
- .NET Aspire 13.2 AppHost as mandatory local development orchestrator (Docker Compose as fallback only)
- Aspire ServiceDefaults project for shared OpenTelemetry, health checks, and resilience configuration

---

## Clarifications

### Session 2026-04-03

- Q: Which container registry should CI/CD use? → A: Docker Hub (docker.io)
- Q: Is LiveKit self-hosted or SaaS? → A: Self-hosted LiveKit OSS in Kubernetes (per constitution, GCC data residency)
- Q: FR-0.8 state management "or similar" — what specifically? → A: Redux Toolkit (RTK) with RTK Query (resolved from research R6)

---

## User Stories

### US-0.1: Developer Environment Setup (Aspire-First)
**Priority:** P1
**Story Points:** 21
**Owner:** Platform Engineering

As a developer, I want to clone the Muntada repository and run the entire platform locally via a single `dotnet run --project AppHost` command using .NET Aspire 13.2 so that I can start contributing without lengthy setup or manual service wiring.

#### Acceptance Criteria

**Given** a developer has cloned the repository and has .NET SDK 8+ and Docker installed
**When** they run `dotnet run --project aspire/Muntada.AppHost`
**Then** Aspire provisions all required services (SQL Server, Redis, RabbitMQ, MinIO, LiveKit) as containers, starts the backend API and frontend SPA with hot-reload, and opens the Aspire Dashboard

**Given** the Aspire-orchestrated environment is running
**When** they navigate to `http://localhost:3000`
**Then** the React frontend loads and is able to connect to the backend API at `http://localhost:5000`

**Given** the Aspire Dashboard is running
**When** they navigate to `http://localhost:18888`
**Then** they can see all services with health status, distributed traces, structured logs, and metrics — without separate Jaeger setup

**Given** a developer does not have .NET SDK installed
**When** they run `make setup` as a fallback
**Then** Docker Compose provisions all services as a degraded alternative (no Aspire Dashboard, no automatic service discovery)

#### Definition of Done
- .NET Aspire 13.2 AppHost project (`aspire/Muntada.AppHost`) declares all service dependencies
- Aspire ServiceDefaults project (`aspire/Muntada.ServiceDefaults`) provides shared OpenTelemetry, health checks, and resilience configuration
- All backend services reference ServiceDefaults for consistent observability
- Docker Compose file retained as fallback for environments without .NET SDK
- Makefile with `setup`, `up`, `down`, `clean` targets (using Aspire as primary)
- `.env.local` template with sensible defaults
- README with step-by-step setup instructions (Aspire primary, Docker Compose fallback)
- Setup completes in < 10 minutes on standard developer machine
- All services health-checked and ready before startup complete
- Database migrations auto-applied on startup
- Aspire Dashboard accessible at `http://localhost:18888` with all services visible

---

### US-0.1b: Aspire AppHost & ServiceDefaults Initialization
**Priority:** P1
**Story Points:** 13
**Owner:** Platform Engineering

As a platform engineer, I want a .NET Aspire 13.2 AppHost project that orchestrates all platform services so that every developer uses a consistent, single-command local environment with built-in observability.

#### Acceptance Criteria

**Given** the Aspire AppHost project is created at `aspire/Muntada.AppHost`
**When** a developer runs `dotnet run --project aspire/Muntada.AppHost`
**Then** the following services are provisioned as containers and registered in service discovery:
- SQL Server (with per-module schemas)
- Redis (for caching and session store)
- RabbitMQ (for async messaging)
- MinIO (S3-compatible object storage)
- LiveKit (media server)
- Backend API (ASP.NET Core, with hot-reload)
- Frontend SPA (React/Vite dev server)

**Given** a new module is being created in a subsequent epic
**When** the module needs infrastructure dependencies
**Then** the module MUST register itself and its dependencies in the Aspire AppHost `Program.cs`

**Given** the Aspire ServiceDefaults project exists at `aspire/Muntada.ServiceDefaults`
**When** any backend service references it
**Then** the service automatically gets OpenTelemetry tracing, structured logging, health check endpoints, and HTTP resilience configuration

**Given** the Aspire environment is running
**When** a service fails or becomes unhealthy
**Then** the Aspire Dashboard shows the failure with logs, traces, and health status in real-time

#### Definition of Done
- `aspire/Muntada.AppHost/Muntada.AppHost.csproj` targeting .NET Aspire 13.2+
- `aspire/Muntada.AppHost/Program.cs` with all service registrations
- `aspire/Muntada.ServiceDefaults/Muntada.ServiceDefaults.csproj` with shared configuration
- `aspire/Muntada.ServiceDefaults/Extensions.cs` configuring OpenTelemetry, health checks, resilience
- Backend API project references ServiceDefaults
- All services visible in Aspire Dashboard with health status
- Service discovery works (no hardcoded connection strings in development)
- Documentation on how new modules register in AppHost

---

### US-0.2: CI/CD Pipeline Automation
**Priority:** P1
**Story Points:** 21
**Owner:** Platform Engineering

As a platform engineer, I want a GitHub Actions CI/CD pipeline that validates every pull request so that code quality and build integrity are maintained.

#### Acceptance Criteria

**Given** a developer pushes a PR to the repository
**When** GitHub Actions workflow is triggered
**Then** the following jobs run in parallel:
- Frontend linting (ESLint)
- Frontend unit tests (Jest)
- Backend code analysis (StyleCop)
- Backend unit tests (xUnit)
- Backend integration tests
- Docker image build for both backend and frontend

**Given** all checks pass
**When** the PR is merged to `main`
**Then** Docker images are automatically pushed to container registry with git SHA tag and `latest` tag

**Given** a test fails
**When** the workflow completes
**Then** a detailed report is posted as a PR comment and the merge is blocked

**Given** the `main` branch has new commits
**When** production deployment is initiated
**Then** images are deployed to the Kubernetes production namespace with automated rollout status tracking

#### Definition of Done
- GitHub Actions workflow file (`.github/workflows/ci-cd.yml`)
- Docker image build for ASP.NET Core backend
- Docker image build for React frontend
- Container registry configured (Docker Hub — docker.io)
- Test coverage reports generated and tracked
- Build artifacts (binaries, reports) retained for 30 days
- Deployment workflow for staging and production environments
- Rollback capability documented

---

### US-0.3: Kubernetes Cluster Setup
**Priority:** P1
**Story Points:** 21
**Owner:** Platform Engineering / DevOps

As an operations team member, I want the Kubernetes cluster to have separate namespaces for dev, staging, and production so that environments are isolated and resources are organized.

#### Acceptance Criteria

**Given** a fresh Kubernetes cluster is provisioned
**When** we apply the infrastructure Helm charts
**Then** three namespaces are created: `muntada-dev`, `muntada-staging`, `muntada-prod`

**Given** services are deployed to the dev namespace
**When** we run `kubectl get ns`
**Then** each namespace has appropriate network policies to isolate traffic between environments

**Given** the prod namespace is running
**When** an incident occurs and rollback is needed
**Then** we can revert to the previous Helm release with `helm rollback [release] [revision]`

**Given** a new cluster member joins
**When** they run `kubectl config use-context muntada-dev`
**Then** they have read-only access to dev by default, with elevated access requiring approval

#### Definition of Done
- Helm chart structure for each environment
- Network policies defined (deny-all by default, explicit allow rules)
- RBAC roles and bindings for different personas (dev, admin, operator)
- Namespace quotas enforced (CPU, memory, storage)
- Resource limits set on all Deployments
- Persistent volume provisioning configured for SQL Server, Redis
- Helm values files for each environment with clear separation

---

### US-0.4: Infrastructure Provisioning & Service Configuration
**Priority:** P1
**Story Points:** 34
**Owner:** Platform Engineering / DevOps

As a platform engineer, I want infrastructure provisioning to be declarative and idempotent so that we can consistently provision databases, caches, queues, and object storage across all environments.

#### Acceptance Criteria

**Given** Helm charts are applied to a Kubernetes cluster
**When** the charts complete
**Then** the following services are provisioned and accessible:
- SQL Server (with schemas per module)
- Redis cluster (for caching and session store)
- RabbitMQ cluster (for async messaging)
- MinIO (S3-compatible object storage for recordings, files)
- LiveKit cluster (with at least 2 deployment replicas)

**Given** a pod needs to connect to SQL Server
**When** it uses the connection string from the ConfigMap
**Then** the connection uses mTLS with certificates provided by cert-manager

**Given** LiveKit cluster is deployed
**When** we verify the deployment
**Then** LiveKit API is accessible at the configured endpoint with valid API key and secret provided via Kubernetes Secrets

**Given** we scale the deployment
**When** we increase replica count
**Then** database connections are pooled and managed automatically without exhausting connection limits

#### Definition of Done
- Helm chart for SQL Server with persistent volume configuration
- Helm chart for Redis with sentinel setup for HA
- Helm chart for RabbitMQ with queue declaration manifests
- Helm chart for MinIO with bucket provisioning
- Helm chart for LiveKit with proper scaling and health checks
- StatefulSet configurations with ordered startup/teardown
- Persistent volume claims with appropriate sizing for each environment
- Database initialization scripts and migration tooling
- Secrets management via Kubernetes Secrets (rotatable)
- Health check endpoints defined for all services
- Monitoring and alerting configured for infrastructure components

---

### US-0.5: Shared Kernel & Common Patterns
**Priority:** P1
**Story Points:** 21
**Owner:** Architecture / Lead Engineer

As a backend developer, I want reusable base classes and utilities in a shared kernel so that modules follow consistent patterns for entities, IDs, errors, events, and observability.

#### Acceptance Criteria

**Given** I'm implementing a new domain entity in a module
**When** I inherit from `AggregateRoot<TId>`
**Then** the entity automatically supports opaque ID generation, event sourcing hooks, and audit field tracking

**Given** I need to generate an opaque ID
**When** I call `OpaqueIdGenerator.Generate(prefix)`
**Then** I get a URL-safe encoded ID like `usr_a7k2jZ9xQpR4b1m` that is unique across the system

**Given** I raise a domain event in an aggregate
**When** the aggregate is saved via the repository
**Then** the event is automatically added to an event queue and published to RabbitMQ via `IIntegrationEventPublisher`

**Given** a request fails with a domain error
**When** the error bubbles up to the `ErrorHandlingMiddleware`
**Then** it is caught, translated to an HTTP error response with appropriate status code, and a structured log is emitted

**Given** the application is running
**When** OpenTelemetry is configured
**Then** traces are exported to a configured backend (Jaeger, DataDog, etc.) with automatic span propagation across services

#### Definition of Done
- `SharedKernel` NuGet package (internal)
- `AggregateRoot<TId>` base class with event tracking
- `OpaqueIdGenerator` with prefix-based encoding
- `IIntegrationEventPublisher` interface and RabbitMQ implementation
- Domain exception hierarchy (DomainException, ValidationException, etc.)
- `ErrorHandlingMiddleware` for translating exceptions to HTTP responses
- `AuditedEntity` base class with CreatedAt, UpdatedAt, CreatedBy tracking
- OpenTelemetry configuration (ActivitySource, Instruments)
- Fluent validation integration
- Unit test helpers and assertions

---

### US-0.6: Environment Configuration & Secrets Management
**Priority:** P2
**Story Points:** 13
**Owner:** Platform Engineering / Security

As a platform engineer, I want environment-specific configuration to be managed safely without hardcoding secrets so that the application behaves correctly in dev, staging, and production.

#### Acceptance Criteria

**Given** the application starts
**When** it reads configuration from environment variables, ConfigMaps, and Kubernetes Secrets
**Then** it loads the correct values for the environment without requiring code changes

**Given** a database password is rotated
**When** the Kubernetes Secret is updated
**Then** the application picks up the new secret within a configurable refresh window without requiring a restart

**Given** a developer wants to run tests
**When** they have a `.env.local` file in the project root
**Then** the application uses those values, but they are never committed to git

**Given** production credentials are exposed
**When** the rotation mechanism is triggered
**Then** old credentials are revoked and new ones are propagated to all pods automatically

#### Definition of Done
- `appsettings.json`, `appsettings.Development.json`, `appsettings.Production.json`
- Environment variable binding (supports nested paths like `Database__ConnectionString`)
- Kubernetes ConfigMap and Secret mounting in Helm values
- `.env.local` support with git ignore rule
- Secret rotation documentation
- Configuration validation on startup (fail fast if required settings missing)
- Encrypted storage of secrets at rest in Kubernetes etcd
- Audit logging of secret access (who, when, what)

---

## Functional Requirements

### Backend Architecture (ASP.NET Core 8+)

**FR-0.1:** The backend shall be structured as a modular monolith with each feature module in a separate NuGet project under `/src/Modules/{ModuleName}`.

**FR-0.2:** Each module shall have its own SQL Server schema (e.g., `[identity]`, `[tenancy]`, `[rooms]`) to enforce data isolation.

**FR-0.3:** The shared kernel (FR-0.1: SharedKernel) shall provide base classes for aggregates, value objects, entities, and domain events.

**FR-0.4:** All async operations shall use RabbitMQ for messaging, with a dedicated queue per module and a dead-letter queue for failed messages.

**FR-0.5:** The application shall implement OpenTelemetry instrumentation with automatic span creation for HTTP requests, database queries, and message publishing.

**FR-0.6:** Health check endpoints (`/health`, `/health/ready`, `/health/live`) shall be exposed and used by Kubernetes for liveness and readiness probes.

### Frontend Architecture (React/TypeScript)

**FR-0.7:** The frontend shall be a single-page application (SPA) built with React 18+ and TypeScript, with strict type checking enabled.

**FR-0.8:** State management shall use Redux Toolkit (RTK) with RTK Query for global and server state, with modules aligned to backend features.

**FR-0.9:** HTTP requests shall use a typed API client generated from OpenAPI/Swagger schema or hand-written with Axios/React Query for data fetching and caching.

**FR-0.10:** The frontend shall support lazy loading of feature modules and code-splitting to minimize initial bundle size.

### Local Development Orchestration (.NET Aspire 13.2)

**FR-0.16:** All projects in this workspace MUST be runnable using .NET Aspire 13.2 or above as the local development orchestrator. The Aspire AppHost project (`aspire/Muntada.AppHost`) is the single entry point for local development.

**FR-0.17:** The Aspire AppHost MUST declare all infrastructure dependencies (SQL Server, Redis, RabbitMQ, MinIO, LiveKit) as container resources, provisioned automatically on startup.

**FR-0.18:** The Aspire ServiceDefaults project (`aspire/Muntada.ServiceDefaults`) MUST provide shared configuration for OpenTelemetry tracing, structured logging, health check endpoints, and HTTP client resilience. All backend services MUST reference this project.

**FR-0.19:** Service discovery in local development MUST use Aspire's built-in service discovery — no hardcoded connection strings in `appsettings.Development.json`. Connection strings, Redis endpoints, and RabbitMQ URLs are injected by the Aspire AppHost at runtime.

**FR-0.20:** Every new module added in subsequent epics MUST register itself and its dependencies in the Aspire AppHost `Program.cs`. Failure to register is a build/review gate violation.

**FR-0.21:** Docker Compose SHALL be retained as a fallback orchestrator for environments that cannot run .NET SDK (e.g., CI containers without SDK). Docker Compose MUST NOT be the primary local development method.

### Infrastructure & Deployment

**FR-0.11:** Docker images shall be built for both backend and frontend with multi-stage builds to minimize final image size.

**FR-0.12:** Kubernetes manifests shall be managed via Helm 3, with separate values files for each environment (dev, staging, prod).

**FR-0.13:** Persistent data (SQL Server, Redis, RabbitMQ, MinIO) shall use Kubernetes StatefulSets with persistent volume claims.

**FR-0.14:** All services shall be accessible via a single ingress controller, with TLS certificates managed by cert-manager.

**FR-0.15:** Monitoring shall be implemented via Prometheus for metrics and Jaeger for distributed tracing, with Grafana dashboards for visualization.

---

## Key Entities

### Shared Kernel Entities

**AggregateRoot<TId>**
- `Id: TId`
- `Version: int` (for optimistic concurrency)
- `CreatedAt: DateTime`
- `UpdatedAt: DateTime`
- `DomainEvents: IReadOnlyList<IDomainEvent>`
- Methods: `ClearDomainEvents()`, `AddDomainEvent(event)`

**OpaqueId**
- `Value: string` (URL-safe encoded with prefix)
- `Prefix: string` (e.g., "usr", "org", "room")
- Methods: `ToString()`, `TryParse(string, out OpaqueId)`

**IntegrationEvent**
- `EventId: Guid`
- `Timestamp: DateTime`
- `Version: int`
- `AggregateId: string`
- Methods: `GetType()` (for routing)

**AuditLog**
- `EntityType: string`
- `EntityId: string`
- `Action: string` (Create, Update, Delete)
- `ChangedBy: string` (User ID)
- `ChangedAt: DateTime`
- `Changes: Dictionary<string, object>` (before/after values)

---

## Success Criteria

- Developer can clone repo and run `dotnet run --project aspire/Muntada.AppHost` to have a fully functional local environment in < 10 minutes
- Aspire Dashboard shows all services with health status, traces, and logs at `http://localhost:18888`
- No hardcoded connection strings in development — all injected via Aspire service discovery
- All PRs build and test successfully before merge
- Kubernetes cluster has 3 isolated namespaces with proper RBAC
- All infrastructure components are provisioned via Helm charts (no manual setup) in staging/production
- 100% of domain entities use shared kernel base classes
- OpenTelemetry traces visible via Aspire Dashboard (dev) and Jaeger (staging/prod) for all requests and background jobs
- Configuration is environment-specific with no hardcoded secrets
- Docker images are < 500MB for frontend, < 800MB for backend
- Health checks pass for all services within 30 seconds of startup
- Database migrations can be applied and rolled back safely
- Every new module registers in Aspire AppHost upon creation

---

## Edge Cases

1. **Partial Infrastructure Failure:** If Redis becomes unavailable, session cache falls back to database. If RabbitMQ is down, events are persisted to database and retried when queue recovers.

2. **Schema Drift:** Multiple database schema versions running simultaneously (blue-green deployments). Migrations must be forward and backward compatible.

3. **Concurrent Secret Rotation:** If multiple pods attempt to refresh secrets simultaneously, coordination via distributed lock prevents conflicts.

4. **Container Registry Outage:** CI/CD pipeline handles image pull errors gracefully and retries with exponential backoff.

5. **Namespace Resource Exhaustion:** If a namespace exceeds its quota, new pods remain Pending until resources are freed. This is monitored and alerted.

6. **Network Partition:** Services in different zones may lose connectivity. Circuit breakers and timeouts prevent cascading failures.

---

## Assumptions

1. **Kubernetes Cluster:** A self-hosted Kubernetes cluster (1.27+) is provisioned and accessible via `kubeconfig`.

2. **Container Registry:** Docker Hub (docker.io) is used as the container registry, accessible to the CI/CD pipeline and Kubernetes nodes.

3. **DNS:** Kubernetes DNS (`coredns`) is functional and service discovery works via DNS names.

4. **Storage:** Persistent volumes are backed by reliable storage (NFS, cloud block storage, etc.) with daily snapshots for disaster recovery.

5. **Networking:** Kubernetes cluster has outbound internet access for fetching Docker images. LiveKit is self-hosted within the cluster (not SaaS) per constitution requirements.

6. **Monitoring:** Prometheus and Jaeger are deployed in the cluster or externally accessible for metrics and tracing.

7. **Security:** TLS is terminated at the ingress controller. Certificate management (Let's Encrypt or internal CA) is in place.

8. **Developer Machine:** Developers have .NET SDK 8+, Docker, .NET Aspire 13.2 workload, Make, kubectl, and Helm installed locally.

9. **Git:** Repository uses GitHub with standard branching strategy (main, develop, feature branches).

10. **Compliance:** GCC region requirements and Saudi PDPL regulations are satisfied by design (data residency, encryption, audit logging).

---

## Implementation Notes

- **Aspire AppHost:** Primary local development entry point. `aspire/Muntada.AppHost/Program.cs` declares all service dependencies. Run with `dotnet run --project aspire/Muntada.AppHost`. Aspire 13.2 or above is mandatory.
- **Aspire ServiceDefaults:** `aspire/Muntada.ServiceDefaults` provides shared OpenTelemetry, health checks, and HTTP resilience. All backend services MUST reference this project.
- **Mono-repo Structure:** Use a single git repository with clear folder hierarchy: `/aspire`, `/backend`, `/frontend`, `/infra/helm`, `/docs`.
- **Docker Compose:** Retained as fallback only. Include all services with proper health checks and startup dependencies (`depends_on` with conditions).
- **Makefile:** Provide convenient targets for local development (`make setup` using Aspire, `make test`, `make docker-build`).
- **OpenTelemetry:** Configured centrally in ServiceDefaults project. Aspire Dashboard replaces Jaeger for local development.
- **Error Handling:** Use structured logging (Serilog) with correlation IDs for request tracing.
- **Testing:** Unit tests in each module, integration tests that spin up dependencies via Aspire (or Docker Compose in CI).
- **Documentation:** Keep README and CONTRIBUTING guide updated with architecture diagrams and runbooks.
- **Module Registration:** Every new module MUST be registered in `aspire/Muntada.AppHost/Program.cs` upon creation.
