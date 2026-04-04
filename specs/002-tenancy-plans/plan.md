# Implementation Plan: Tenancy & Plans

**Branch**: `002-tenancy-plans` | **Date**: 2026-04-03 (updated 2026-04-04) | **Spec**: [spec.md](spec.md)
**Input**: Feature specification from `/specs/002-tenancy-plans/spec.md`

## Summary

Implement a multi-tenancy and subscription plan management module for Muntada. This module provides organizational workspaces (tenants) with membership management, tiered subscription plans with resource limit enforcement, configurable data retention policies, and feature toggles for gradual rollout. It follows the modular monolith pattern established by Identity (Epic 1), with its own `[tenancy]` SQL Server schema, CQRS command/query handlers via MediatR, and integration events via MassTransit/RabbitMQ.

## Technical Context

**Language/Version**: C# / .NET 10
**Primary Dependencies**: ASP.NET Core 10, Entity Framework Core 10, MediatR 14.1.0, FluentValidation 12.1.1, MassTransit 9.1.0 (RabbitMQ), Serilog, OpenTelemetry SDK
**Storage**: SQL Server (`[tenancy]` schema), Redis (usage cache, feature toggle cache), MinIO (logo uploads)
**Testing**: xUnit (unit tests), Playwright (integration/E2E), React Testing Library (frontend)
**Target Platform**: Linux containers (Kubernetes), local dev via .NET Aspire 13.2
**Project Type**: Modular monolith module (backend) + React SPA feature (frontend)
**Performance Goals**: All plan/usage endpoints < 200ms p95, support 10,000+ concurrent tenants
**Constraints**: Single-tenant request context per API call, PDPL compliance (7-year audit log retention), GCC region only
**Scale/Scope**: 6 aggregates/entities, ~15 commands, ~5 queries, ~3 background jobs, 5 API controllers, 2 React pages

## Clarification Impact (2026-04-04)

Three spec clarifications were added during `/speckit.clarify`:

1. **Trial expiration → auto-downgrade to Free tier**: Requires a `TrialExpirationJob` background job that runs daily, checks `TrialEndsAt`, and transitions expired trial tenants to Free plan. No lockout — data preserved, limits reduced.
2. **Max 10 tenants per user**: Adds a configurable constraint on `TenantMembership` creation. Validated at invite-acceptance time. Default limit: 10, overridable per user for enterprise.
3. **Suspended tenant = read-only access**: `TenantContextMiddleware` must check tenant status. If Suspended, allow GET requests but reject POST/PUT/PATCH/DELETE with "Tenant suspended — read-only access" error.

## Constitution Check

*GATE: Must pass before Phase 0 research. Re-check after Phase 1 design.*

| Principle | Status | Notes |
|-----------|--------|-------|
| I. Modular Monolith Discipline | PASS | Tenancy module owns `[tenancy]` schema, communicates via integration events (TenantCreated, PlanUpgraded, etc.) |
| II. Server-Authoritative State | PASS | All plan limits, membership roles, and feature toggles are server-managed. Suspension read-only enforced server-side. |
| III. API-First for Multi-Client Readiness | PASS | All capabilities exposed via versioned REST APIs before UI |
| IV. Test-First for Critical Paths | PASS | TDD for plan limit enforcement, membership role validation, retention policy rules, trial expiration logic, suspension enforcement |
| V. Invite-Only Security Model | PASS | Membership is invite-only with token-based acceptance |
| VI. Observability from Day One | PASS | OpenTelemetry traces for tenant creation, plan changes; structured logging with correlation IDs |
| VII. Explicit Over Implicit | PASS | Explicit state machines for TenantStatus, BillingStatus, MembershipStatus; opaque IDs (tnt_, mbr_, pln_) |
| VIII. Clean Code & Documentation | PASS | Clean Architecture layers, XML docs on all public types |
| IX. Component Reusability | PASS | Shared React components for usage bars, member list, plan comparison |
| X. AI-Safe Database Migrations | PASS | Migrations via `dotnet ef migrations add` CLI only |
| XI. Comprehensive Testing Strategy | PASS | xUnit unit tests, Playwright E2E, all tests pass before commit |
| XII. Aspire-First Local Development | PASS | Tenancy module registers in Aspire AppHost |

