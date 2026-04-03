# Tasks: Foundation & Infrastructure

**Input**: Design documents from `/specs/000-foundation/`
**Prerequisites**: plan.md (v1.1, post-clarification), spec.md (v1.1), research.md, data-model.md, contracts/
**Version**: 2.0 — Updated with Aspire 13.2, Docker Hub, self-hosted LiveKit, RTK Query
**Last Updated**: 2026-04-03

**Tests**: Unit tests are MANDATORY per Constitution XI. Integration tests use Playwright.

**Organization**: Tasks are grouped by user story to enable independent implementation and testing. Foundation epic user stories are infrastructure-focused (not end-user-focused), so phases map to functional areas.

## Format: `[ID] [P?] [Story] Description`

- **[P]**: Can run in parallel (different files, no dependencies)
- **[Story]**: Which user story this task belongs to (US0.1, US0.1b, US0.2, etc.)
- Include exact file paths in descriptions

---

## Phase 1: Setup & Mono-Repo Structure

**Purpose**: Project initialization, directory scaffolding, solution file
**User Story**: US-0.1

- [x] T001 Create mono-repo directory structure: `aspire/`, `backend/src/`, `backend/tests/`, `frontend/src/`, `frontend/tests/`, `infra/helm/`, `infra/k8s/`, `docs/architecture/`, `docs/runbooks/`, `.github/workflows/`
- [x] T002 [P] Create `backend/Muntada.sln` solution file with SharedKernel, Api, and test project references
- [x] T003 [P] Create `.gitignore` with rules for .NET, Node, Docker, IDE files, `.env.local`, `*.user`
- [x] T004 [P] Create `.env.local.template` with sensible defaults for all service connection strings
- [x] T005 [P] Create `.editorconfig` with C# and TypeScript formatting rules (tabs/spaces, line endings)

**Checkpoint**: Directory structure exists, solution file compiles (empty projects).

---

## Phase 2: Aspire AppHost & ServiceDefaults (Blocking)

**Purpose**: Primary local development orchestrator — MUST complete before any service can run locally
**User Story**: US-0.1b

**Independent Test**: Run `dotnet run --project aspire/Muntada.AppHost` and verify Aspire Dashboard opens at `http://localhost:18888`

- [x] T006 [US0.1b] Create `aspire/Muntada.AppHost/Muntada.AppHost.csproj` targeting .NET Aspire 13.2+ with Aspire.Hosting references
- [x] T007 [US0.1b] Create `aspire/Muntada.AppHost/Program.cs` registering all container resources: SQL Server, Redis, RabbitMQ, MinIO, LiveKit
- [x] T008 [US0.1b] Create `aspire/Muntada.ServiceDefaults/Muntada.ServiceDefaults.csproj` with OpenTelemetry, health check, and resilience NuGet packages
- [x] T009 [US0.1b] Create `aspire/Muntada.ServiceDefaults/Extensions.cs` with `AddServiceDefaults()` and `MapDefaultEndpoints()` extension methods configuring: OpenTelemetry tracing + metrics, health check endpoints (`/health`, `/health/ready`, `/health/live`), HTTP client resilience (retry, circuit breaker, timeout), structured logging via Serilog
- [x] T010 [US0.1b] Add Backend API project reference to AppHost `Program.cs` and wire service discovery (connection strings injected by Aspire, not hardcoded)
- [x] T011 [US0.1b] Add Frontend SPA to AppHost as npm project or container resource with Vite dev server
- [x] T012 [US0.1b] Write unit tests for `Extensions.cs` service registration in `backend/tests/Muntada.SharedKernel.Tests/Infrastructure/ServiceDefaultsTests.cs`

**Checkpoint**: `dotnet run --project aspire/Muntada.AppHost` starts all services. Dashboard shows healthy status.

---

## Phase 3: Shared Kernel — Domain Layer

**Purpose**: Core domain base classes that all modules depend on
**User Story**: US-0.5

**Independent Test**: `dotnet test backend/tests/Muntada.SharedKernel.Tests/` passes with 100% coverage on domain types

### Unit Tests (Write FIRST per Constitution IV)

