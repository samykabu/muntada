# Research: Foundation & Infrastructure

**Feature**: 000-foundation
**Date**: 2026-04-03
**Status**: Complete

---

## R1: Modular Monolith Project Structure (.NET 8+)

**Decision**: Each module is a separate class library NuGet project under `backend/src/Modules/{ModuleName}/` with Clean Architecture layers (Domain, Application, Infrastructure, Api). SharedKernel is a standalone project referenced by all modules.

**Rationale**: Clean Architecture enforces dependency inversion — Domain has zero external dependencies, Application depends only on Domain, Infrastructure implements interfaces from Application. This aligns with Constitution VIII (Clean Code Architecture) and I (Modular Monolith Discipline). Separate projects enforce compile-time boundaries between modules.

**Alternatives Considered**:
- **Single project with folders**: Rejected — no compile-time enforcement of module boundaries, too easy to create cross-module dependencies.
- **Microservices from day one**: Rejected — premature complexity for a platform still defining its domain boundaries. Constitution I explicitly preserves future extraction options.

---

## R2: Opaque ID Generation Strategy

**Decision**: Use `Sqids` (formerly Hashids) library with a custom prefix scheme. Format: `{prefix}_{sqid}` (e.g., `usr_a7k2jZ9xQpR4b1m`). Underlying storage uses `Guid` for database keys; opaque IDs are generated for external-facing APIs only.

**Rationale**: Sqids produces short, URL-safe, non-sequential IDs from numeric/GUID inputs. The prefix provides type safety and debugging convenience. Storing GUIDs internally avoids index fragmentation. Constitution VII (Explicit Over Implicit) requires opaque identifiers over sequential IDs.

**Alternatives Considered**:
- **ULIDs**: Good sortability but no prefix support natively. Would need custom wrapper.
- **NanoID**: Random strings without reversibility. Cannot decode back to underlying entity type.
- **Sequential GUIDs**: Violates Constitution VII (no opaque identifiers).

---

## R3: Integration Event Bus — MassTransit vs Raw RabbitMQ

**Decision**: Use MassTransit as the abstraction layer over RabbitMQ. MassTransit provides saga support, retry policies, dead-letter handling, and message serialization out of the box.

**Rationale**: MassTransit handles the undifferentiated heavy lifting of message routing, retry, and dead-letter queuing. It supports the outbox pattern for transactional event publishing (critical for "no event loss on transient failures" acceptance criteria). Direct RabbitMQ client requires building all this manually.

**Alternatives Considered**:
- **Raw RabbitMQ client (RabbitMQ.Client)**: Full control but requires manual retry, DLQ, serialization, and topology management. Rejected as unnecessarily complex for initial implementation.
- **CAP**: Lightweight but less ecosystem support and fewer features than MassTransit.
- **NServiceBus**: Commercial license, cost not justified at this stage.

---

## R4: OpenTelemetry Configuration — SDK Choices

**Decision**: Use `OpenTelemetry.Extensions.Hosting` for automatic instrumentation of ASP.NET Core, HTTP clients, SQL client, and Entity Framework. Export to Jaeger via OTLP exporter. Use `System.Diagnostics.ActivitySource` for custom spans.

**Rationale**: The .NET OpenTelemetry SDK has first-party support in ASP.NET Core 8. Auto-instrumentation captures HTTP request/response, EF Core queries, and HttpClient calls without manual code. Custom `ActivitySource` instances allow module-specific tracing. Constitution VI (Observability from Day One) requires traces for all critical paths.

**Alternatives Considered**:
- **Application Insights SDK**: Azure-specific, not portable to self-hosted K8s.
- **Serilog-only**: Structured logging without distributed trace context. Insufficient for cross-service correlation.

---

## R5: Error Handling Strategy — Problem Details RFC 9457

**Decision**: Implement `ErrorHandlingMiddleware` that catches domain exceptions and returns RFC 9457 (Problem Details) JSON responses. Map `DomainException` → 400, `EntityNotFoundException` → 404, `UnauthorizedException` → 403, unhandled → 500.

**Rationale**: RFC 9457 is the HTTP standard for machine-readable error responses. ASP.NET Core 8 has built-in `IProblemDetailsService` support. Standardized error format enables typed error handling in frontend API client. Constitution VII (Explicit Over Implicit) — errors are explicit, structured, and traceable via correlation ID.

