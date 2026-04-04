# Tasks: Tenancy & Plans

**Input**: Design documents from `/specs/002-tenancy-plans/`
**Prerequisites**: plan.md (required), spec.md (required for user stories), research.md, data-model.md, contracts/

**Tests**: TDD is mandatory for critical paths per Constitution Principle IV: plan limit enforcement, membership role validation, retention policy rules, state machine transitions, trial expiration logic, and suspension enforcement. Unit tests are included inline with implementation tasks.

**Clarification Updates (2026-04-04)**: Tasks updated to reflect 3 spec clarifications: trial expiration auto-downgrades to Free tier (T036A-T036B), max 10 tenants per user validated at invite acceptance (T046 updated), suspended tenant read-only enforcement (T033 updated, T041A added).

**Organization**: Tasks are grouped by user story to enable independent implementation and testing of each story.

## Format: `[ID] [P?] [Story] Description`

- **[P]**: Can run in parallel (different files, no dependencies)
- **[Story]**: Which user story this task belongs to (e.g., US1, US2, US3)
- Include exact file paths in descriptions

## Path Conventions

- **Backend**: `backend/src/Modules/Tenancy/` (Domain, Application, Infrastructure, Api layers)
- **SharedKernel**: `backend/src/Muntada.SharedKernel/`
- **API Gateway**: `backend/src/Muntada.Api/`
- **Tests**: `backend/tests/Modules/Tenancy.Tests/`
- **Frontend**: `frontend/src/features/tenancy/`
- **Aspire**: `aspire/Muntada.AppHost/`

---

## Phase 1: Setup (Shared Infrastructure)

**Purpose**: Create Tenancy module structure, project files, and register in the solution

- [x] T001 Create Tenancy module project with `backend/src/Modules/Tenancy/Tenancy.csproj` referencing SharedKernel, MediatR, FluentValidation, EF Core, MassTransit
- [x] T002 Create Tenancy test project with `backend/tests/Modules/Tenancy.Tests/Tenancy.Tests.csproj` referencing xUnit, Moq, and the Tenancy module
- [x] T003 [P] Register Tenancy module in API gateway — add TenancyDbContext, MediatR assembly scan, and Tenancy services in `backend/src/Muntada.Api/Program.cs`
- [x] T004 [P] Register Tenancy module in Aspire AppHost — verify API project references Tenancy in `aspire/Muntada.AppHost/AppHost.cs`
- [x] T005 Create TenancyDbContext with `[tenancy]` schema in `backend/src/Modules/Tenancy/Infrastructure/TenancyDbContext.cs` (empty, with HasDefaultSchema)
- [ ] T006 Run `dotnet ef migrations add InitialTenancy` to create initial empty migration for the `[tenancy]` schema

**Checkpoint**: Tenancy module compiles, is registered in the API gateway and Aspire, and has an empty database schema.

---

## Phase 2: Foundational (Blocking Prerequisites)

**Purpose**: Core domain entities and infrastructure that ALL user stories depend on

**CRITICAL**: No user story work can begin until this phase is complete

