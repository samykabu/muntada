# Epic 2: Tenancy & Plans Module - Task Breakdown

**Version:** 1.0
**Epic Owner:** Platform / Product
**Last Updated:** 2026-04-03

---

## Execution Overview

This epic implements multi-tenancy and plan management. Depends on Identity (Epic 1) and Shared Kernel (Epic 0). Tasks organize into 5 phases: infrastructure, tenant management, plan management, retention policies, and feature toggles.

---

## Phase 1: Module Setup & Entities

### T201: Tenancy Module Structure & Database [P]
**User Story:** US-2.1
**Priority:** P1
**Effort:** 5 pts
**Dependencies:** Epic 0, Epic 1

Create Tenancy module structure and database schema.

**File Locations:**
- `backend/src/Modules/Tenancy/Tenancy.csproj`
- `backend/src/Modules/Tenancy/Infrastructure/TenancyDbContext.cs`
- SQL schema: `[tenancy]`

**Acceptance:**
- Module compiles
- Schema created
- Migrations run

---

### T202: Tenant & TenantBranding Entities [P]
**User Story:** US-2.1, US-2.2
**Priority:** P1
**Effort:** 5 pts
**Dependencies:** T201

Implement Tenant aggregate and branding value object.

**Deliverables:**
- `Tenant` aggregate (Id, Name, Slug, Status, BillingStatus, CreatedBy, CreatedAt, UpdatedAt)
- `TenantBranding` value object (LogoUrl, PrimaryColor, SecondaryColor, CustomDomain)
- Slug validation and uniqueness

**File Locations:**
- `backend/src/Modules/Tenancy/Domain/Tenant/Tenant.cs`
- `backend/src/Modules/Tenancy/Domain/Tenant/TenantBranding.cs`

**Acceptance:**
- Entities compile
- Slug format validated
- Branding modeled correctly

---

### T203: TenantMembership Entity [P]
**User Story:** US-2.3
**Priority:** P1
**Effort:** 3 pts
**Dependencies:** T202

Implement tenant membership with roles.

**Deliverables:**
- `TenantMembership` entity (TenantId, UserId, Role, Status, JoinedAt, InvitedAt, InvitedBy)
- `TenantRole` enum (Owner, Admin, Member)
- `TenantMembershipStatus` enum (Active, Pending, Inactive)

**File Locations:**
- `backend/src/Modules/Tenancy/Domain/Membership/TenantMembership.cs`

**Acceptance:**
- Entity models roles correctly
- Database constraints enforceable

---

### T204: Plan & PlanLimits Entities [P]
**User Story:** US-2.4
**Priority:** P1
**Effort:** 5 pts
**Dependencies:** T202

Implement Plan and limits.

**Deliverables:**
- `Plan` entity (TenantId, Name, Tier, MonthlyPrice, Limits, FeatureFlags, StartDate, EndDate)
- `PlanLimits` value object (MaxRoomsPerMonth, MaxParticipantsPerRoom, MaxStorageGB, MaxRecordingHoursPerMonth, AllowRecording, etc.)
- `PlanTier` enum (Free, Trial, Starter, Professional, Enterprise)

**File Locations:**
- `backend/src/Modules/Tenancy/Domain/Plan/Plan.cs`
- `backend/src/Modules/Tenancy/Domain/Plan/PlanLimits.cs`

**Acceptance:**
- Plan models correctly
- Limits enforced

---

### T205: RetentionPolicy Entity [P]
**User Story:** US-2.6
**Priority:** P2
**Effort:** 3 pts
**Dependencies:** T202

Implement retention policy configuration.

**Deliverables:**
- `RetentionPolicy` entity (TenantId, RecordingRetentionDays, ChatMessageRetentionDays, FileRetentionDays, AuditLogRetentionDays)
- Min audit log: 2555 days (7 years)

**File Locations:**
- `backend/src/Modules/Tenancy/Domain/Retention/RetentionPolicy.cs`

**Acceptance:**
- Entity models correctly
- Min retention enforced

---

### T206: FeatureToggle Entity [P]
**User Story:** US-2.7
**Priority:** P2
**Effort:** 3 pts
**Dependencies:** T202

Implement feature toggle configuration.