- [ ] T013 [P] [US0.5] Write unit tests for `Entity<TId>` equality in `backend/tests/Muntada.SharedKernel.Tests/Domain/EntityTests.cs`
- [ ] T014 [P] [US0.5] Write unit tests for `AggregateRoot<TId>` (version increment, domain event tracking) in `backend/tests/Muntada.SharedKernel.Tests/Domain/AggregateRootTests.cs`
- [ ] T015 [P] [US0.5] Write unit tests for `ValueObject` equality in `backend/tests/Muntada.SharedKernel.Tests/Domain/ValueObjectTests.cs`
- [ ] T016 [P] [US0.5] Write unit tests for `OpaqueIdGenerator` (uniqueness, prefix validation, TryParse) in `backend/tests/Muntada.SharedKernel.Tests/Domain/OpaqueIdGeneratorTests.cs`
- [ ] T017 [P] [US0.5] Write unit tests for domain exceptions in `backend/tests/Muntada.SharedKernel.Tests/Domain/ExceptionTests.cs`

### Implementation

- [ ] T018 [P] [US0.5] Create `Muntada.SharedKernel.csproj` at `backend/src/Muntada.SharedKernel/` with references to Aspire ServiceDefaults
- [ ] T019 [P] [US0.5] Implement `Entity<TId>` base class in `backend/src/Muntada.SharedKernel/Domain/Entity.cs` — XML docs on all public members
- [ ] T020 [P] [US0.5] Implement `ValueObject` abstract base class in `backend/src/Muntada.SharedKernel/Domain/ValueObject.cs` — XML docs on all public members
- [ ] T021 [US0.5] Implement `AggregateRoot<TId>` in `backend/src/Muntada.SharedKernel/Domain/AggregateRoot.cs` — inherits Entity, adds Version, CreatedAt, UpdatedAt, domain event tracking — XML docs
- [ ] T022 [US0.5] Implement `IDomainEvent` interface in `backend/src/Muntada.SharedKernel/Domain/IDomainEvent.cs` — EventId (Guid), OccurredAt (DateTimeOffset) — XML docs
- [ ] T023 [US0.5] Implement `OpaqueIdGenerator` static class in `backend/src/Muntada.SharedKernel/Domain/OpaqueIdGenerator.cs` — Generate(prefix), TryParse, URL-safe encoding via Sqids — XML docs
- [ ] T024 [US0.5] Implement `AuditedEntity<TId>` in `backend/src/Muntada.SharedKernel/Domain/AuditedEntity.cs` — inherits AggregateRoot, adds CreatedBy, UpdatedBy, IsDeleted, DeletedAt, DeletedBy — XML docs
- [ ] T025 [US0.5] Implement `AuditLog` entity in `backend/src/Muntada.SharedKernel/Domain/AuditLog.cs` — XML docs
- [ ] T026 [P] [US0.5] Implement `DomainException` base class in `backend/src/Muntada.SharedKernel/Domain/Exceptions/DomainException.cs` — XML docs
- [ ] T027 [P] [US0.5] Implement `ValidationException` in `backend/src/Muntada.SharedKernel/Domain/Exceptions/ValidationException.cs` — includes ValidationError list — XML docs
- [ ] T028 [P] [US0.5] Implement `EntityNotFoundException` in `backend/src/Muntada.SharedKernel/Domain/Exceptions/EntityNotFoundException.cs` — XML docs
- [ ] T029 [P] [US0.5] Implement `UnauthorizedException` in `backend/src/Muntada.SharedKernel/Domain/Exceptions/UnauthorizedException.cs` — XML docs
- [ ] T030 [US0.5] Verify all unit tests pass (`dotnet test backend/tests/Muntada.SharedKernel.Tests/`)

**Checkpoint**: All domain base classes compile, fully tested, XML documented.

---

## Phase 4: Shared Kernel — Application & Infrastructure Layer

**Purpose**: Integration event publishing, error handling middleware, validation, telemetry
**User Story**: US-0.5

### Unit Tests (Write FIRST)

- [x] T031 [P] [US0.5] Write unit tests for `ErrorHandlingMiddleware` in `backend/tests/Muntada.SharedKernel.Tests/Infrastructure/ErrorHandlingMiddlewareTests.cs`
- [x] T032 [P] [US0.5] Write unit tests for `IntegrationEventPublisher` in `backend/tests/Muntada.SharedKernel.Tests/Infrastructure/IntegrationEventPublisherTests.cs`
- [x] T033 [P] [US0.5] Write unit tests for `ValidationBehavior` in `backend/tests/Muntada.SharedKernel.Tests/Application/ValidationBehaviorTests.cs`

### Implementation