- [x] T007 Implement TenantId strongly-typed ID with opaque prefix `tnt_` using OpaqueIdGenerator in `backend/src/Modules/Tenancy/Domain/Tenant/TenantId.cs`
- [x] T008 [P] Implement TenantMembershipId with prefix `mbr_` in `backend/src/Modules/Tenancy/Domain/Membership/TenantMembershipId.cs`
- [x] T009 [P] Implement PlanDefinitionId and TenantPlanId with prefix `pln_` in `backend/src/Modules/Tenancy/Domain/Plan/PlanDefinitionId.cs` and `TenantPlanId.cs`
- [x] T010 [P] Implement RetentionPolicyId with prefix `rtn_` in `backend/src/Modules/Tenancy/Domain/Retention/RetentionPolicyId.cs`
- [x] T011 [P] Implement FeatureToggleId with prefix `ftg_` in `backend/src/Modules/Tenancy/Domain/Features/FeatureToggleId.cs`
- [x] T012 Implement TenantStatus enum (Active, Suspended, Deleted) in `backend/src/Modules/Tenancy/Domain/Tenant/TenantStatus.cs`
- [x] T013 [P] Implement BillingStatus enum (Active, Trial, Overdue, Cancelled) in `backend/src/Modules/Tenancy/Domain/Tenant/BillingStatus.cs`
- [x] T014 [P] Implement TenantRole enum (Owner, Admin, Member) in `backend/src/Modules/Tenancy/Domain/Membership/TenantRole.cs`
- [x] T015 [P] Implement TenantMembershipStatus enum (Active, Pending, Inactive) in `backend/src/Modules/Tenancy/Domain/Membership/TenantMembershipStatus.cs`
- [x] T016 [P] Implement PlanTier enum (Free, Trial, Starter, Professional, Enterprise) in `backend/src/Modules/Tenancy/Domain/Plan/PlanTier.cs`
- [x] T017 [P] Implement FeatureToggleScope enum (Global, PerTenant, PerUser, PerRegion, Canary) in `backend/src/Modules/Tenancy/Domain/Features/FeatureToggleScope.cs`
- [x] T018 Implement TenantSlug value object with validation (lowercase alphanumeric + hyphens, 3-63 chars, reserved word check) in `backend/src/Modules/Tenancy/Domain/Tenant/TenantSlug.cs`
- [x] T019 Implement TenantBranding value object (LogoUrl, PrimaryColor, SecondaryColor, CustomDomain with hex color validation) in `backend/src/Modules/Tenancy/Domain/Tenant/TenantBranding.cs`
- [x] T020 Implement PlanLimits value object (MaxRoomsPerMonth, MaxParticipantsPerRoom, MaxStorageGB, MaxRecordingHoursPerMonth, MaxDataRetentionDays, AllowRecording, AllowGuestAccess, AllowCustomBranding) in `backend/src/Modules/Tenancy/Domain/Plan/PlanLimits.cs`
- [x] T021 Implement Tenant aggregate root (Id, Name, Slug, Branding, Status, BillingStatus, TrialEndsAt, CreatedBy, state transition methods) in `backend/src/Modules/Tenancy/Domain/Tenant/Tenant.cs`
- [x] T022 Write unit tests for Tenant aggregate — state transitions, slug validation, branding validation in `backend/tests/Modules/Tenancy.Tests/Unit/Domain/TenantTests.cs`
- [x] T023 Implement TenantMembership entity (TenantId, UserId, InvitedEmail, Role, Status, JoinedAt, InvitedAt, InvitedBy, status transition methods, Owner guard) in `backend/src/Modules/Tenancy/Domain/Membership/TenantMembership.cs`
- [x] T024 [P] Implement TenantInviteToken value object (Token, ExpiresAt, IsUsed) in `backend/src/Modules/Tenancy/Domain/Membership/TenantInviteToken.cs`
- [x] T025 Write unit tests for TenantMembership — role validation, last-Owner prevention, status transitions in `backend/tests/Modules/Tenancy.Tests/Unit/Domain/TenantMembershipTests.cs`
- [x] T026 Implement PlanDefinition entity (Id, Name, Tier, MonthlyPriceUsd, Limits, IsActive) in `backend/src/Modules/Tenancy/Domain/Plan/PlanDefinition.cs`
- [x] T027 [P] Implement TenantPlan entity (Id, TenantId, PlanDefinitionId, StartDate, EndDate, IsCurrent) in `backend/src/Modules/Tenancy/Domain/Plan/TenantPlan.cs`
- [x] T028 [P] Implement RetentionPolicy entity (Id, TenantId, per-type retention days, audit log min 2555 days validation) in `backend/src/Modules/Tenancy/Domain/Retention/RetentionPolicy.cs`
- [x] T029 [P] Implement FeatureToggle entity (Id, FeatureName, IsEnabled, Scope, CanaryPercentage) and FeatureToggleOverride owned entity in `backend/src/Modules/Tenancy/Domain/Features/FeatureToggle.cs`
- [x] T030 Implement TenancyEvents — domain events (TenantCreatedDomainEvent, MembershipChangedDomainEvent) and integration events (TenantCreatedEvent, TenantMembershipChangedEvent, PlanChangedEvent, UsageLimitExceededEvent, RetentionPolicyChangedEvent, FeatureToggleChangedEvent) in `backend/src/Modules/Tenancy/Domain/Events/TenancyEvents.cs`
- [x] T031 Configure EF Core entity mappings for all entities in TenancyDbContext — Tenant, TenantMembership, TenantInviteToken, PlanDefinition, TenantPlan, RetentionPolicy, FeatureToggle, FeatureToggleOverride, TenantUsageSnapshot with proper indexes in `backend/src/Modules/Tenancy/Infrastructure/TenancyDbContext.cs`
- [ ] T032 Run `dotnet ef migrations add AddTenancyEntities` to generate migration for all entities
- [x] T033 Implement ITenantContext interface in `backend/src/Modules/Tenancy/Application/Services/ITenantContext.cs` and TenantContextMiddleware that resolves tenant from JWT claims or X-Tenant-ID header AND enforces suspension read-only (allow GET/HEAD, reject POST/PUT/PATCH/DELETE with 403 "Tenant suspended") in `backend/src/Modules/Tenancy/Infrastructure/Services/TenantContextMiddleware.cs`
- [x] T034 Seed PlanDefinition data (Free, Trial, Starter, Professional, Enterprise) via EF Core HasData or migration seed in `backend/src/Modules/Tenancy/Infrastructure/TenancyDbContext.cs`
- [ ] T035 Run `dotnet ef migrations add SeedPlanDefinitions` to generate seed data migration
- [x] T036A Implement TrialExpirationJob — daily background job that queries tenants where BillingStatus=Trial AND TrialEndsAt < now, assigns Free plan definition, transitions BillingStatus to Active, publishes PlanChangedEvent in `backend/src/Modules/Tenancy/Application/BackgroundJobs/TrialExpirationJob.cs`
- [ ] T036B [P] Write unit tests for TrialExpirationJob — verify auto-downgrade to Free, data preservation, BillingStatus transition, edge case where trial is already expired in `backend/tests/Modules/Tenancy.Tests/Unit/Application/TrialExpirationJobTests.cs`
- [ ] T036C [P] Write unit tests for TenantContextMiddleware suspension enforcement — GET allowed, POST rejected with 403, read-only error message in `backend/tests/Modules/Tenancy.Tests/Unit/Infrastructure/SuspensionEnforcementTests.cs`