**Alternatives Considered**:
- **Custom JSON format**: Non-standard, requires frontend to learn proprietary format.
- **Exception filters only**: Limited to MVC pipeline; misses middleware-level errors.

---

## R6: Frontend State Management

**Decision**: Use Redux Toolkit (RTK) with RTK Query for server-state management. RTK Query handles API caching, deduplication, and automatic re-fetching. Local UI state uses React hooks (`useState`, `useReducer`).

**Rationale**: RTK Query eliminates boilerplate for API calls and aligns with the API-first principle (Constitution III). Generated TypeScript types from OpenAPI schema ensure type safety. Redux DevTools provide excellent debugging experience.

**Alternatives Considered**:
- **React Query (TanStack Query)**: Excellent for server state but no global state story. Would need Zustand or Context alongside it.
- **Zustand**: Simpler but lacks the structured patterns of Redux for a large app with 13 modules.
- **MobX**: Observable pattern doesn't align with the explicit state philosophy.

---

## R7: Docker Image Optimization

**Decision**: Multi-stage Docker builds. Backend: SDK image for build, ASP.NET runtime image for final. Frontend: Node image for build, Nginx Alpine for serving static files.

**Rationale**: Multi-stage builds minimize final image size (spec targets: frontend < 500MB, backend < 800MB). Alpine-based Nginx image for frontend will be ~50MB. ASP.NET runtime image is ~200MB. Build artifacts don't ship in production images.

**Alternatives Considered**:
- **Single-stage builds**: Includes SDK/Node in production image, bloating size 2-3x.
- **Distroless images**: Smaller but harder to debug in production. Can be adopted later.

---

## R8: Helm Chart Strategy

**Decision**: One Helm chart per infrastructure component (SQL Server, Redis, RabbitMQ, MinIO, LiveKit) and one umbrella chart for the Muntada application. Values files per environment (`values-dev.yaml`, `values-staging.yaml`, `values-prod.yaml`).

**Rationale**: Separate charts allow independent lifecycle management of infrastructure vs application. Umbrella chart for the app simplifies deployment. Per-environment values files keep configuration explicit (Constitution VII).

**Alternatives Considered**:
- **Single monolithic chart**: Too complex, coupling infrastructure and application lifecycle.
- **Kustomize**: Less feature-rich than Helm for templating. Helm's `helm rollback` is needed per spec.
- **ArgoCD ApplicationSets**: Adds another tool. Can layer on top of Helm later.

---

## R9: Database Migration Strategy

**Decision**: Entity Framework Core Code-First migrations, applied via `dotnet ef migrations add <Name>` CLI command ONLY. Never AI-generated. Applied at startup in development, via CI/CD job in staging/production.

**Rationale**: Constitution X (AI-Safe Database Migrations) explicitly forbids AI-generated migration files. EF Core's snapshot-based tracking ensures consistency. Startup application handles development; production uses a dedicated migration job to avoid race conditions with multiple pods.

**Alternatives Considered**:
- **DbUp**: SQL script-based, no model snapshot tracking. Harder to evolve schema incrementally.
- **Flyway**: Java-based, external dependency for a .NET project.

---

## R10: CI/CD Pipeline Design

**Decision**: Two GitHub Actions workflows: `ci.yml` (runs on PRs — lint, test, build) and `deploy.yml` (runs on main merge — Docker push, Helm deploy). Matrix strategy for parallel frontend/backend jobs.

**Rationale**: Separating CI and CD allows PRs to be validated quickly without deployment concerns. Matrix strategy runs frontend (ESLint + Jest) and backend (StyleCop + xUnit) checks in parallel. Constitution workflow requires all tests pass before merge.

**Alternatives Considered**:
- **Single workflow**: Longer feedback loop, harder to maintain.
- **GitLab CI**: Spec requires GitHub Actions (GitHub is the chosen platform).
- **Jenkins**: Self-hosted complexity not justified for a GitHub-native project.

---

## R11: .NET Aspire as Local Development Orchestrator