**Deliverables:**
- `FeatureToggle` entity (FeatureName, IsEnabled, Scope, TenantOverrides, CanaryPercentage)
- `FeatureToggleScope` enum (Global, PerTenant, PerUser, PerRegion, Canary)

**File Locations:**
- `backend/src/Modules/Tenancy/Domain/Features/FeatureToggle.cs`

**Acceptance:**
- Entity models toggle scopes
- Database queryable

---

## Phase 2: Tenant Management

### T207: Tenant Creation Command & Handler [P]
**User Story:** US-2.1
**Priority:** P1
**Effort:** 8 pts
**Dependencies:** T202

Implement tenant creation with onboarding.

**Deliverables:**
- `CreateTenantCommand` & handler
- Slug generation and uniqueness check
- Assign creator as Owner
- Assign default Trial plan
- Publish `TenantCreated` event

**File Locations:**
- `backend/src/Modules/Tenancy/Application/Commands/CreateTenantCommand.cs`

**Acceptance:**
- Tenant created with correct slug
- Creator is Owner
- Default plan assigned
- Event published

---

### T208: Tenant Branding Update Command [P]
**User Story:** US-2.2
**Priority:** P2
**Effort:** 8 pts
**Dependencies:** T207

Implement branding customization.

**Deliverables:**
- `UpdateTenantBrandingCommand` & handler
- Logo upload to MinIO (with resizing)
- Color validation (hex format)
- Custom domain validation (DNS)
- CDN caching headers

**File Locations:**
- `backend/src/Modules/Tenancy/Application/Commands/UpdateTenantBrandingCommand.cs`
- `backend/src/Modules/Tenancy/Application/Services/BrandingService.cs`
- `backend/src/Modules/Tenancy/Infrastructure/Services/MinIoService.cs`

**Acceptance:**
- Logo uploaded and resized
- Colors validated
- Domain checked for uniqueness
- Images served with caching

---

### T209: Tenant Membership Invite Command [P]
**User Story:** US-2.3
**Priority:** P1
**Effort:** 8 pts
**Dependencies:** T203

Implement member invitation.

**Deliverables:**
- `InviteTenantMemberCommand` & handler
- Invite token generation
- Email sending
- Token validation on join
- Status transitions (Pending → Active)

**File Locations:**
- `backend/src/Modules/Tenancy/Application/Commands/InviteTenantMemberCommand.cs`
- `backend/src/Modules/Tenancy/Domain/Membership/TenantInviteToken.cs`

**Acceptance:**
- Invite created and emailed
- Token validates on join
- Membership activated

---

### T210: Tenant Membership Role Management [P]
**User Story:** US-2.3
**Priority:** P1
**Effort:** 5 pts
**Dependencies:** T203

Implement role updates and removal.

**Deliverables:**
- `UpdateTenantMemberRoleCommand` & handler
- `RemoveTenantMemberCommand` & handler
- Enforce: at least one Owner required
- Audit logging

**File Locations:**
- `backend/src/Modules/Tenancy/Application/Commands/UpdateTenantMemberRoleCommand.cs`
- `backend/src/Modules/Tenancy/Application/Commands/RemoveTenantMemberCommand.cs`

**Acceptance:**
- Roles updateable (with validation)
- Member removal works
- Last Owner prevented from removal

---

## Phase 3: Plan Management & Usage Tracking

### T211: Plan Assignment & Enforcement [P]
**User Story:** US-2.4
**Priority:** P1
**Effort:** 8 pts
**Dependencies:** T204

Implement plan assignment and limit checking.

**Deliverables:**
- `AssignPlanCommand` & handler
- `CheckPlanLimitsQuery` for room/recording creation
- Hard limit enforcement
- Clear error messages
- Pro-ration for upgrades

**File Locations:**
- `backend/src/Modules/Tenancy/Application/Commands/AssignPlanCommand.cs`
- `backend/src/Modules/Tenancy/Application/Queries/CheckPlanLimitsQuery.cs`
- `backend/src/Modules/Tenancy/Application/Services/PlanLimitService.cs`

**Acceptance:**
- Plan limits enforced
- Rooms/recordings rejected at limit
- Upgrade pro-rates correctly

---

### T212: Usage Tracking & Reporting [P]
**User Story:** US-2.5
**Priority:** P2
**Effort:** 10 pts
**Dependencies:** T211

Implement usage metrics and dashboard.