**Checkpoint**: All domain entities, value objects, enums, DB schema, tenant context middleware (with suspension read-only), and trial expiration job are in place. User story implementation can begin.

---

## Phase 3: User Story 1 — Tenant Creation & Onboarding (Priority: P1) MVP

**Goal**: Authenticated users can create a tenant, get auto-assigned as Owner with a Trial plan, and access the tenant dashboard.

**Independent Test**: Complete the onboarding form, verify tenant created with unique slug, creator is Owner, Trial plan active.

### Implementation for User Story 1

- [ ] T036 [US1] Write unit tests for CreateTenantCommand — valid creation, duplicate slug rejection, reserved slug rejection, creator-as-Owner assignment, Trial plan assignment in `backend/tests/Modules/Tenancy.Tests/Unit/Application/CreateTenantCommandTests.cs`
- [x] T037 [US1] Implement CreateTenantCommand and CreateTenantCommandHandler — slug generation from name, uniqueness check, creator added as Owner, default Trial plan assigned, TenantCreated domain event raised, TenantCreatedEvent integration event published in `backend/src/Modules/Tenancy/Application/Commands/CreateTenantCommand.cs`
- [x] T038 [US1] Implement CreateTenantValidator with FluentValidation — name length (3-100), slug format, slug uniqueness in `backend/src/Modules/Tenancy/Application/Validators/CreateTenantValidator.cs`
- [x] T039 [US1] Implement GetTenantQuery and handler — return tenant with branding, status, billing status in `backend/src/Modules/Tenancy/Application/Queries/GetTenantQuery.cs`
- [x] T040 [US1] Implement TenantsController with POST /api/v1/tenants (create) and GET /api/v1/tenants/{tenantId} (retrieve) endpoints, DTOs (CreateTenantRequest, TenantResponse) in `backend/src/Modules/Tenancy/Api/Controllers/TenantsController.cs` and `backend/src/Modules/Tenancy/Api/Dtos/`
- [ ] T041 [US1] Write integration tests for tenant creation flow — end-to-end POST, slug uniqueness, GET by ID in `backend/tests/Modules/Tenancy.Tests/Integration/TenantCreationTests.cs`
- [ ] T041A [US1] Write integration tests for suspension enforcement — verify GET returns data for suspended tenant, verify POST/PATCH/DELETE return 403 with "Tenant suspended" message in `backend/tests/Modules/Tenancy.Tests/Integration/SuspensionEnforcementTests.cs`

**Checkpoint**: Tenants can be created via API, creator is Owner, Trial plan assigned. Suspension read-only enforced. This is the MVP — independently testable and deployable.

---

## Phase 4: User Story 2 — Tenant Membership & Roles (Priority: P1)

**Goal**: Tenant owners invite members via email, assign roles, update roles, remove members with proper authorization.

**Independent Test**: Invite a user by email, accept invite, verify role grants correct access, verify last-Owner protection.

### Implementation for User Story 2

- [ ] T042 [US2] Write unit tests for InviteTenantMemberCommand — valid invite, duplicate email handling, role assignment, token generation in `backend/tests/Modules/Tenancy.Tests/Unit/Application/InviteTenantMemberCommandTests.cs`
- [ ] T043 [US2] Write unit tests for role management — UpdateTenantMemberRoleCommand (role change, last-Owner prevention) and RemoveTenantMemberCommand (removal, last-Owner guard) in `backend/tests/Modules/Tenancy.Tests/Unit/Application/MembershipRoleTests.cs`
- [x] T044 [US2] Implement InviteTenantMemberCommand and handler — create membership as Pending, generate TenantInviteToken (32-byte base64url, 7-day expiry), send invite email via IEmailService, publish TenantMembershipChangedEvent in `backend/src/Modules/Tenancy/Application/Commands/InviteTenantMemberCommand.cs`
- [x] T045 [US2] Implement InviteTenantMemberValidator — email format, role validation, Owner-only for Owner role assignment in `backend/src/Modules/Tenancy/Application/Validators/InviteTenantMemberValidator.cs`
- [x] T046 [US2] Implement AcceptTenantInviteCommand and handler — validate token, check user has < 10 active tenants (configurable limit, reject with "Maximum organization limit reached" if exceeded), transition membership Pending → Active, mark token as used in `backend/src/Modules/Tenancy/Application/Commands/AcceptTenantInviteCommand.cs`
- [x] T047 [US2] Implement UpdateTenantMemberRoleCommand and handler — role change with Owner-only authorization, last-Owner guard in `backend/src/Modules/Tenancy/Application/Commands/UpdateTenantMemberRoleCommand.cs`
- [x] T048 [US2] Implement RemoveTenantMemberCommand and handler — soft-delete to Inactive, last-Owner guard, publish TenantMembershipChangedEvent in `backend/src/Modules/Tenancy/Application/Commands/RemoveTenantMemberCommand.cs`
- [x] T049 [US2] Implement GetTenantMembersQuery and handler — paginated list with filters (status), includes user display name and email in `backend/src/Modules/Tenancy/Application/Queries/GetTenantMembersQuery.cs`
- [x] T050 [US2] Implement MembersController with all endpoints — GET (list), POST /invite, POST /accept, PATCH /{memberId}/role, DELETE /{memberId} with DTOs (InviteMemberRequest, MemberResponse) in `backend/src/Modules/Tenancy/Api/Controllers/MembersController.cs` and `backend/src/Modules/Tenancy/Api/Dtos/`
- [ ] T051 [US2] Write integration tests for membership flow — invite, accept, role change, removal, last-Owner protection, max-10-tenants-per-user rejection in `backend/tests/Modules/Tenancy.Tests/Integration/MembershipFlowTests.cs`