- [x] T034 [US0.5] Implement `IIntegrationEvent` interface in `backend/src/Muntada.SharedKernel/Domain/IIntegrationEvent.cs` — extends IDomainEvent with AggregateId, AggregateType, Version — XML docs
- [x] T035 [US0.5] Implement `IIntegrationEventPublisher` interface in `backend/src/Muntada.SharedKernel/Application/IIntegrationEventPublisher.cs` — XML docs
- [x] T036 [US0.5] Implement `IntegrationEventPublisher` (MassTransit/RabbitMQ) in `backend/src/Muntada.SharedKernel/Infrastructure/IntegrationEventPublisher.cs` — event routing, DLQ support — XML docs
- [x] T037 [US0.5] Implement `ErrorHandlingMiddleware` in `backend/src/Muntada.SharedKernel/Infrastructure/Middleware/ErrorHandlingMiddleware.cs` — RFC 9457 Problem Details, exception-to-HTTP mapping, correlation ID — XML docs
- [x] T038 [US0.5] Implement `ValidationBehavior<TRequest,TResponse>` MediatR pipeline in `backend/src/Muntada.SharedKernel/Application/Behaviors/ValidationBehavior.cs` — FluentValidation integration — XML docs
- [x] T039 [US0.5] Implement `TelemetryConfiguration` in `backend/src/Muntada.SharedKernel/Infrastructure/Telemetry/TelemetryConfiguration.cs` — ActivitySource setup, OTLP export (Aspire Dashboard in dev, Jaeger in prod) — XML docs
- [x] T040 [P] [US0.5] Implement `ActivitySourceExtensions` in `backend/src/Muntada.SharedKernel/Infrastructure/Telemetry/ActivitySourceExtensions.cs` — convenience methods for custom spans — XML docs
- [x] T041 [P] [US0.5] Create `BaseTestFixture` in `backend/tests/Muntada.SharedKernel.Tests/BaseTestFixture.cs` — common test setup, mocking helpers
- [x] T042 [P] [US0.5] Create `DomainAssertions` in `backend/tests/Muntada.SharedKernel.Tests/Assertions/DomainAssertions.cs` — custom FluentAssertions for domain objects
- [x] T043 [US0.5] Verify all unit tests pass (`dotnet test backend/tests/Muntada.SharedKernel.Tests/`)

**Checkpoint**: SharedKernel fully implemented and tested. All infrastructure patterns available for downstream modules.

---

## Phase 5: Backend API Host & Health Checks

**Purpose**: ASP.NET Core host application with health check endpoints
**User Story**: US-0.1, US-0.5

- [x] T044 Create `Muntada.Api.csproj` at `backend/src/Muntada.Api/` referencing SharedKernel and Aspire ServiceDefaults
- [x] T045 Create `backend/src/Muntada.Api/Program.cs` — register middleware (ErrorHandling), MassTransit, FluentValidation, Serilog, ServiceDefaults
- [x] T046 Implement health check endpoints (`/health`, `/health/ready`, `/health/live`) per contract in `backend/src/Muntada.Api/` using ASP.NET Core health checks + ServiceDefaults
- [x] T047 Create `backend/src/Muntada.Api/appsettings.json` — base config (no secrets)
- [x] T048 [P] Create `backend/src/Muntada.Api/appsettings.Development.json` — Aspire service discovery (no hardcoded connection strings)
- [x] T049 [P] Create `backend/src/Muntada.Api/appsettings.Production.json` — K8s ConfigMap/Secret references
- [x] T050 Write integration test for health check endpoints in `backend/tests/Muntada.Integration.Tests/HealthCheckTests.cs`
- [x] T051 Verify backend starts via Aspire AppHost and health checks return 200

**Checkpoint**: Backend API running via Aspire, health endpoints operational, traces visible in Aspire Dashboard.

---

## Phase 6: Frontend Scaffolding

**Purpose**: React/TypeScript SPA with Vite, Redux Toolkit + RTK Query, Playwright
**User Story**: US-0.1

- [ ] T052 Initialize React + TypeScript project with Vite at `frontend/` — `npm create vite@latest`
- [ ] T053 Install and configure Redux Toolkit + RTK Query in `frontend/src/app/store.ts`
- [ ] T054 [P] Create base API slice with RTK Query in `frontend/src/shared/api/baseApi.ts` — base URL from env, typed endpoints
- [ ] T055 [P] Configure ESLint in `frontend/eslint.config.js` with TypeScript rules
- [ ] T056 [P] Configure Playwright in `frontend/playwright.config.ts` with `webServer` auto-start
- [ ] T057 [P] Create `frontend/src/App.tsx` shell component with router setup
- [ ] T058 [P] Create `frontend/src/main.tsx` entry point with Redux Provider
- [ ] T059 Write Playwright smoke test (frontend loads, connects to backend) in `frontend/tests/e2e/smoke.spec.ts`
- [ ] T060 [P] Create `frontend/Dockerfile` — multi-stage build (Node build → Nginx Alpine serve), target < 500MB
- [ ] T061 Verify frontend starts via Aspire AppHost and loads at `http://localhost:3000`