**Deliverables:**
- `UsageAggregationJob` - daily aggregation
- `GetTenantUsageQuery` - current usage
- `GetUsageHistoryQuery` - trends
- Redis caching for metrics
- Threshold alerts (95%, 100%)

**File Locations:**
- `backend/src/Modules/Tenancy/Application/BackgroundJobs/UsageAggregationJob.cs`
- `backend/src/Modules/Tenancy/Application/Queries/GetTenantUsageQuery.cs`
- `backend/src/Modules/Tenancy/Application/Services/AlertService.cs`

**Acceptance:**
- Usage tracked daily
- Metrics available in real-time (via cache)
- Alerts sent at thresholds
- History available

---

### T213: Plan Upgrade & Downgrade [P]
**User Story:** US-2.8
**Priority:** P2
**Effort:** 10 pts
**Dependencies:** T211

Implement plan change logic.

**Deliverables:**
- `UpgradePlanCommand` & handler
- `DowngradePlanCommand` & handler
- Pro-ration calculation
- Validation on downgrade (usage check)
- Publish `PlanUpgraded`, `PlanDowngraded` events

**File Locations:**
- `backend/src/Modules/Tenancy/Application/Commands/UpgradePlanCommand.cs`
- `backend/src/Modules/Tenancy/Application/Commands/DowngradePlanCommand.cs`

**Acceptance:**
- Upgrades effective immediately
- Pro-ration calculated correctly
- Downgrades validated
- Events published

---

## Phase 4: Retention Policies

### T214: Retention Policy Configuration [P]
**User Story:** US-2.6
**Priority:** P2
**Effort:** 5 pts
**Dependencies:** T205

Implement retention policy commands.

**Deliverables:**
- `UpdateRetentionPolicyCommand` & handler
- Validation (min 1 day, max 10 years, audit log min 7 years)
- Publish `RetentionPolicyChanged` event

**File Locations:**
- `backend/src/Modules/Tenancy/Application/Commands/UpdateRetentionPolicyCommand.cs`

**Acceptance:**
- Policy updated with validation
- Audit log minimum enforced

---

### T215: Data Lifecycle Management Job [P]
**User Story:** US-2.6
**Priority:** P2
**Effort:** 10 pts
**Dependencies:** T214

Implement retention cleanup with soft-delete then hard-delete.

**Deliverables:**
- `DataLifecycleCleanupJob` - daily at midnight
- Soft-delete (mark as deleted)
- 7-day grace period
- Hard-delete after grace
- Audit logging of deletions
- Restoration capability during grace

**File Locations:**
- `backend/src/Modules/Tenancy/Application/BackgroundJobs/DataLifecycleCleanupJob.cs`

**Acceptance:**
- Job runs daily
- Data soft-deleted on schedule
- Can restore during grace period
- Hard-deletion after grace
- Audit trail preserved

---

## Phase 5: Feature Toggles

### T216: Feature Toggle Management [P]
**User Story:** US-2.7
**Priority:** P3
**Effort:** 8 pts
**Dependencies:** T206

Implement feature toggle administration.

**Deliverables:**
- `CreateFeatureToggleCommand` & handler
- `UpdateFeatureToggleCommand` & handler
- Scope evaluation (tenant, user, region, canary %)
- Redis caching (5 min TTL)
- Admin API (internal)

**File Locations:**
- `backend/src/Modules/Tenancy/Application/Commands/CreateFeatureToggleCommand.cs`
- `backend/src/Modules/Tenancy/Application/Services/FeatureToggleService.cs`

**Acceptance:**
- Toggles created and updated
- Scopes evaluated correctly
- Cached and TTL expires
- Admin API secured

---

### T217: Feature Toggle Middleware [P]
**User Story:** US-2.7
**Priority:** P3
**Effort:** 5 pts
**Dependencies:** T216

Implement feature toggle checking in middleware.

**Deliverables:**
- `FeatureToggleMiddleware` - check at request time
- Return 403 if disabled
- Attribute-based marking on controllers

**File Locations:**
- `backend/src/SharedKernel/Infrastructure/Middleware/FeatureToggleMiddleware.cs`
- `backend/src/SharedKernel/Infrastructure/Attributes/RequiresFeatureAttribute.cs`

**Acceptance:**
- Disabled features return 403
- Enabled features allow access

