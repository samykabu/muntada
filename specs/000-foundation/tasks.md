# Epic 0: Foundation & Infrastructure - Task Breakdown

**Version:** 1.0
**Epic Owner:** Platform Engineering
**Last Updated:** 2026-04-03

---

## Execution Overview

This epic establishes foundational infrastructure and shared patterns. All tasks must complete before dependent modules begin. Tasks are organized into 3 phases: setup, core infrastructure, and shared kernel.

---

## Phase 1: Setup & Environment Foundation

### T001: Mono-Repo Structure Setup [P]
**User Story:** US-0.1
**Priority:** P1
**Effort:** 3 pts
**Dependencies:** None

Create directory structure for mono-repo with clear separation of backend, frontend, and infrastructure code.

**Deliverables:**
- `backend/src/SharedKernel/` (NuGet project)
- `backend/src/Modules/{ModuleName}/Domain/`, `Application/`, `Infrastructure/`, `Api/`
- `frontend/src/features/`, `frontend/src/shared/`
- `infra/helm/`, `infra/k8s/`
- `.github/workflows/`
- Root `Makefile`, `docker-compose.yml`
- `.env.local` template

**File Locations:**
- Create at: `/backend`, `/frontend`, `/infra` (project root)

**Acceptance:**
- Directory tree is correct per spec
- `.gitignore` properly excludes sensitive files
- README explains structure for new developers

---

### T002: Docker Compose Setup for Local Development [P]
**User Story:** US-0.1
**Priority:** P1
**Effort:** 5 pts
**Dependencies:** T001

Create Docker Compose file provisioning all services (SQL Server, Redis, RabbitMQ, MinIO, LiveKit) with health checks and proper ordering.

**Deliverables:**
- `docker-compose.yml` with all services
- Health check endpoints for each service
- Named volumes for persistence
- Network configuration
- Environment variables binding

**File Locations:**
- `docker-compose.yml` (project root)
- `.env.local.template` (project root)

**Acceptance:**
- All services start with `docker-compose up -d`
- All services report healthy within 60 seconds
- Volume mounts work for code hot-reload
- Can access services on localhost (SQL: 1433, Redis: 6379, RabbitMQ: 5672, MinIO: 9000, LiveKit: 7880)

---

### T003: Makefile with Local Development Targets [P]
**User Story:** US-0.1
**Priority:** P1
**Effort:** 3 pts
**Dependencies:** T001, T002

Create Makefile with convenience targets for developers.

**Deliverables:**
- `make setup` - installs deps, runs migrations, starts services
- `make up` - starts Docker Compose services
- `make down` - stops services
- `make clean` - removes volumes and containers
- `make test` - runs all tests
- `make docker-build` - builds backend and frontend images
- `make logs` - tails service logs

**File Locations:**
- `Makefile` (project root)

**Acceptance:**
- Each target completes successfully
- `make setup` completes in < 10 minutes on standard machine
- Help text is clear (`make help`)

---

### T004: Setup README and Developer Onboarding [P]
**User Story:** US-0.1
**Priority:** P1
**Effort:** 3 pts
**Dependencies:** T001, T002, T003

Create comprehensive README with step-by-step setup instructions.

**Deliverables:**
- Project overview
- Prerequisites (Docker, Make, Node, .NET SDK)
- Quickstart: `git clone`, `make setup`
- Service endpoints and URLs
- Architecture diagram (ASCII or link to Figma)
- CONTRIBUTING guide with branching strategy
- Troubleshooting section

**File Locations:**
- `README.md` (project root)
- `CONTRIBUTING.md` (project root)

**Acceptance:**
- New developer can set up in < 10 minutes
- All prerequisites clearly listed
- Common issues and solutions documented

---

## Phase 2: Kubernetes & Core Infrastructure

### T005: Kubernetes Namespace Configuration [P]
**User Story:** US-0.3
**Priority:** P1
**Effort:** 5 pts
**Dependencies:** T001