**Gate Result**: ALL PASS — proceed.

## Project Structure

### Documentation (this feature)

```text
specs/002-tenancy-plans/
├── plan.md              # This file
├── spec.md              # Speckit specification
├── spec-source.md       # Original detailed spec (entity models, implementation notes)
├── tasks-source.md      # Original task breakdown (T201-T222)
├── research.md          # Phase 0 output
├── data-model.md        # Phase 1 output
├── quickstart.md        # Phase 1 output
├── contracts/           # Phase 1 output (API contracts)
│   ├── tenants.md
│   ├── members.md
│   ├── plans.md
│   ├── usage.md
│   ├── retention.md
│   └── feature-toggles.md
└── checklists/
    └── requirements.md  # Spec quality checklist
```

### Source Code (repository root)

```text
backend/
├── src/
│   ├── Modules/
│   │   └── Tenancy/
│   │       ├── Tenancy.csproj
│   │       ├── Domain/
│   │       │   ├── Tenant/
│   │       │   │   ├── Tenant.cs              # Aggregate root
│   │       │   │   ├── TenantBranding.cs      # Value object
│   │       │   │   ├── TenantStatus.cs        # Enum
│   │       │   │   ├── BillingStatus.cs       # Enum
│   │       │   │   └── TenantSlug.cs          # Value object
│   │       │   ├── Membership/
│   │       │   │   ├── TenantMembership.cs    # Entity
│   │       │   │   ├── TenantRole.cs          # Enum
│   │       │   │   ├── TenantMembershipStatus.cs # Enum
│   │       │   │   └── TenantInviteToken.cs   # Value object
│   │       │   ├── Plan/
│   │       │   │   ├── Plan.cs                # Entity
│   │       │   │   ├── PlanLimits.cs          # Value object
│   │       │   │   ├── PlanTier.cs            # Enum
│   │       │   │   └── PlanDefinition.cs      # Seed data reference type
│   │       │   ├── Retention/
│   │       │   │   └── RetentionPolicy.cs     # Entity
│   │       │   ├── Features/
│   │       │   │   ├── FeatureToggle.cs       # Entity
│   │       │   │   └── FeatureToggleScope.cs  # Enum
│   │       │   └── Events/
│   │       │       └── TenancyEvents.cs       # Domain + integration events
│   │       ├── Application/
│   │       │   ├── Commands/
│   │       │   │   ├── CreateTenantCommand.cs
│   │       │   │   ├── UpdateTenantBrandingCommand.cs
│   │       │   │   ├── InviteTenantMemberCommand.cs
│   │       │   │   ├── AcceptTenantInviteCommand.cs
│   │       │   │   ├── UpdateTenantMemberRoleCommand.cs
│   │       │   │   ├── RemoveTenantMemberCommand.cs
│   │       │   │   ├── AssignPlanCommand.cs
│   │       │   │   ├── UpgradePlanCommand.cs
│   │       │   │   ├── DowngradePlanCommand.cs
│   │       │   │   ├── UpdateRetentionPolicyCommand.cs
│   │       │   │   ├── CreateFeatureToggleCommand.cs
│   │       │   │   └── UpdateFeatureToggleCommand.cs
│   │       │   ├── Queries/
│   │       │   │   ├── GetTenantQuery.cs
│   │       │   │   ├── GetTenantMembersQuery.cs
│   │       │   │   ├── GetTenantPlanQuery.cs
│   │       │   │   ├── CheckPlanLimitsQuery.cs
│   │       │   │   ├── GetTenantUsageQuery.cs
│   │       │   │   ├── GetUsageHistoryQuery.cs
│   │       │   │   └── GetRetentionPolicyQuery.cs
│   │       │   ├── Services/
│   │       │   │   ├── ITenantContext.cs       # Interface: current tenant resolution
│   │       │   │   ├── IPlanLimitService.cs
│   │       │   │   ├── IBrandingService.cs
│   │       │   │   ├── IFeatureToggleService.cs
│   │       │   │   └── IAlertService.cs
│   │       │   ├── Validators/
│   │       │   │   ├── CreateTenantValidator.cs
│   │       │   │   ├── InviteTenantMemberValidator.cs
│   │       │   │   └── UpdateRetentionPolicyValidator.cs
│   │       │   └── BackgroundJobs/
│   │       │       ├── UsageAggregationJob.cs
│   │       │       ├── DataLifecycleCleanupJob.cs
│   │       │       └── TrialExpirationJob.cs   # NEW: auto-downgrade expired trials
│   │       ├── Infrastructure/
│   │       │   ├── TenancyDbContext.cs         # EF Core DbContext, [tenancy] schema
│   │       │   ├── Services/
│   │       │   │   ├── TenantContextMiddleware.cs  # Resolves tenant + enforces suspension read-only
│   │       │   │   ├── PlanLimitService.cs
│   │       │   │   ├── BrandingService.cs
│   │       │   │   ├── MinIoStorageService.cs
│   │       │   │   ├── FeatureToggleService.cs
│   │       │   │   └── AlertService.cs
│   │       │   └── Middleware/
│   │       │       └── FeatureToggleMiddleware.cs
│   │       └── Api/
│   │           ├── Controllers/
│   │           │   ├── TenantsController.cs
│   │           │   ├── MembersController.cs
│   │           │   ├── PlansController.cs
│   │           │   ├── UsageController.cs
│   │           │   └── RetentionController.cs
│   │           └── Dtos/
│   │               ├── CreateTenantRequest.cs
│   │               ├── TenantResponse.cs
│   │               ├── InviteMemberRequest.cs
│   │               ├── MemberResponse.cs
│   │               ├── PlanResponse.cs
│   │               ├── UsageResponse.cs
│   │               └── RetentionPolicyRequest.cs
│   ├── SharedKernel/
│   │   └── Infrastructure/
│   │       ├── Middleware/
│   │       │   └── FeatureToggleMiddleware.cs  # Shared middleware (if cross-module)
│   │       └── Attributes/
│   │           └── RequiresFeatureAttribute.cs
│   └── Muntada.Api/
│       └── Program.cs                          # Register TenancyDbContext, MediatR assembly
└── tests/
    └── Modules/
        └── Tenancy.Tests/
            ├── Unit/
            │   ├── TenantTests.cs
            │   ├── PlanLimitsTests.cs
            │   ├── MembershipTests.cs
            │   ├── RetentionPolicyTests.cs
            │   └── TrialExpirationTests.cs     # NEW
            └── Integration/
                ├── TenantCreationTests.cs
                ├── MembershipFlowTests.cs
                ├── PlanEnforcementTests.cs
                ├── RetentionCleanupTests.cs
                └── SuspensionEnforcementTests.cs # NEW

frontend/
└── src/
    └── features/
        └── tenancy/
            ├── api/
            │   ├── tenantApi.ts
            │   ├── memberApi.ts
            │   └── planApi.ts
            ├── components/
            │   ├── BrandingEditor.tsx
            │   ├── MemberList.tsx
            │   ├── InviteMemberDialog.tsx
            │   ├── PlanComparison.tsx
            │   ├── UsageProgressBar.tsx
            │   └── RetentionSettings.tsx
            ├── pages/
            │   ├── CreateTenantPage.tsx
            │   ├── TenantSettingsPage.tsx
            │   └── UsageDashboardPage.tsx
            └── hooks/
                └── useTenant.ts

aspire/
└── Muntada.AppHost/
    └── AppHost.cs                              # Add Tenancy module reference
```

**Structure Decision**: Follows the established Identity module pattern — Domain/Application/Infrastructure/Api layers within `backend/src/Modules/Tenancy/`. Frontend follows the feature-based organization under `frontend/src/features/tenancy/`. New additions from clarifications: `TrialExpirationJob.cs`, suspension enforcement in `TenantContextMiddleware.cs`.

## Complexity Tracking

No constitution violations. No complexity justification needed.
