# Data Model: Tenancy & Plans

**Branch**: `002-tenancy-plans` | **Date**: 2026-04-03 | **Schema**: `[tenancy]`

## Entity Relationship Overview

```
Tenant (aggregate root)
├── TenantBranding (owned value object)
├── TenantMembership (entity, 1:N)
│   └── TenantInviteToken (value object)
├── TenantPlan (entity, 1:1 active)
│   └── PlanLimits (owned value object)
├── RetentionPolicy (entity, 1:1)
└── TenantUsageSnapshot (entity, 1:N - daily)

PlanDefinition (standalone entity - seed data)
└── PlanLimits (owned value object)

FeatureToggle (standalone aggregate root)
└── FeatureToggleOverride (owned collection)
```

---

## Entities

### Tenant (Aggregate Root)

**Table**: `[tenancy].Tenants`
**ID Prefix**: `tnt_`

| Field | Type | Constraints | Description |
|-------|------|-------------|-------------|
| Id | TenantId (Guid) | PK, opaque `tnt_` | Unique tenant identifier |
| Name | string | Required, 3-100 chars | Organization display name |
| Slug | string | Required, unique, lowercase alphanumeric + hyphens | URL-safe subdomain identifier |
| Status | TenantStatus | Required | Active, Suspended, Deleted |
| BillingStatus | BillingStatus | Required | Active, Trial, Overdue, Cancelled |
| TrialEndsAt | DateTime? | Nullable | Trial expiration date (null if not trial) |
| CreatedBy | UserId | Required, FK to Identity | User who created the tenant |
| CreatedAt | DateTime | Required | Creation timestamp |
| UpdatedAt | DateTime | Required | Last modification timestamp |
| Version | int | Required | Optimistic concurrency (from AggregateRoot) |

**Validation Rules**:
- Slug: `^[a-z0-9][a-z0-9-]*[a-z0-9]$`, 3-63 chars, unique across all tenants
- Slug must NOT be a reserved word: admin, api, www, app, help, support, billing, status, mail, ftp
- Name: 3-100 characters, trimmed whitespace
- At least one Owner membership must exist at all times

**State Transitions**:
```
[Created] → Active (BillingStatus: Trial, TrialEndsAt set to +14 days)
Active (Trial) → Active (Free) (automatic: TrialExpirationJob when TrialEndsAt < now — data preserved, limits reduced)
Active → Suspended (billing overdue or admin action — read-only access: GET/HEAD allowed, mutations blocked)
Suspended → Active (payment resolved or admin action)
Active → Deleted (owner-initiated, soft delete)
Suspended → Deleted (admin-initiated)
```

**Suspension Behavior**: When Status = Suspended, all API requests with mutating HTTP methods (POST, PUT, PATCH, DELETE) are rejected by TenantContextMiddleware with 403 "Tenant suspended". GET and HEAD requests are allowed for read-only data access.

---

### TenantBranding (Value Object)

**Owned by**: Tenant (stored in same table row or separate `[tenancy].TenantBrandings` table)

| Field | Type | Constraints | Description |
|-------|------|-------------|-------------|
| LogoUrl | string? | Nullable, valid URL | MinIO URL for logo image |
| PrimaryColor | string? | Nullable, hex format `#RRGGBB` | Primary brand color |
| SecondaryColor | string? | Nullable, hex format `#RRGGBB` | Secondary brand color |
| CustomDomain | string? | Nullable, unique, valid hostname | Custom subdomain (e.g., company.muntada.com) |

**Validation Rules**:
- Colors: Regex `^#[0-9A-Fa-f]{6}$`
- CustomDomain: Valid hostname, unique across tenants, not reserved
- LogoUrl: Must be a valid MinIO URL (validated at upload time)

---

### TenantMembership (Entity)

**Table**: `[tenancy].TenantMemberships`
**ID Prefix**: `mbr_`

| Field | Type | Constraints | Description |
|-------|------|-------------|-------------|
| Id | TenantMembershipId (Guid) | PK, opaque `mbr_` | Unique membership identifier |
| TenantId | TenantId | Required, FK | Owning tenant |
| UserId | UserId? | Nullable (null until user accepts if invited by email) | FK to Identity user |
| InvitedEmail | string? | Nullable | Email address for pending invites |
| Role | TenantRole | Required | Owner, Admin, Member |
| Status | TenantMembershipStatus | Required | Active, Pending, Inactive |
| JoinedAt | DateTime? | Nullable | Date membership became active |
| InvitedAt | DateTime? | Nullable | Date invite was sent |
| InvitedBy | UserId? | Nullable | Who sent the invite |
| UpdatedAt | DateTime | Required | Last modification timestamp |