**Checkpoint**: Frontend SPA running via Aspire, RTK Query configured, Playwright tests passing.

---

## Phase 7: Docker Compose Fallback & Makefile

**Purpose**: Fallback orchestration for environments without .NET SDK
**User Story**: US-0.1

- [ ] T062 Create `docker-compose.yml` with all services: SQL Server, Redis, RabbitMQ, MinIO, LiveKit, backend, frontend — health checks, named volumes, proper startup ordering
- [ ] T063 [P] Create `docker-compose.test.yml` for CI integration test execution
- [ ] T064 [P] Create `backend/Dockerfile` — multi-stage build (SDK build → runtime), target < 800MB
- [ ] T065 Create `Makefile` with targets: `setup` (Aspire primary), `aspire`, `up` (Docker Compose fallback), `down`, `clean`, `test`, `docker-build`, `logs`, `help`
- [ ] T066 Verify `make setup` completes in < 10 minutes and all services are healthy

**Checkpoint**: Docker Compose fallback works. Makefile provides all convenience targets.

---

## Phase 8: Kubernetes & Helm Charts

**Purpose**: K8s namespace model, infrastructure Helm charts
**User Stories**: US-0.3, US-0.4

### Kubernetes Namespaces (US-0.3)

- [ ] T067 [US0.3] Create Helm chart `infra/helm/namespaces/` — Chart.yaml, templates for namespace, RBAC, network policies, quotas
- [ ] T068 [P] [US0.3] Create `infra/helm/namespaces/values-dev.yaml` — dev namespace config
- [ ] T069 [P] [US0.3] Create `infra/helm/namespaces/values-staging.yaml` — staging namespace config
- [ ] T070 [P] [US0.3] Create `infra/helm/namespaces/values-prod.yaml` — prod namespace config with stricter quotas

### Infrastructure Services (US-0.4)

- [ ] T071 [P] [US0.4] Create Helm chart `infra/helm/sql-server/` — StatefulSet, PVC, ConfigMap, Secret, init scripts, values per environment
- [ ] T072 [P] [US0.4] Create Helm chart `infra/helm/redis/` — StatefulSet with Sentinel, ConfigMap, values per environment
- [ ] T073 [P] [US0.4] Create Helm chart `infra/helm/rabbitmq/` — StatefulSet, queue/exchange init job, ConfigMap, values per environment
- [ ] T074 [P] [US0.4] Create Helm chart `infra/helm/minio/` — StatefulSet, bucket provisioning job, values per environment
- [ ] T075 [P] [US0.4] Create Helm chart `infra/helm/livekit/` — StatefulSet (self-hosted OSS), 2+ replicas in prod, API key/secret via Secret, values per environment
- [ ] T076 [US0.4] Create Helm chart `infra/helm/muntada/` — umbrella chart for backend + frontend Deployments, Services, Ingress, TLS via cert-manager

**Checkpoint**: All Helm charts render cleanly (`helm template`). K8s namespaces created with RBAC and quotas.

---

## Phase 9: CI/CD Pipeline

**Purpose**: GitHub Actions for lint, test, build, Docker Hub push, K8s deploy
**User Story**: US-0.2

- [ ] T077 [US0.2] Create `.github/workflows/ci.yml` — PR trigger, matrix jobs: frontend lint (ESLint), frontend tests (Jest), backend lint (StyleCop), backend tests (xUnit), backend integration tests, Docker image build
- [ ] T078 [US0.2] Add Docker Hub push step to ci.yml — push on main merge, tag with git SHA + `latest`, uses `DOCKERHUB_USERNAME` and `DOCKERHUB_TOKEN` secrets
- [ ] T079 [US0.2] Create `.github/workflows/deploy.yml` — manual trigger or main merge, Helm deploy to staging/prod K8s namespaces, rollout status tracking
- [ ] T080 [P] [US0.2] Add test coverage report generation and PR comment posting to ci.yml
- [ ] T081 [US0.2] Document rollback procedure in `docs/runbooks/rollback.md`

**Checkpoint**: CI pipeline validates PRs. CD pipeline pushes images to Docker Hub and deploys to K8s.

---

## Phase 10: Environment Configuration & Secrets

**Purpose**: Environment-specific config management
**User Story**: US-0.6

- [ ] T082 [US0.6] Configure environment variable binding in backend (nested paths: `Database__ConnectionString`)
- [ ] T083 [P] [US0.6] Create Helm templates for ConfigMap and Secret mounting across all service charts
- [ ] T084 [P] [US0.6] Implement configuration validation on startup — fail fast if required settings missing
- [ ] T085 [US0.6] Document secret rotation procedure in `docs/runbooks/secret-rotation.md`