Set up Helm charts for namespace creation and RBAC.

**Deliverables:**
- Helm chart: `infra/helm/namespaces/`
- RBAC roles and bindings (dev, admin, operator personas)
- Network policies (deny-all default, explicit allow)
- Namespace quotas (CPU, memory)
- Helm values files for dev, staging, prod

**File Locations:**
- `infra/helm/namespaces/Chart.yaml`
- `infra/helm/namespaces/templates/namespace.yaml`
- `infra/helm/namespaces/templates/rbac.yaml`
- `infra/helm/namespaces/templates/network-policy.yaml`
- `infra/helm/namespaces/values-dev.yaml`
- `infra/helm/namespaces/values-staging.yaml`
- `infra/helm/namespaces/values-prod.yaml`

**Acceptance:**
- `helm install` creates 3 namespaces
- Network policies isolate traffic
- RBAC prevents unauthorized access
- Quotas are enforced

---

### T006: SQL Server Helm Chart [P]
**User Story:** US-0.4
**Priority:** P1
**Effort:** 8 pts
**Dependencies:** T005

Create Helm chart for SQL Server with persistent volumes and initialization scripts.

**Deliverables:**
- Helm chart: `infra/helm/sql-server/`
- StatefulSet configuration
- PVC for data persistence
- ConfigMap for connection strings
- Secrets for admin password
- Database initialization script
- Helm values for each environment

**File Locations:**
- `infra/helm/sql-server/Chart.yaml`
- `infra/helm/sql-server/templates/statefulset.yaml`
- `infra/helm/sql-server/templates/service.yaml`
- `infra/helm/sql-server/templates/pvc.yaml`
- `infra/helm/sql-server/templates/configmap.yaml`
- `infra/helm/sql-server/templates/secret.yaml`
- `infra/helm/sql-server/values-dev.yaml`
- `infra/helm/sql-server/values-prod.yaml`

**Acceptance:**
- SQL Server pod starts and is healthy
- Database accepts connections
- Data persists across pod restarts
- Passwords stored in Secrets (not ConfigMap)

---

### T007: Redis Helm Chart with Sentinel [P]
**User Story:** US-0.4
**Priority:** P1
**Effort:** 5 pts
**Dependencies:** T005

Create Helm chart for Redis with HA setup via Sentinel.

**Deliverables:**
- Helm chart: `infra/helm/redis/`
- StatefulSet for Redis
- StatefulSet for Sentinel
- Services for primary and replicas
- PVC for persistence
- ConfigMap for Redis config
- Helm values

**File Locations:**
- `infra/helm/redis/Chart.yaml`
- `infra/helm/redis/templates/statefulset.yaml`
- `infra/helm/redis/templates/sentinel-statefulset.yaml`
- `infra/helm/redis/templates/configmap.yaml`
- `infra/helm/redis/values-dev.yaml`
- `infra/helm/redis/values-prod.yaml`

**Acceptance:**
- Redis cluster operational
- Sentinel monitors and failover works
- Data persists
- Can connect via DNS

---

### T008: RabbitMQ Helm Chart [P]
**User Story:** US-0.4
**Priority:** P1
**Effort:** 5 pts
**Dependencies:** T005

Create Helm chart for RabbitMQ cluster.

**Deliverables:**
- Helm chart: `infra/helm/rabbitmq/`
- StatefulSet for RabbitMQ
- Service for discovery
- ConfigMap for config
- Queue and exchange declarations (init job)
- PVC for persistence

**File Locations:**
- `infra/helm/rabbitmq/Chart.yaml`
- `infra/helm/rabbitmq/templates/statefulset.yaml`
- `infra/helm/rabbitmq/templates/configmap.yaml`
- `infra/helm/rabbitmq/templates/init-job.yaml`
- `infra/helm/rabbitmq/values-dev.yaml`