**Checkpoint**: Full membership lifecycle works — invite, accept, role management, removal with proper authorization.

---

## Phase 5: User Story 3 — Plan Management & Limit Enforcement (Priority: P1)

**Goal**: Plans with resource limits are enforced at room/recording/storage creation time, with clear error messages.

**Independent Test**: Assign a plan to a tenant, consume resources up to the limit, verify exceeding the limit is blocked.

### Implementation for User Story 3

- [ ] T052 [US3] Write unit tests for PlanLimitService — limit checks for rooms, participants, storage, recording hours, feature permission checks (AllowRecording, AllowGuestAccess) in `backend/tests/Modules/Tenancy.Tests/Unit/Application/PlanLimitServiceTests.cs`
- [x] T053 [US3] Implement IPlanLimitService interface in `backend/src/Modules/Tenancy/Application/Services/IPlanLimitService.cs` and PlanLimitService implementation — check current usage against plan limits, return limit status with clear error messages in `backend/src/Modules/Tenancy/Infrastructure/Services/PlanLimitService.cs`
- [x] T054 [US3] Implement AssignPlanCommand and handler — assign plan definition to tenant, create TenantPlan record, set IsCurrent, publish PlanChangedEvent in `backend/src/Modules/Tenancy/Application/Commands/AssignPlanCommand.cs`
- [x] T055 [US3] Implement GetTenantPlanQuery and handler — return current plan with limits in `backend/src/Modules/Tenancy/Application/Queries/GetTenantPlanQuery.cs`
- [x] T056 [US3] Implement CheckPlanLimitsQuery and handler — check specific resource type against current plan, return allowed/denied with message in `backend/src/Modules/Tenancy/Application/Queries/CheckPlanLimitsQuery.cs`
- [x] T057 [US3] Implement PlansController with endpoints — GET /api/v1/tenants/{tenantId}/plan, GET /api/v1/plans/available, DTOs (PlanResponse, PlanDefinitionResponse) in `backend/src/Modules/Tenancy/Api/Controllers/PlansController.cs` and `backend/src/Modules/Tenancy/Api/Dtos/`
- [ ] T058 [US3] Write integration tests for plan enforcement — assign plan, check limits at boundary, verify rejection when exceeded in `backend/tests/Modules/Tenancy.Tests/Integration/PlanEnforcementTests.cs`

**Checkpoint**: Plans enforce resource limits. Rooms/recordings are blocked when limits exceeded. Core P1 stories are complete.

---

## Phase 6: User Story 4 — Plan Upgrades & Downgrades (Priority: P2)

**Goal**: Tenant owners can upgrade (immediate, pro-rated) or downgrade (immediate or next cycle) their plan.

**Independent Test**: Upgrade plan mid-cycle, verify new limits apply and pro-ration calculated. Downgrade with over-quota warning.

### Implementation for User Story 4

- [ ] T059 [US4] Write unit tests for plan change logic — pro-ration calculation, downgrade usage validation, concurrent change rejection in `backend/tests/Modules/Tenancy.Tests/Unit/Application/PlanChangeTests.cs`
- [x] T060 [US4] Implement UpgradePlanCommand and handler — validate higher tier, calculate pro-rated charge, assign new plan immediately, end previous plan, publish PlanChangedEvent in `backend/src/Modules/Tenancy/Application/Commands/UpgradePlanCommand.cs`
- [x] T061 [US4] Implement DowngradePlanCommand and handler — validate lower tier, check current usage vs new limits, support immediate or next-billing-cycle effective date, publish PlanChangedEvent in `backend/src/Modules/Tenancy/Application/Commands/DowngradePlanCommand.cs`
- [x] T062 [US4] Add upgrade/downgrade endpoints to PlansController — POST /api/v1/tenants/{tenantId}/plan/upgrade and POST /api/v1/tenants/{tenantId}/plan/downgrade with DTOs in `backend/src/Modules/Tenancy/Api/Controllers/PlansController.cs`
- [ ] T063 [US4] Write integration tests for plan changes — upgrade with pro-ration, downgrade with usage warning, concurrent change conflict in `backend/tests/Modules/Tenancy.Tests/Integration/PlanChangeTests.cs`

