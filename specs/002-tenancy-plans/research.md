# Research: Tenancy & Plans

**Branch**: `002-tenancy-plans` | **Date**: 2026-04-03

## Overview

All technical context is resolved from the existing codebase patterns (Identity module, SharedKernel) and the detailed source specification. No NEEDS CLARIFICATION items existed. This document records design decisions and rationale.

---

## Decision 1: Multi-Tenancy Isolation Strategy

**Decision**: Row-level filtering via `TenantId` foreign key on all tenant-scoped tables, enforced by a `TenantContextMiddleware` that resolves the current tenant from JWT claims or `X-Tenant-ID` header.

**Rationale**: The codebase uses a modular monolith with per-module SQL Server schemas. Full database-per-tenant would be overkill for the current scale target (10K tenants). Row-level filtering is simpler, allows shared schema migrations, and is the standard pattern for multi-tenant SaaS at this scale. The middleware ensures every query is scoped — unscoped queries return empty results as a fail-safe.

**Alternatives considered**:
- Database-per-tenant: Too complex for current scale, increases migration burden
- Schema-per-tenant: SQL Server schema limit concerns, complex EF Core configuration
- Row-Level Security (RLS): May add later as defense-in-depth, but application-level filtering is primary

---

## Decision 2: Plan Definition Architecture

**Decision**: Plan definitions (Starter, Professional, Enterprise, etc.) are stored in the `[tenancy].Plans` table and seeded during deployment. A `PlanDefinition` reference type provides the template; tenants get a `TenantPlan` assignment that references the definition with start/end dates.

**Rationale**: Storing plan definitions in the database (not code/config) allows runtime plan management without redeployment. Separating definition from assignment supports plan versioning — existing tenants keep their plan terms even if the definition changes for new subscribers.

**Alternatives considered**:
- Configuration file: Requires redeployment to change plans
- Code-based enum: Too rigid, can't add enterprise custom plans

---

## Decision 3: Tenant Context Resolution

**Decision**: Use `X-Tenant-ID` header for API requests, with JWT claim as fallback. The `TenantContextMiddleware` extracts the tenant ID and makes it available via `ITenantContext` (scoped DI service).

**Rationale**: The Identity module already establishes JWT-based auth. Adding tenant context follows the same pattern. The header approach allows users in multiple tenants to switch context without re-authenticating. The middleware pattern is consistent with how Identity sets the current user.

**Alternatives considered**:
- Subdomain-based: Complex DNS configuration, harder to test locally
- JWT-only: Requires re-authentication when switching tenants

---

## Decision 4: Feature Toggle Implementation

**Decision**: Custom implementation using a `FeatureToggle` entity in the `[tenancy]` schema with Redis caching (5-minute TTL). Evaluation supports per-tenant, per-role, per-region, and canary percentage scopes.

**Rationale**: The project's constitution requires self-hosted infrastructure (GCC region, Kubernetes). External services like LaunchDarkly are unsuitable. .NET's built-in `Microsoft.FeatureManagement` is too simple for multi-tenant scoped toggles. A custom implementation fits the modular monolith pattern and integrates with existing Redis infrastructure.

**Alternatives considered**:
- LaunchDarkly/Unleash: External dependency, data residency concerns for GCC
- Microsoft.FeatureManagement: No built-in multi-tenant scope support
- Flagsmith (self-hosted): Additional infrastructure to maintain

---

## Decision 5: Usage Tracking Architecture

**Decision**: Daily background job (`UsageAggregationJob`) aggregates usage metrics into a `TenantUsageSnapshot` table. Current-period metrics are cached in Redis for real-time dashboard access. Hard limit checks query Redis first, falling back to database.

**Rationale**: Real-time per-request counting would add latency to every API call. Daily aggregation + Redis caching provides near-real-time accuracy (within 5 minutes per SC-005) without performance impact. The background job pattern is consistent with the constitution's observability requirements.

**Alternatives considered**:
- Real-time counters per request: Too much latency overhead
- Event-sourced usage: Overengineered for current needs
- Redis-only counters: Risk of data loss on Redis restart

---

## Decision 6: Data Retention Implementation

**Decision**: `DataLifecycleCleanupJob` runs daily at off-peak hours. Uses soft-delete (set `ScheduledForDeletionAt` timestamp) followed by hard-delete after 7-day grace period. All deletions are recorded in the audit log.

**Rationale**: The soft-delete-then-hard-delete pattern with grace period is explicitly required by the spec (FR-016). Running at off-peak reduces database contention. Audit trail preservation satisfies PDPL compliance (7-year minimum for audit logs).

**Alternatives considered**:
- Immediate hard-delete: No recovery option, spec requires grace period
- Archive to cold storage: Additional complexity for v1, can add later

---

## Decision 7: Logo Upload & Image Processing

**Decision**: Upload logos to MinIO via `IBrandingService`. Use ImageSharp for server-side resizing to 32px, 64px, 128px, 256px variants. Store under `tenants/{tenantId}/logo-{size}.png` path in MinIO. Serve with cache headers.

**Rationale**: MinIO is already in the Aspire AppHost stack. ImageSharp is the standard .NET cross-platform image library (no native dependency issues). Pre-generating size variants avoids on-the-fly resizing.