**Acceptance:**
- RabbitMQ cluster stable
- Queues and exchanges created
- Can publish/consume messages
- Management UI accessible

---

### T009: MinIO Helm Chart [P]
**User Story:** US-0.4
**Priority:** P1
**Effort:** 4 pts
**Dependencies:** T005

Create Helm chart for MinIO S3-compatible storage.

**Deliverables:**
- Helm chart: `infra/helm/minio/`
- StatefulSet for MinIO
- Service
- PVC for storage
- ConfigMap with access keys
- Bucket provisioning job

**File Locations:**
- `infra/helm/minio/Chart.yaml`
- `infra/helm/minio/templates/statefulset.yaml`
- `infra/helm/minio/templates/provisioning-job.yaml`
- `infra/helm/minio/values-dev.yaml`

**Acceptance:**
- MinIO operational
- S3 API accessible
- Buckets created
- Can upload/download files

---

### T010: LiveKit Helm Chart [P]
**User Story:** US-0.4
**Priority:** P1
**Effort:** 8 pts
**Dependencies:** T005

Create Helm chart for LiveKit cluster with 2+ replicas.

**Deliverables:**
- Helm chart: `infra/helm/livekit/`
- StatefulSet for LiveKit
- Service
- ConfigMap with API keys
- Secrets for signing keys
- PVC for recordings
- Initialization job

**File Locations:**
- `infra/helm/livekit/Chart.yaml`
- `infra/helm/livekit/templates/statefulset.yaml`
- `infra/helm/livekit/templates/configmap.yaml`
- `infra/helm/livekit/templates/secret.yaml`
- `infra/helm/livekit/values-dev.yaml`
- `infra/helm/livekit/values-prod.yaml`

**Acceptance:**
- LiveKit API accessible
- Webhooks configurable
- Can create rooms and participants
- 2+ replicas running in prod

---

## CHECKPOINT: Core Infrastructure Operational

At this point, Kubernetes cluster should be fully provisioned with all stateful services running. Proceed to shared kernel.

---

## Phase 3: Shared Kernel & CI/CD

### T011: SharedKernel NuGet Project Setup [P]
**User Story:** US-0.5
**Priority:** P1
**Effort:** 3 pts
**Dependencies:** T001

Create the SharedKernel NuGet project structure.

**Deliverables:**
- `backend/src/SharedKernel/SharedKernel.csproj`
- Base namespaces: `Muntada.SharedKernel.{Domain, Application, Infrastructure}`
- Project file references common dependencies

**File Locations:**
- `backend/src/SharedKernel/SharedKernel.csproj`
- `backend/src/SharedKernel/Domain/` (directory)
- `backend/src/SharedKernel/Application/` (directory)
- `backend/src/SharedKernel/Infrastructure/` (directory)

**Acceptance:**
- Project compiles
- Can be referenced as NuGet package by other modules
- No circular dependencies

---

### T012: AggregateRoot & Entity Base Classes [P]
**User Story:** US-0.5
**Priority:** P1
**Effort:** 5 pts
**Dependencies:** T011

Implement base classes for domain entities.

**Deliverables:**
- `AggregateRoot<TId>` base class
- `Entity<TId>` base class
- `ValueObject` base class
- Version field for optimistic concurrency
- Domain event tracking

**File Locations:**
- `backend/src/SharedKernel/Domain/AggregateRoot.cs`
- `backend/src/SharedKernel/Domain/Entity.cs`
- `backend/src/SharedKernel/Domain/ValueObject.cs`
- `backend/src/SharedKernel/Domain/IDomainEvent.cs`

**Acceptance:**
- Classes compile
- Tests verify event tracking
- Equality comparison works
- Unit tests for version increment

---

### T013: OpaqueIdGenerator Implementation [P]
**User Story:** US-0.5
**Priority:** P1
**Effort:** 5 pts
**Dependencies:** T012

Implement opaque ID generation with prefix-based encoding.