**Checkpoint**: Plan upgrades and downgrades work with pro-ration and validation.

---

## Phase 7: User Story 5 — Usage Tracking & Reporting (Priority: P2)

**Goal**: Tenant admins see a usage dashboard with metrics, progress bars, threshold alerts, and 30-day history.

**Independent Test**: Consume resources, verify dashboard shows accurate metrics with correct threshold colors.

### Implementation for User Story 5

- [x] T064 [US5] Implement TenantUsageSnapshot entity mapping (if not already in T031) and verify index on (TenantId, SnapshotDate) in `backend/src/Modules/Tenancy/Infrastructure/TenancyDbContext.cs`
- [x] T065 [US5] Implement UsageAggregationJob — daily background job that aggregates room count, storage, recording hours per tenant into TenantUsageSnapshot, caches current metrics in Redis in `backend/src/Modules/Tenancy/Application/BackgroundJobs/UsageAggregationJob.cs`
- [x] T066 [US5] Implement IAlertService interface in `backend/src/Modules/Tenancy/Application/Services/IAlertService.cs` and AlertService — send threshold notifications (95%, 100%) to tenant owners, publish UsageLimitExceededEvent in `backend/src/Modules/Tenancy/Infrastructure/Services/AlertService.cs`
- [x] T067 [US5] Implement GetTenantUsageQuery and handler — return current usage metrics from Redis cache (fallback to DB), calculate percentage and threshold status in `backend/src/Modules/Tenancy/Application/Queries/GetTenantUsageQuery.cs`
- [x] T068 [US5] Implement GetUsageHistoryQuery and handler — return daily snapshots for last N days (default 30, max 90) in `backend/src/Modules/Tenancy/Application/Queries/GetUsageHistoryQuery.cs`
- [x] T069 [US5] Implement UsageController with GET /api/v1/tenants/{tenantId}/usage and GET /api/v1/tenants/{tenantId}/usage/history endpoints with DTOs (UsageResponse, UsageHistoryResponse) in `backend/src/Modules/Tenancy/Api/Controllers/UsageController.cs` and `backend/src/Modules/Tenancy/Api/Dtos/`
- [ ] T070 [US5] Write integration tests for usage tracking — verify aggregation, Redis caching, threshold alerts in `backend/tests/Modules/Tenancy.Tests/Integration/UsageTrackingTests.cs`

**Checkpoint**: Usage dashboard API returns accurate metrics with threshold statuses. Alerts fire at thresholds.

---

## Phase 8: User Story 6 — Retention Policies & Data Lifecycle (Priority: P2)

**Goal**: Tenant owners configure retention periods. Expired data is soft-deleted with 7-day grace, then hard-deleted with audit trail.

**Independent Test**: Set short retention, verify soft-delete, restore during grace, confirm hard-delete after grace.

### Implementation for User Story 6

- [ ] T071 [US6] Write unit tests for RetentionPolicy — validation (min/max days, audit log 7-year minimum), retroactive reduction scheduling in `backend/tests/Modules/Tenancy.Tests/Unit/Domain/RetentionPolicyTests.cs`
- [x] T072 [US6] Implement UpdateRetentionPolicyCommand and handler — update per-type retention days, validate audit log minimum (2555), schedule retroactive deletions if reduced, publish RetentionPolicyChangedEvent in `backend/src/Modules/Tenancy/Application/Commands/UpdateRetentionPolicyCommand.cs`
- [x] T073 [US6] Implement UpdateRetentionPolicyValidator — min 1, max 3650, audit log min 2555 in `backend/src/Modules/Tenancy/Application/Validators/UpdateRetentionPolicyValidator.cs`
- [x] T074 [US6] Implement GetRetentionPolicyQuery and handler — return current policy with defaults and allowed ranges in `backend/src/Modules/Tenancy/Application/Queries/GetRetentionPolicyQuery.cs`
- [x] T075 [US6] Implement DataLifecycleCleanupJob — daily background job: identify expired data, soft-delete (set ScheduledForDeletionAt), hard-delete after 7-day grace, log all deletions to audit trail, retry with exponential backoff in `backend/src/Modules/Tenancy/Application/BackgroundJobs/DataLifecycleCleanupJob.cs`
- [x] T076 [US6] Implement RetentionController with GET /api/v1/tenants/{tenantId}/retention-policies and PATCH /api/v1/tenants/{tenantId}/retention-policies endpoints with DTOs (RetentionPolicyResponse, UpdateRetentionPolicyRequest) in `backend/src/Modules/Tenancy/Api/Controllers/RetentionController.cs` and `backend/src/Modules/Tenancy/Api/Dtos/`
- [ ] T077 [US6] Write integration tests for retention lifecycle — policy update, soft-delete scheduling, grace period restore, hard-delete, audit trail in `backend/tests/Modules/Tenancy.Tests/Integration/RetentionCleanupTests.cs`

**Checkpoint**: Retention policies configurable. Data lifecycle (soft-delete → grace → hard-delete) works with audit trail.