**Validation Rules**:
- Unique constraint on (TenantId, UserId) for active memberships
- Unique constraint on (TenantId, InvitedEmail) for pending invites
- At least one membership with Role = Owner must exist per tenant
- Maximum 10 active tenants per user (configurable, validated at invite-acceptance time). If limit exceeded, reject with "Maximum organization limit reached"

**State Transitions**:
```
[Invited] → Pending (invite sent)
Pending → Active (invite accepted — validates user has < 10 active tenants)
Active → Inactive (removed by admin/owner)
```

---

### TenantInviteToken (Value Object)

**Owned by**: TenantMembership (stored in separate `[tenancy].TenantInviteTokens` table)

| Field | Type | Constraints | Description |
|-------|------|-------------|-------------|
| Token | string | Required, unique, 32-byte base64url | Cryptographic invite token |
| MembershipId | TenantMembershipId | Required, FK | Associated membership |
| ExpiresAt | DateTime | Required | Token expiration (7 days from creation) |
| IsUsed | bool | Required, default false | Whether token has been consumed |
| CreatedAt | DateTime | Required | Token creation timestamp |

---

### PlanDefinition (Entity - Seed Data)

**Table**: `[tenancy].PlanDefinitions`

| Field | Type | Constraints | Description |
|-------|------|-------------|-------------|
| Id | PlanDefinitionId (Guid) | PK | Unique plan definition identifier |
| Name | string | Required, unique | Plan display name (Starter, Professional, etc.) |
| Tier | PlanTier | Required | Free, Trial, Starter, Professional, Enterprise |
| MonthlyPriceUsd | decimal | Required, >= 0 | Monthly price in USD |
| IsActive | bool | Required, default true | Whether plan is available for new assignments |
| CreatedAt | DateTime | Required | Creation timestamp |
| UpdatedAt | DateTime | Required | Last modification timestamp |

**Seed Data**:

| Name | Tier | MonthlyPrice | MaxRooms | MaxParticipants | MaxStorage | MaxRecording | AllowRecording | AllowGuests | AllowBranding |
|------|------|-------------|----------|-----------------|------------|--------------|----------------|-------------|---------------|
| Free | Free | 0 | 5 | 10 | 1 | 0 | false | false | false |
| Trial | Trial | 0 | 100 | 100 | 10 | 10 | true | true | true |
| Starter | Starter | 29 | 50 | 50 | 50 | 5 | true | true | false |
| Professional | Professional | 99 | 200 | 100 | 200 | 50 | true | true | true |
| Enterprise | Enterprise | 0 (custom) | 0 (unlimited) | 500 | 1000 | 200 | true | true | true |

---

### PlanLimits (Value Object)

**Owned by**: PlanDefinition and TenantPlan

| Field | Type | Constraints | Description |
|-------|------|-------------|-------------|
| MaxRoomsPerMonth | int | Required, >= 0 (0 = unlimited) | Room creation limit per month |
| MaxParticipantsPerRoom | int | Required, >= 1 | Max participants in a single room |
| MaxStorageGB | int | Required, >= 0 | Storage quota in GB |
| MaxRecordingHoursPerMonth | int | Required, >= 0 | Recording hours per month |
| MaxDataRetentionDays | int | Required, >= 1 | Maximum data retention period |
| AllowRecording | bool | Required | Whether recording feature is available |
| AllowGuestAccess | bool | Required | Whether guest access is available |
| AllowCustomBranding | bool | Required | Whether custom branding is available |

---

### TenantPlan (Entity)

**Table**: `[tenancy].TenantPlans`
**ID Prefix**: `pln_`

| Field | Type | Constraints | Description |
|-------|------|-------------|-------------|
| Id | TenantPlanId (Guid) | PK, opaque `pln_` | Unique plan assignment identifier |
| TenantId | TenantId | Required, FK | Owning tenant |
| PlanDefinitionId | PlanDefinitionId | Required, FK | Referenced plan definition |
| StartDate | DateTime | Required | When this plan became active |
| EndDate | DateTime? | Nullable | When this plan ended (null if current) |
| IsCurrent | bool | Required | Whether this is the active plan |

**Validation Rules**:
- Only one plan with IsCurrent = true per tenant
- EndDate must be after StartDate

---

### RetentionPolicy (Entity)

**Table**: `[tenancy].RetentionPolicies`
**ID Prefix**: `rtn_`

| Field | Type | Constraints | Description |
|-------|------|-------------|-------------|
| Id | RetentionPolicyId (Guid) | PK, opaque `rtn_` | Unique retention policy identifier |
| TenantId | TenantId | Required, FK, unique | One policy per tenant |
| RecordingRetentionDays | int | Required, 1-3650 | Default: 90 |
| ChatMessageRetentionDays | int | Required, 1-3650 | Default: 365 |
| FileRetentionDays | int | Required, 1-3650 | Default: 365 |
| AuditLogRetentionDays | int | Required, >= 2555 (7 years) | Min: 2555 (PDPL compliance) |
| UserActivityLogRetentionDays | int | Required, 1-3650 | Default: 365 |
| UpdatedAt | DateTime | Required | Last modification timestamp |