**Deliverables:**
- `OpaqueIdGenerator` static class
- ID format: `{prefix}_{base32-encoded-random}`
- Validation and parsing logic
- Example: `usr_a7k2jZ9xQpR4b1m`

**File Locations:**
- `backend/src/SharedKernel/Domain/OpaqueIdGenerator.cs`

**Acceptance:**
- Generated IDs are unique
- Format matches spec
- TryParse works correctly
- Can extract prefix

---

### T014: Integration Event Publishing [P]
**User Story:** US-0.5
**Priority:** P1
**Effort:** 8 pts
**Dependencies:** T011, T013

Implement RabbitMQ-based integration event publishing.

**Deliverables:**
- `IIntegrationEvent` interface
- `IIntegrationEventPublisher` interface
- RabbitMQ implementation
- Event routing by type
- Dead letter queue support

**File Locations:**
- `backend/src/SharedKernel/Domain/IIntegrationEvent.cs`
- `backend/src/SharedKernel/Application/IIntegrationEventPublisher.cs`
- `backend/src/SharedKernel/Infrastructure/IntegrationEventPublisher.cs`

**Acceptance:**
- Events published to RabbitMQ
- Multiple subscribers can consume
- Dead letter queue configured
- No event loss on transient failures

---

### T015: Domain Exception Hierarchy [P]
**User Story:** US-0.5
**Priority:** P1
**Effort:** 3 pts
**Dependencies:** T011

Create domain exception base classes.

**Deliverables:**
- `DomainException` base class
- `ValidationException` for invalid input
- `EntityNotFoundException` for missing entities
- `UnauthorizedException` for permission errors

**File Locations:**
- `backend/src/SharedKernel/Domain/Exceptions/DomainException.cs`
- `backend/src/SharedKernel/Domain/Exceptions/ValidationException.cs`
- `backend/src/SharedKernel/Domain/Exceptions/EntityNotFoundException.cs`
- `backend/src/SharedKernel/Domain/Exceptions/UnauthorizedException.cs`

**Acceptance:**
- Exceptions compile
- Can be caught and handled
- Proper inheritance hierarchy

---

### T016: Error Handling Middleware [P]
**User Story:** US-0.5
**Priority:** P1
**Effort:** 5 pts
**Dependencies:** T015

Create ASP.NET Core middleware for translating exceptions to HTTP responses.

**Deliverables:**
- `ErrorHandlingMiddleware` class
- Exception → HTTP status code mapping
- Structured error response format
- Correlation ID tracking

**File Locations:**
- `backend/src/SharedKernel/Infrastructure/Middleware/ErrorHandlingMiddleware.cs`

**Acceptance:**
- Middleware catches exceptions
- Returns appropriate HTTP status codes
- Error responses are JSON
- Correlation IDs are included

---

### T017: AuditedEntity & Audit Logging [P]
**User Story:** US-0.5
**Priority:** P1
**Effort:** 5 pts
**Dependencies:** T012

Implement audited entity tracking.

**Deliverables:**
- `AuditedEntity<TId>` base class
- `CreatedAt`, `UpdatedAt`, `CreatedBy` fields
- `AuditLog` entity for tracking changes

**File Locations:**
- `backend/src/SharedKernel/Domain/AuditedEntity.cs`
- `backend/src/SharedKernel/Domain/AuditLog.cs`

**Acceptance:**
- Audit fields populated automatically
- Changes tracked in database
- Audit logs searchable

---

### T018: OpenTelemetry Configuration [P]
**User Story:** US-0.5
**Priority:** P1
**Effort:** 8 pts
**Dependencies:** T011

Configure OpenTelemetry for distributed tracing.

**Deliverables:**
- ActivitySource setup
- Instruments for metrics
- Jaeger exporter configuration
- HTTP middleware instrumentation
- Database query instrumentation

**File Locations:**
- `backend/src/SharedKernel/Infrastructure/Telemetry/TelemetryConfiguration.cs`
- `backend/src/SharedKernel/Infrastructure/Telemetry/ActivitySourceExtensions.cs`