**Alternatives considered**:
- Client-side resizing: Inconsistent quality, can't guarantee sizes
- On-the-fly resizing proxy: Additional infrastructure complexity

---

## Decision 8: Invite Token Mechanism

**Decision**: Generate a cryptographically random token (32 bytes, base64url-encoded) stored in `TenantInviteToken`. Token is embedded in invite email link. Valid for 7 days. Acceptance transitions membership from Pending to Active.

**Rationale**: Follows the same pattern as Identity module's `EmailVerificationToken`. Time-limited tokens prevent stale invites. The 7-day expiry is explicitly specified in FR-005.

**Alternatives considered**:
- JWT-based invite links: Unnecessary complexity for a simple token lookup
- Magic link (passwordless): Already used for guest access in Identity, but invite acceptance requires an authenticated session

---

## Decision 9: Opaque ID Prefixes

**Decision**: Use the SharedKernel `OpaqueIdGenerator` (Sqids) with these prefixes:
- Tenant: `tnt_`
- TenantMembership: `mbr_`
- Plan: `pln_`
- RetentionPolicy: `rtn_`
- FeatureToggle: `ftg_`

**Rationale**: Consistent with Identity module's approach (usr_, pat_, ses_). Opaque IDs prevent enumeration attacks and are required by Constitution Principle VII (Explicit Over Implicit).

**Alternatives considered**:
- Sequential IDs: Security risk (enumeration), violates constitution
- UUIDs without prefix: Harder to debug — prefix identifies entity type at a glance

---

## Decision 10: Integration Events

**Decision**: Publish the following integration events via MassTransit/RabbitMQ:
- `TenantCreatedEvent` — consumed by future modules needing tenant awareness
- `TenantMembershipChangedEvent` — membership created, updated, removed
- `PlanChangedEvent` — plan assigned, upgraded, downgraded
- `UsageLimitExceededEvent` — threshold alerts (95%, 100%)
- `RetentionPolicyChangedEvent` — retention configuration updated
- `FeatureToggleChangedEvent` — toggle state changed

**Rationale**: These are the events listed in FR-020 and the source spec's FR-2.19. Using MassTransit with RabbitMQ is the established pattern from SharedKernel. Events enable the Billing module (future) and other modules to react to tenancy changes without direct coupling.

**Alternatives considered**:
- Direct service calls: Violates Constitution Principle I (module isolation)
- Shared database triggers: Fragile, not testable

---

## Decision 11: Trial Expiration Strategy (Clarification 2026-04-04)

**Decision**: Expired trial tenants are automatically downgraded to the Free tier. All data is preserved, but resource limits are reduced to Free plan levels. Users see upgrade prompts but are never locked out.

**Rationale**: Auto-downgrade to Free is the safest approach — no data loss, no abrupt lockout. Users retain access to their workspace and can upgrade at any time. This reduces support burden and avoids hostile UX for users evaluating the platform.

**Alternatives considered**:
- Immediate lockout (Suspended state): Too aggressive for trial users, risks losing them permanently
- 7-day grace period then downgrade: Adds complexity without clear benefit — the Free tier already provides graceful degradation
- Indefinite trial extension: No enforcement, undermines the plan-based business model

**Implementation**: A `TrialExpirationJob` background job runs daily, queries tenants where `BillingStatus = Trial AND TrialEndsAt < now`, and assigns the Free plan definition. Publishes `PlanChangedEvent` for downstream billing reconciliation.

---

## Decision 12: Maximum Tenants Per User (Clarification 2026-04-04)

**Decision**: Users can belong to a maximum of 10 tenants. This limit is configurable and can be raised for enterprise users.

**Rationale**: A default limit of 10 prevents abuse (automated account creation farming) while being generous enough for consultants, freelancers, and power users who legitimately work across multiple organizations. The configurable aspect allows enterprise support without code changes.

**Alternatives considered**:
- No limit: Risk of abuse and unbounded resource consumption per user
- Limit of 5: Too restrictive for multi-org consultants
- Plan-tier-based limit: Overcomplicates the model — the limit is per-user, not per-tenant

**Implementation**: Validate at `AcceptTenantInviteCommand` handler time — count active memberships for the user. If >= limit, reject with "You have reached the maximum number of organizations (10). Contact support to increase your limit." Store the configurable limit as a user-level setting with a system default of 10.

---

## Decision 13: Suspended Tenant Access Model (Clarification 2026-04-04)

**Decision**: Suspended tenants allow read-only access. Members can view existing data (rooms, recordings, files, chat history) but cannot create new resources (rooms, recordings, file uploads, invites).

**Rationale**: Read-only access preserves data accessibility while incentivizing payment resolution. Full lockout risks data hostage perception and support escalation. Members who need their data (e.g., downloading recordings) can still access it without creating new resources that consume quota.

**Alternatives considered**:
- Full lockout (403 on all requests): Too aggressive, prevents data export
- Degraded mode (Free tier limits): Confusing — suspension is a billing state, not a plan state

**Implementation**: `TenantContextMiddleware` checks `Tenant.Status`. If `Suspended`, allow requests with HTTP methods GET and HEAD. Reject POST, PUT, PATCH, DELETE with 403 and body: `{"error": "Tenant suspended", "message": "This organization is suspended. You can view existing data but cannot create new resources. Contact your organization owner to resolve.", "code": "TENANT_SUSPENDED"}`.