---

## Phase 9: User Story 7 — Tenant Branding & Customization (Priority: P2)

**Goal**: Tenant owners upload logos, set brand colors, and configure custom subdomains. Branding displayed across rooms.

**Independent Test**: Upload logo, set colors, verify they appear on tenant pages and API responses.

### Implementation for User Story 7

- [x] T078 [US7] Implement IBrandingService interface in `backend/src/Modules/Tenancy/Application/Services/IBrandingService.cs` — logo upload, image resizing, MinIO storage
- [x] T079 [US7] Implement BrandingService — upload logo to MinIO at `tenants/{tenantId}/logo-{size}.png`, resize to 32px, 64px, 128px, 256px using ImageSharp, set cache headers in `backend/src/Modules/Tenancy/Infrastructure/Services/BrandingService.cs`
- [x] T080 [US7] Implement MinIoStorageService — S3-compatible upload/download/delete operations for tenant files in `backend/src/Modules/Tenancy/Infrastructure/Services/MinIoStorageService.cs`
- [x] T081 [US7] Implement UpdateTenantBrandingCommand and handler — validate hex colors, upload logo via IBrandingService, validate custom domain uniqueness in `backend/src/Modules/Tenancy/Application/Commands/UpdateTenantBrandingCommand.cs`
- [x] T082 [US7] Add PATCH /api/v1/tenants/{tenantId}/branding endpoint (multipart/form-data) to TenantsController with branding DTOs in `backend/src/Modules/Tenancy/Api/Controllers/TenantsController.cs`
- [ ] T083 [US7] Write integration tests for branding — logo upload and resize, color validation, custom domain validation in `backend/tests/Modules/Tenancy.Tests/Integration/BrandingTests.cs`

**Checkpoint**: Branding (logo, colors, subdomain) configurable via API. Logos stored and resized in MinIO.

---

## Phase 10: User Story 8 — Feature Toggles & Gradual Rollout (Priority: P3)

**Goal**: Product team controls feature availability per tenant via toggles with scoped evaluation and Redis caching.

**Independent Test**: Create toggle, enable for specific tenant, verify enabled tenant has access and others are blocked.

### Implementation for User Story 8

- [ ] T084 [US8] Write unit tests for FeatureToggleService — scope evaluation (per-tenant, canary percentage, global), cache behavior in `backend/tests/Modules/Tenancy.Tests/Unit/Application/FeatureToggleServiceTests.cs`
- [x] T085 [US8] Implement IFeatureToggleService interface in `backend/src/Modules/Tenancy/Application/Services/IFeatureToggleService.cs` — check if feature enabled for tenant/user/region
- [x] T086 [US8] Implement FeatureToggleService — evaluate scopes (Global, PerTenant overrides, Canary percentage), cache in Redis with 5-minute TTL, fallback to DB in `backend/src/Modules/Tenancy/Infrastructure/Services/FeatureToggleService.cs`
- [x] T087 [US8] Implement CreateFeatureToggleCommand and UpdateFeatureToggleCommand with handlers — create/update toggles, manage overrides, publish FeatureToggleChangedEvent in `backend/src/Modules/Tenancy/Application/Commands/CreateFeatureToggleCommand.cs` and `backend/src/Modules/Tenancy/Application/Commands/UpdateFeatureToggleCommand.cs`
- [x] T088 [US8] Implement FeatureToggleMiddleware — check RequiresFeature attribute on controllers, return 403 if disabled, integrated with IFeatureToggleService in `backend/src/Modules/Tenancy/Infrastructure/Middleware/FeatureToggleMiddleware.cs`
- [x] T089 [US8] Implement RequiresFeatureAttribute for controller/action decoration in `backend/src/Muntada.SharedKernel/Infrastructure/Attributes/RequiresFeatureAttribute.cs`
- [x] T090 [US8] Implement admin FeatureTogglesController — CRUD for toggles, tenant overrides, GET enabled features for tenant in `backend/src/Modules/Tenancy/Api/Controllers/FeatureTogglesController.cs` and DTOs
- [ ] T091 [US8] Write integration tests for feature toggles — create toggle, enable per-tenant, verify 403 for disabled, verify canary percentage in `backend/tests/Modules/Tenancy.Tests/Integration/FeatureToggleTests.cs`

**Checkpoint**: Feature toggles work with scoped evaluation, Redis caching, and middleware enforcement.

---

## Phase 11: Frontend — Tenant Management UI (Priority: P2)

**Goal**: React SPA pages for tenant creation, settings, member management, plan display, and usage dashboard.

**Independent Test**: Navigate to tenant creation, fill form, verify tenant appears. Edit branding, invite members, view usage.

### Implementation for Frontend