**Validation Rules**:
- AuditLogRetentionDays must be >= 2555 (7 years PDPL compliance)
- All other retention values: 1 <= value <= 3650 (10 years)

---

### FeatureToggle (Aggregate Root)

**Table**: `[tenancy].FeatureToggles`
**ID Prefix**: `ftg_`

| Field | Type | Constraints | Description |
|-------|------|-------------|-------------|
| Id | FeatureToggleId (Guid) | PK, opaque `ftg_` | Unique toggle identifier |
| FeatureName | string | Required, unique | Feature key name (e.g., "livekit-3d-viz") |
| IsEnabled | bool | Required, default false | Global enabled/disabled state |
| Scope | FeatureToggleScope | Required | Global, PerTenant, PerUser, PerRegion, Canary |
| CanaryPercentage | int | Required, 0-100, default 0 | Percentage of tenants in canary |
| CreatedAt | DateTime | Required | Creation timestamp |
| UpdatedAt | DateTime | Required | Last modification timestamp |

---

### FeatureToggleOverride (Owned by FeatureToggle)

**Table**: `[tenancy].FeatureToggleOverrides`

| Field | Type | Constraints | Description |
|-------|------|-------------|-------------|
| FeatureToggleId | FeatureToggleId | FK | Parent toggle |
| TenantId | TenantId | Required | Tenant this override applies to |
| IsEnabled | bool | Required | Override state for this tenant |

**Validation Rules**:
- Unique constraint on (FeatureToggleId, TenantId)

---

### TenantUsageSnapshot (Entity)

**Table**: `[tenancy].TenantUsageSnapshots`

| Field | Type | Constraints | Description |
|-------|------|-------------|-------------|
| Id | Guid | PK | Unique snapshot identifier |
| TenantId | TenantId | Required, FK | Owning tenant |
| SnapshotDate | DateOnly | Required | Date of snapshot |
| RoomsCreated | int | Required | Rooms created on this date |
| RoomsCreatedMonth | int | Required | Cumulative rooms this billing month |
| PeakParticipants | int | Required | Peak concurrent participants on this date |
| StorageUsedGB | decimal | Required | Total storage used (cumulative) |
| RecordingHoursUsed | decimal | Required | Recording hours used this billing month |
| CreatedAt | DateTime | Required | Snapshot creation timestamp |

**Validation Rules**:
- Unique constraint on (TenantId, SnapshotDate)

---

## Indexes

| Table | Index | Columns | Type |
|-------|-------|---------|------|
| Tenants | IX_Tenants_Slug | Slug | Unique |
| Tenants | IX_Tenants_Status | Status | Non-unique (filtered) |
| TenantMemberships | IX_Memberships_TenantUser | TenantId, UserId | Unique (where Status != Inactive) |
| TenantMemberships | IX_Memberships_TenantEmail | TenantId, InvitedEmail | Unique (where Status = Pending) |
| TenantInviteTokens | IX_InviteTokens_Token | Token | Unique |
| TenantPlans | IX_Plans_TenantCurrent | TenantId, IsCurrent | Unique (where IsCurrent = true) |
| RetentionPolicies | IX_Retention_Tenant | TenantId | Unique |
| FeatureToggles | IX_Toggles_FeatureName | FeatureName | Unique |
| FeatureToggleOverrides | IX_Overrides_ToggleTenant | FeatureToggleId, TenantId | Unique |
| TenantUsageSnapshots | IX_Usage_TenantDate | TenantId, SnapshotDate | Unique |

---

## Enumerations

### TenantStatus
- `Active` (0) — Normal operating state
- `Suspended` (1) — Billing issue or admin action
- `Deleted` (2) — Soft-deleted, pending permanent removal

### BillingStatus
- `Active` (0) — Paid and current
- `Trial` (1) — Free trial period
- `Overdue` (2) — Payment past due
- `Cancelled` (3) — Subscription cancelled

### TenantRole
- `Owner` (0) — Full control including billing and deletion
- `Admin` (1) — Manage rooms and members, view billing
- `Member` (2) — Participate in rooms only

### TenantMembershipStatus
- `Active` (0) — Active membership
- `Pending` (1) — Invited, awaiting acceptance
- `Inactive` (2) — Removed or deactivated

### PlanTier
- `Free` (0)
- `Trial` (1)
- `Starter` (2)
- `Professional` (3)
- `Enterprise` (4)

### FeatureToggleScope
- `Global` (0) — Applies to all tenants
- `PerTenant` (1) — Per-tenant overrides
- `PerUser` (2) — Per-user evaluation
- `PerRegion` (3) — Geographic region
- `Canary` (4) — Percentage-based rollout