**Acceptance:**
- Traces exported to Jaeger
- HTTP requests traced
- Database queries visible
- Span propagation across services

---

### T019: Fluent Validation Integration [P]
**User Story:** US-0.5
**Priority:** P1
**Effort:** 3 pts
**Dependencies:** T011, T016

Integrate FluentValidation for input validation.

**Deliverables:**
- `AbstractValidator<T>` setup
- Global validation pipeline
- Custom validators for common patterns

**File Locations:**
- `backend/src/SharedKernel/Infrastructure/Validation/ValidationBehavior.cs`

**Acceptance:**
- Validators compile
- Invalid input rejected
- Validation errors included in 400 responses

---

### T020: Unit Test Helpers & Assertions [P]
**User Story:** US-0.5
**Priority:** P1
**Effort:** 3 pts
**Dependencies:** T011

Create test helpers for common testing scenarios.

**Deliverables:**
- Base test class with common setup
- Custom assertions for domain objects
- Mocking helpers for dependencies

**File Locations:**
- `backend/src/SharedKernel.Tests/BaseTestFixture.cs`
- `backend/src/SharedKernel.Tests/Assertions/DomainAssertions.cs`

**Acceptance:**
- Test helpers compile
- Simplify unit test writing
- Documentation with examples

---

## Phase 4: Configuration & CI/CD

### T021: Environment Configuration Management [P]
**User Story:** US-0.6
**Priority:** P2
**Effort:** 5 pts
**Dependencies:** T002

Configure appsettings files for all environments.

**Deliverables:**
- `backend/appsettings.json` (base)
- `backend/appsettings.Development.json`
- `backend/appsettings.Production.json`
- Environment variable binding (nested paths)
- `.env.local` support

**File Locations:**
- `backend/appsettings.json`
- `backend/appsettings.Development.json`
- `backend/appsettings.Production.json`
- `.env.local.template`

**Acceptance:**
- Configuration loads correctly
- Environment variables override settings
- Secrets not in version control
- Fail-fast on startup if required settings missing

---

### T022: Kubernetes ConfigMap & Secrets Integration [P]
**User Story:** US-0.6
**Priority:** P2
**Effort:** 5 pts
**Dependencies:** T021, T005

Set up Kubernetes ConfigMap and Secret mounting in Helm.

**Deliverables:**
- Helm templates for ConfigMap
- Helm templates for Secrets
- Volume mounts in deployment specs
- Secret rotation support

**File Locations:**
- `infra/helm/*/templates/configmap.yaml`
- `infra/helm/*/templates/secret.yaml`

**Acceptance:**
- ConfigMaps mounted as files or env vars
- Secrets accessible to pods
- Can be updated without redeployment
- Secrets encrypted in etcd

---

### T023: GitHub Actions CI Pipeline - Linting & Tests [P]
**User Story:** US-0.2
**Priority:** P1
**Effort:** 8 pts
**Dependencies:** T001, T003

Create GitHub Actions workflow for linting and testing.

**Deliverables:**
- `.github/workflows/ci.yml`
- Frontend linting (ESLint)
- Frontend tests (Jest)
- Backend linting (StyleCop)
- Backend unit tests (xUnit)
- Backend integration tests
- Test coverage reports

**File Locations:**
- `.github/workflows/ci.yml`

**Acceptance:**
- Workflow runs on PR creation
- All checks pass on valid code
- Failing tests block merge
- Coverage reports generated

---

### T024: GitHub Actions CD Pipeline - Build & Push Images [P]
**User Story:** US-0.2
**Priority:** P1
**Effort:** 8 pts
**Dependencies:** T023

Create GitHub Actions workflow for Docker image build and push.

**Deliverables:**
- Image build steps for backend (ASP.NET Core)
- Image build steps for frontend (React)
- Push to container registry
- Tagging: git SHA + `latest`
- Multi-stage builds for small images