---

## Phase 6: API Endpoints

### T218: Tenancy API - Tenant Management [P]
**User Story:** US-2.1, US-2.2
**Priority:** P1
**Effort:** 8 pts
**Dependencies:** T207, T208

Create REST endpoints for tenant management.

**Deliverables:**
- `POST /api/tenants` - create
- `GET /api/tenants/{tenantId}` - retrieve
- `PATCH /api/tenants/{tenantId}/branding` - update branding
- DTOs and Swagger docs

**File Locations:**
- `backend/src/Modules/Tenancy/Api/Controllers/TenantsController.cs`

**Acceptance:**
- All endpoints functional
- Swagger docs complete

---

### T219: Tenancy API - Membership & Plans [P]
**User Story:** US-2.3, US-2.4, US-2.8
**Priority:** P1
**Effort:** 8 pts
**Dependencies:** T209, T211, T213

Create REST endpoints for membership and plan management.

**Deliverables:**
- `GET /api/tenants/{tenantId}/members` - list
- `POST /api/tenants/{tenantId}/members/invite` - invite
- `PATCH /api/tenants/{tenantId}/members/{memberId}/role` - update role
- `DELETE /api/tenants/{tenantId}/members/{memberId}` - remove
- `GET /api/tenants/{tenantId}/plan` - get current plan
- `POST /api/tenants/{tenantId}/plan/upgrade` - upgrade
- `POST /api/tenants/{tenantId}/plan/downgrade` - downgrade

**File Locations:**
- `backend/src/Modules/Tenancy/Api/Controllers/MembersController.cs`
- `backend/src/Modules/Tenancy/Api/Controllers/PlansController.cs`

**Acceptance:**
- All endpoints functional and tested

---

### T220: Tenancy API - Usage & Retention [P]
**User Story:** US-2.5, US-2.6
**Priority:** P2
**Effort:** 8 pts
**Dependencies:** T212, T214

Create REST endpoints for usage and retention.

**Deliverables:**
- `GET /api/tenants/{tenantId}/usage` - current usage
- `GET /api/tenants/{tenantId}/usage/history` - trends
- `GET /api/tenants/{tenantId}/retention-policies` - retrieve
- `PATCH /api/tenants/{tenantId}/retention-policies` - update

**File Locations:**
- `backend/src/Modules/Tenancy/Api/Controllers/UsageController.cs`
- `backend/src/Modules/Tenancy/Api/Controllers/RetentionController.cs`

**Acceptance:**
- Endpoints return correct data
- Pagination works

---

### T221: Frontend: Tenant Creation & Settings [P]
**User Story:** US-2.1, US-2.2
**Priority:** P2
**Effort:** 10 pts
**Dependencies:** T218

Create React pages for tenant creation and settings.

**Deliverables:**
- `frontend/src/features/tenancy/pages/CreateTenantPage.tsx`
- `frontend/src/features/tenancy/pages/SettingsPage.tsx`
- Branding editor (logo upload, color picker)
- Member management UI
- Plan display and upgrade UI

**File Locations:**
- `frontend/src/features/tenancy/pages/CreateTenantPage.tsx`
- `frontend/src/features/tenancy/pages/SettingsPage.tsx`

**Acceptance:**
- Tenant created via form
- Branding editable
- Members manageable
- Plan visible and upgradeable

---

## Phase 7: Integration Tests

### T222: Tenancy Integration Tests [P]
**User Story:** All
**Priority:** P1
**Effort:** 13 pts
**Dependencies:** All tasks

Write comprehensive tests.

**Deliverables:**
- Tests for tenant creation, branding, membership
- Tests for plan assignment and limits
- Tests for usage tracking
- Tests for retention cleanup
- Tests for feature toggles

**File Locations:**
- `backend/src/Modules/Tenancy.Tests/Integration/`

**Acceptance:**
- All scenarios tested
- Edge cases covered
- Coverage > 80%

---

## Success Metrics

- Tenants created in < 2 minutes
- Plan limits enforced (room creation blocked at limit)
- Usage dashboard accurate (updated within 5 minutes)
- Retention policies delete data on schedule
- Feature toggles effective within 5 minutes
- All plan changes audited and reconciled
- Multi-tenant isolation enforced (no data leakage)
- Performance: all endpoints < 200ms p95