**Decision**: Use .NET Aspire 13.2 as the mandatory local development orchestrator. The Aspire AppHost project (`aspire/Muntada.AppHost`) declares all service dependencies and provisions them via a single `dotnet run` command. Docker Compose is retained as a fallback only.

**Rationale**: Constitution XII (Aspire-First Local Development) mandates Aspire for all projects. Aspire provides: (1) single-command local dev startup, (2) built-in service discovery eliminating manual connection string wiring, (3) integrated dashboard with health monitoring and distributed tracing (replacing separate Jaeger setup), (4) automatic container management for SQL Server, Redis, RabbitMQ, MinIO, and LiveKit. The `Muntada.ServiceDefaults` project centralizes OpenTelemetry, health checks, and resilience configuration — consumed by every service via a shared NuGet reference.

**Alternatives Considered**:
- **Docker Compose only**: No service discovery, no integrated dashboard, requires manual wiring of connection strings. Verbose for 6+ service dependencies.
- **Tye (Project Tye)**: Archived/deprecated by Microsoft in favor of Aspire.
- **Custom orchestration scripts**: Fragile, hard to maintain, no standardized dashboard.

---

## R12: Playwright Testing Strategy

**Decision**: Playwright for all integration and E2E tests. Configure in `frontend/playwright.config.ts` with `webServer` option to auto-start the dev server. Separate test commands: `npm run test:e2e` (Playwright), `npm run test:unit` (Jest).

**Rationale**: Constitution XI mandates Playwright for integration/E2E tests. Playwright's auto-waiting, cross-browser support, and API testing capabilities make it ideal. Tests that would require manual human validation should be replaced by Playwright tests where feasible.

**Alternatives Considered**:
- **Cypress**: Good DX but slower execution and no multi-tab/multi-browser support.
- **Selenium**: Verbose API, slower execution, less modern tooling.

---

## R13: Container Registry — Docker Hub (Clarification Decision)

**Decision**: Use Docker Hub (docker.io) as the container registry for CI/CD image push and Kubernetes image pull. Images tagged with git SHA and `latest` for the main branch.

**Rationale**: Decided during spec clarification session (2026-04-03). Docker Hub is the most widely supported registry, requires minimal configuration in GitHub Actions, and is accessible from self-hosted Kubernetes clusters without VPN or private network setup. Free tier limits are sufficient for early-stage development.

**Alternatives Considered**:
- **GitHub Container Registry (ghcr.io)**: Native GitHub Actions integration, but adds a dependency on GitHub-specific features. Could be adopted later if rate limits become an issue.
- **Self-hosted Harbor**: Full data residency control, but adds significant operational overhead for the foundation phase.
- **Azure Container Registry**: Adds Azure dependency to a self-hosted Kubernetes platform.

---

## R14: LiveKit Deployment — Self-Hosted OSS (Clarification Decision)

**Decision**: Self-hosted LiveKit OSS deployed in Kubernetes via Helm chart. Not using LiveKit Cloud (SaaS).

**Rationale**: Decided during spec clarification session (2026-04-03). The constitution mandates "LiveKit OSS (self-hosted)" under Technology Constraints. Self-hosting ensures GCC data residency compliance — media streams and recording data never leave the region. The Helm chart (T010) manages scaling (2+ replicas in prod), health checks, and API key/secret provisioning via Kubernetes Secrets.

**Alternatives Considered**:
- **LiveKit Cloud (SaaS)**: Less operational burden but media data may leave GCC region, violating PDPL alignment requirements.
- **Hybrid (self-hosted prod, SaaS dev)**: Inconsistent environments increase risk of dev/prod divergence.

---

## R15: Frontend State Management — Redux Toolkit + RTK Query (Clarification Decision)

**Decision**: Use Redux Toolkit (RTK) with RTK Query for both global state and server-state management. Confirmed explicitly (FR-0.8 "or similar" ambiguity removed).

**Rationale**: Decided during spec clarification session (2026-04-03), confirming research R6. RTK Query eliminates API call boilerplate, handles caching/deduplication/refetching, and aligns with API-first principle (Constitution III). TypeScript-first with excellent DevTools support. Single state management paradigm for 13 modules reduces cognitive overhead.

**Alternatives Considered**: See R6 for full analysis (React Query, Zustand, MobX).