**File Locations:**
- `.github/workflows/ci.yml` (extended)
- `backend/Dockerfile`
- `frontend/Dockerfile`

**Acceptance:**
- Images build successfully
- Images pushed to registry
- Frontend < 500MB, backend < 800MB
- `latest` tag points to main branch

---

### T025: GitHub Actions Deployment Pipeline - Staging & Prod [P]
**User Story:** US-0.2
**Priority:** P1
**Effort:** 8 pts
**Dependencies:** T024, T005

Create GitHub Actions workflow for Kubernetes deployment.

**Deliverables:**
- `.github/workflows/deploy.yml`
- Helm chart deployment for staging
- Helm chart deployment for production
- Rollout status checking
- Rollback documentation

**File Locations:**
- `.github/workflows/deploy.yml`
- Deployment instructions in docs

**Acceptance:**
- Manual trigger or automatic on main merge
- Pods roll out successfully
- Health checks pass
- Previous release documented for rollback

---

### T026: Docker Compose for Integration Tests [P]
**User Story:** US-0.2
**Priority:** P2
**Effort:** 3 pts
**Dependencies:** T002, T023

Ensure Docker Compose supports integration test execution.

**Deliverables:**
- `docker-compose.test.yml` (optional, or use main)
- Services start before tests run
- Database migrations run automatically

**File Locations:**
- `docker-compose.test.yml` or shared config

**Acceptance:**
- Integration tests can run locally
- CI pipeline uses same setup
- Tests pass consistently

---

### T027: Health Check Endpoints [P]
**User Story:** US-0.4
**Priority:** P1
**Effort:** 3 pts
**Dependencies:** T016

Implement health check endpoints for Kubernetes probes.

**Deliverables:**
- `GET /health` - overall health
- `GET /health/ready` - readiness probe
- `GET /health/live` - liveness probe
- Each service checks dependencies

**File Locations:**
- `backend/src/SharedKernel/Api/HealthCheckController.cs`

**Acceptance:**
- Endpoints respond with 200 OK
- Kubernetes probes configured
- Liveness: false if severe error
- Readiness: false if dependencies unavailable

---

### T028: Documentation: Architecture Diagrams [P]
**User Story:** US-0.1
**Priority:** P2
**Effort:** 3 pts
**Dependencies:** T001, T005

Create architecture diagrams (C4 model or ASCII).

**Deliverables:**
- System context diagram (Muntada + external services)
- Container diagram (backend, frontend, Kubernetes)
- Component diagram (modules within backend)
- Deployment diagram (Kubernetes, RabbitMQ, etc.)

**File Locations:**
- `docs/architecture/` (diagrams as PNG or Markdown)

**Acceptance:**
- Diagrams are clear and accurate
- Easy for new developers to understand
- Linkable from README

---

### T029: Documentation: Runbooks [P]
**User Story:** US-0.1
**Priority:** P2
**Effort:** 5 pts
**Dependencies:** T005, T024

Create operational runbooks for common tasks.

**Deliverables:**
- Local development troubleshooting
- Kubernetes troubleshooting
- Database migration guide
- Backup and recovery procedures
- Scaling procedures

**File Locations:**
- `docs/runbooks/` (one file per topic)

**Acceptance:**
- Clear step-by-step instructions
- Common issues and solutions
- Contact points for escalation

---

## CHECKPOINT: Foundation Complete

All infrastructure, CI/CD, and shared kernel are operational. Ready for dependent modules to start.

---

## Success Metrics

- Developer can clone repo and run `make setup` in < 10 minutes
- All CI/CD checks pass on every PR
- Kubernetes cluster fully provisioned with all services healthy
- All 100% of developers use shared kernel base classes
- OpenTelemetry traces visible in Jaeger for all requests
- Docker images < 500MB (frontend), < 800MB (backend)
- Health checks pass within 30 seconds of startup