**Checkpoint**: Config loads per environment. Secrets not in version control. Fail-fast on missing config.

---

## Phase 11: Documentation & Polish

**Purpose**: Developer onboarding, architecture docs, runbooks
**User Stories**: US-0.1, cross-cutting

- [ ] T086 Create `README.md` — project overview, prerequisites (.NET SDK 8+, Aspire 13.2, Docker, Node), quickstart (`dotnet run --project aspire/Muntada.AppHost`), service endpoints, architecture overview
- [ ] T087 [P] Create `CONTRIBUTING.md` — branching strategy, PR process, commit conventions, code review, Aspire module registration guide
- [ ] T088 [P] Create architecture diagrams in `docs/architecture/` — system context (C4), container diagram, module diagram, deployment diagram
- [ ] T089 [P] Create `docs/runbooks/local-dev-troubleshooting.md` — common issues with Aspire, Docker, connectivity
- [ ] T090 [P] Create `docs/runbooks/database-migration.md` — `dotnet ef migrations add` procedure (NEVER AI-generated)
- [ ] T091 Run full test suite: `dotnet test` (backend) + `npm test` (frontend) + Playwright E2E — all must pass
- [ ] T092 Final validation: `dotnet run --project aspire/Muntada.AppHost` starts everything, Aspire Dashboard shows all services healthy

**Checkpoint**: Foundation complete. All tests pass. Documentation ready for onboarding.

---

## Dependencies & Execution Order

### Phase Dependencies

```
Phase 1 (Setup) ──────────> Phase 2 (Aspire) ──────> Phase 5 (API Host)
                                    │                        │
                                    ├──────> Phase 3 (Domain)│──> Phase 4 (Infra Layer)
                                    │                               │
                                    ├──────> Phase 6 (Frontend)     │
                                    │                               │
                                    └──────> Phase 7 (Docker/Make)  │
                                                                    │
Phase 8 (K8s/Helm) ────────────────────────────────────────────────┘
Phase 9 (CI/CD) ── depends on: Phase 5, Phase 6, Phase 7
Phase 10 (Config) ── depends on: Phase 5, Phase 8
Phase 11 (Docs) ── depends on: ALL phases
```

### Parallel Opportunities

- **Phase 1**: T002, T003, T004, T005 can run in parallel
- **Phase 2**: T006, T008 can start in parallel; T007 depends on T006
- **Phase 3**: All test tasks (T013-T017) in parallel; implementation tasks (T019, T020, T026-T029) in parallel where marked [P]
- **Phase 4**: Test tasks (T031-T033) in parallel; T040-T042 in parallel
- **Phase 6**: T054-T060 can mostly run in parallel
- **Phase 8**: All Helm charts (T068-T075) in parallel
- **Phase 9**: T080 in parallel with T077

---

## Implementation Strategy

### MVP First (Phases 1-5 Only)

1. Complete Phase 1: Setup (directory structure)
2. Complete Phase 2: Aspire AppHost (primary dev environment)
3. Complete Phase 3: SharedKernel Domain (base types + tests)
4. Complete Phase 4: SharedKernel Infrastructure (middleware, events, telemetry)
5. Complete Phase 5: Backend API Host (health checks)
6. **STOP and VALIDATE**: Backend runs via Aspire, all tests pass, health checks work

### Incremental Delivery

7. Phase 6: Frontend scaffolding → frontend connects to backend via Aspire
8. Phase 7: Docker Compose fallback + Makefile
9. Phase 8: Kubernetes Helm charts
10. Phase 9: CI/CD pipeline with Docker Hub
11. Phase 10: Environment configuration
12. Phase 11: Documentation and polish

---

## Git & PR Workflow (per Constitution)

- **GitHub Issues**: Create a GitHub issue for each task before implementation begins. Close it upon completion.
- **Commit after each task** — one Git commit per completed task, not batched.
- **All unit tests MUST pass** before each commit.
- **PR per Phase**: Create a Pull Request at the end of each phase with a detailed summary of all changes.
- **Code Review**: Run code review before submitting any PR. Fix all findings first.
- **Phase Summary**: Include a detailed summary of all implemented tasks when the phase is completed.
- **Database Migrations**: NEVER generate migrations via AI — use `dotnet ef migrations add` only.
- **Aspire AppHost**: Every new module MUST register itself in the Aspire AppHost project. Local dev runs via `dotnet run --project AppHost`.