- [x] T092 [P] Create tenantApi.ts — API client for tenant CRUD, branding update endpoints using baseApi in `frontend/src/features/tenancy/api/tenantApi.ts`
- [x] T093 [P] Create memberApi.ts — API client for member list, invite, role update, removal in `frontend/src/features/tenancy/api/memberApi.ts`
- [x] T094 [P] Create planApi.ts — API client for plan retrieval, upgrade, downgrade, usage in `frontend/src/features/tenancy/api/planApi.ts`
- [x] T095 [P] Implement useTenant hook — tenant context management, current tenant state in `frontend/src/features/tenancy/hooks/useTenant.ts`
- [x] T096 Implement CreateTenantPage — onboarding form (name, slug auto-generation, industry, team size), form validation, API integration in `frontend/src/features/tenancy/pages/CreateTenantPage.tsx`
- [x] T097 [P] Implement BrandingEditor component — logo upload (drag & drop, max 5MB), color picker for primary/secondary, custom domain input in `frontend/src/features/tenancy/components/BrandingEditor.tsx`
- [x] T098 [P] Implement MemberList component — paginated member table with role badges, action buttons (edit role, remove) in `frontend/src/features/tenancy/components/MemberList.tsx`
- [x] T099 [P] Implement InviteMemberDialog component — email input, role selector, optional message in `frontend/src/features/tenancy/components/InviteMemberDialog.tsx`
- [x] T100 [P] Implement PlanComparison component — compare current plan with available plans, feature comparison table in `frontend/src/features/tenancy/components/PlanComparison.tsx`
- [x] T101 [P] Implement UsageProgressBar component — progress bar with color changes at 80%/95%/100% thresholds in `frontend/src/features/tenancy/components/UsageProgressBar.tsx`
- [x] T102 [P] Implement RetentionSettings component — retention period inputs per data type with validation in `frontend/src/features/tenancy/components/RetentionSettings.tsx`
- [x] T103 Implement TenantSettingsPage — tabs for branding, members, plan, usage, retention settings using above components in `frontend/src/features/tenancy/pages/TenantSettingsPage.tsx`
- [x] T104 Implement UsageDashboardPage — usage metrics with progress bars, history charts, alert indicators in `frontend/src/features/tenancy/pages/UsageDashboardPage.tsx`
- [x] T105 Add tenancy routes to App.tsx — /create-tenant, /tenant/settings, /tenant/usage, /join-tenant?token=... in `frontend/src/App.tsx`
- [x] T106 Write Playwright E2E tests for tenant creation and settings flows in `frontend/tests/e2e/tenancy.spec.ts`

**Checkpoint**: Frontend pages for tenant management are functional and connected to backend APIs.

---

## Phase 12: Polish & Cross-Cutting Concerns

**Purpose**: Improvements that affect multiple user stories

- [x] T107 [P] Add OpenTelemetry instrumentation — custom traces for tenant creation, plan changes, membership changes, feature toggle evaluation in `backend/src/Modules/Tenancy/Infrastructure/TenancyTelemetry.cs`
- [x] T108 [P] Add structured logging with Serilog — log tenant context, plan enforcement decisions, retention actions in all command handlers
- [x] T109 [P] Add XML documentation comments to all public types, methods, and properties across the Tenancy module (Constitution Principle VIII)
- [x] T110 Verify multi-tenant data isolation — write tests confirming queries without TenantId context return empty results in `backend/tests/Modules/Tenancy.Tests/Integration/TenantIsolationTests.cs`
- [x] T111 [P] Add audit logging for all state changes — tenant creation, membership changes, plan changes, retention policy changes, feature toggle changes using SharedKernel AuditLog entity
- [x] T112 Run quickstart.md validation — verify all setup steps work, all endpoints respond, all tests pass
- [x] T113 Security review — verify authorization on all endpoints (Owner-only, Admin-only, member-only), verify no cross-tenant data leakage

---

## Dependencies & Execution Order

### Phase Dependencies

- **Setup (Phase 1)**: No dependencies — can start immediately
- **Foundational (Phase 2)**: Depends on Setup completion — BLOCKS all user stories
- **US1 Tenant Creation (Phase 3)**: Depends on Foundational — MVP
- **US2 Membership (Phase 4)**: Depends on Foundational — can parallel with US1
- **US3 Plan Enforcement (Phase 5)**: Depends on Foundational — can parallel with US1, US2
- **US4 Plan Changes (Phase 6)**: Depends on US3 (plan enforcement)
- **US5 Usage Tracking (Phase 7)**: Depends on US3 (plan limits for threshold comparison)
- **US6 Retention (Phase 8)**: Depends on Foundational only — can parallel with US1-US3
- **US7 Branding (Phase 9)**: Depends on US1 (tenant must exist)
- **US8 Feature Toggles (Phase 10)**: Depends on Foundational only — can parallel with any story
- **Frontend (Phase 11)**: Depends on Phases 3-10 backend APIs being available
- **Polish (Phase 12)**: Depends on all story phases being complete

### User Story Dependencies

```
Phase 2: Foundational ─────┬──── US1 (Phase 3) ── MVP!
                            ├──── US2 (Phase 4) ── can parallel with US1
                            ├──── US3 (Phase 5) ──┬── US4 (Phase 6)
                            │                      └── US5 (Phase 7)
                            ├──── US6 (Phase 8) ── independent
                            ├──── US7 (Phase 9) ── needs US1
                            └──── US8 (Phase 10) ── independent
```

### Within Each User Story

1. Unit tests FIRST (TDD for critical paths — plan limits, role validation, retention rules)
2. Domain logic / commands before API endpoints
3. Validators alongside commands
4. Queries after commands
5. Controllers and DTOs last
6. Integration tests to verify end-to-end

### Parallel Opportunities

- **Phase 2**: T007-T011 (ID types), T012-T017 (enums), T018-T020 (value objects) all parallelizable
- **Phase 2**: T026-T029 (entities: PlanDefinition, TenantPlan, RetentionPolicy, FeatureToggle) parallelizable
- **After Phase 2**: US1, US2, US3, US6, US8 can all start in parallel (no cross-dependencies)
- **Phase 11**: All API clients (T092-T094) parallelizable; all components (T097-T102) parallelizable

---

## Parallel Example: Phase 2 Foundational

```text
# Wave 1: All ID types in parallel
Task T007: TenantId
Task T008: TenantMembershipId  [P]
Task T009: PlanDefinitionId + TenantPlanId  [P]
Task T010: RetentionPolicyId  [P]
Task T011: FeatureToggleId  [P]

# Wave 2: All enums in parallel
Task T012-T017: All enum types  [P]

# Wave 3: All value objects in parallel
Task T018: TenantSlug
Task T019: TenantBranding  [P]
Task T020: PlanLimits  [P]

# Wave 4: Aggregate + entities in parallel
Task T021: Tenant aggregate
Task T023: TenantMembership  (after T021)
Task T026: PlanDefinition  [P]
Task T027: TenantPlan  [P]
Task T028: RetentionPolicy  [P]
Task T029: FeatureToggle  [P]
```

## Parallel Example: User Story 2 (Membership)

```text
# Wave 1: Tests first (TDD)
Task T042: InviteTenantMemberCommand tests  [P]
Task T043: Membership role management tests  [P]

# Wave 2: Commands (can parallel different commands)
Task T044: InviteTenantMemberCommand
Task T045: InviteTenantMemberValidator  [P with T044]

# Wave 3: More commands
Task T046: AcceptTenantInviteCommand
Task T047: UpdateTenantMemberRoleCommand  [P]
Task T048: RemoveTenantMemberCommand  [P]

# Wave 4: Query + Controller
Task T049: GetTenantMembersQuery
Task T050: MembersController (after T044-T049)
Task T051: Integration tests (after T050)
```

---

## Implementation Strategy

### MVP First (User Story 1 Only)

1. Complete Phase 1: Setup
2. Complete Phase 2: Foundational (CRITICAL — blocks all stories)
3. Complete Phase 3: User Story 1 — Tenant Creation
4. **STOP and VALIDATE**: Test tenant creation independently
5. Deploy/demo if ready

### Incremental Delivery

1. Setup + Foundational → Foundation ready
2. Add US1 (Tenant Creation) → Test → Deploy (MVP!)
3. Add US2 (Membership) + US3 (Plan Limits) → Test → Deploy (Core functionality)
4. Add US4 (Plan Changes) + US5 (Usage) → Test → Deploy (Business operations)
5. Add US6 (Retention) + US7 (Branding) → Test → Deploy (Enterprise features)
6. Add US8 (Feature Toggles) → Test → Deploy (Platform capabilities)
7. Add Frontend (Phase 11) → Test → Deploy (User-facing UI)
8. Polish (Phase 12) → Final release

### Parallel Team Strategy

With multiple developers:

1. Team completes Setup + Foundational together
2. Once Foundational is done:
   - Developer A: US1 (Tenant Creation) + US7 (Branding)
   - Developer B: US2 (Membership) + US6 (Retention)
   - Developer C: US3 (Plan Limits) → US4 (Plan Changes) → US5 (Usage)
   - Developer D: US8 (Feature Toggles) + Frontend (Phase 11)
3. Stories complete and integrate independently

---

## Notes

- [P] tasks = different files, no dependencies
- [Story] label maps task to specific user story for traceability
- Each user story should be independently completable and testable
- TDD mandatory for: plan limit enforcement (T052), membership role validation (T042-T043), retention policy rules (T071), trial expiration (T036B), suspension enforcement (T036C), max-tenants-per-user (T046)
- **Commit after each task** — one Git commit per completed task, not batched
- **All unit tests MUST pass** before each commit
- Stop at any checkpoint to validate story independently
- Avoid: vague tasks, same file conflicts, cross-story dependencies that break independence

## Git & PR Workflow (per Constitution)

- **GitHub Issues**: Create a GitHub issue for each task before implementation begins. Close it upon completion.
- **PR per Phase**: Create a Pull Request at the end of each phase with a detailed summary of all changes.
- **Code Review**: Run code review before submitting any PR. Fix all findings first.
- **Phase Summary**: Include a detailed summary of all implemented tasks when the phase is completed.
- **Database Migrations**: NEVER generate migrations via AI — use `dotnet ef migrations add` only.
- **Aspire AppHost**: Every new module MUST register itself in the Aspire AppHost project. Local dev runs via `dotnet run --project aspire/Muntada.AppHost`.
