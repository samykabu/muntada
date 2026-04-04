# Epic 2: Tenancy & Plans Module

**Version:** 1.0
**Status:** Specification
**Last Updated:** 2026-04-03
**Owner:** Platform / Product

---

## Overview

This epic implements multi-tenancy support and plan management for Muntada. It enables organizations to be created, branded, members to be managed, and plans to be configured with feature limits, retention policies, and feature toggles. This module builds on Identity (Epic 1) to provide organizational context for all subsequent modules.

### Scope

- Tenant creation and onboarding workflow
- Tenant branding (logo, colors, custom subdomain)
- Tenant membership management (invite users, assign roles: Owner, Admin, Member)
- Plan configuration (room limits, participant limits, storage quotas, recording limits)
- Retention policies (configurable per tenant: how long to keep recordings, files, chat, audit logs)
- Feature toggles (per-tenant feature flags for gradual rollout)
- Tenant-level policy settings (guest access policy, recording policy, file sharing policy)
- Plan upgrades/downgrades with proration
- Usage tracking and limit enforcement

### Dependencies

- **Identity Module (Epic 1)** for user accounts, sessions, and authentication
- **Shared Kernel (Epic 0)** for base entity types, ID generation, event publishing
- **SQL Server** for persistent tenant and plan data
- **RabbitMQ** for integration events (TenantCreated, PlanUpgraded, etc.)

---

## User Stories

### US-2.1: Tenant Creation & Onboarding
**Priority:** P1
**Story Points:** 13
**Owner:** Platform / Product

As a user, I want to create a new tenant (organization) so that I can establish a workspace for my team and configure it for our needs.

#### Acceptance Criteria

**Given** I am an authenticated user
**When** I navigate to the onboarding flow
**Then** I see a form asking for:
- Organization name (required, 3-100 characters)
- Organization slug/subdomain (auto-generated from name, editable, unique)
- Industry/use case (optional dropdown)
- Team size (optional)

**Given** I fill in the form
**When** I submit
**Then** the system validates:
- Name is unique within reasonable scope (can be duplicated, slug must be unique)
- Slug contains only lowercase alphanumeric and hyphens
- Slug is not reserved (e.g., "admin", "api", "www")
- Slug is not already taken

**Given** validation passes
**When** the form is submitted
**Then** a Tenant record is created with:
- `Id` (opaque ID, prefix `tnt_`)
- `Name`
- `Slug` (unique, used for subdomain)
- `CreatedBy` (current user ID)
- `Status: Active`
- `BillingStatus: Trial` (default: 14-day free trial)
- `CreatedAt`

**Given** the tenant is created
**When** the user is returned to the dashboard
**Then** they are automatically added as TenantMembership with Role: Owner

**Given** the tenant is created
**When** the onboarding continues
**Then** the user is prompted to:
- Upload a logo (optional)
- Choose brand colors (optional)
- Invite team members (optional)

**Given** the user completes onboarding
**When** they access the tenant dashboard
**Then** they can immediately create rooms and invite others

#### Definition of Done
- Tenant creation endpoint: `POST /api/tenants`
- Tenant retrieval endpoint: `GET /api/tenants/{tenantId}`
- Tenant slug validation (uniqueness, format)
- Auto-assignment of creator as Owner
- Default plan assignment (Trial plan)
- Integration event: `TenantCreated` published
- Unit and integration tests
- Validation of all input fields
- Error handling for duplicate slugs

---

### US-2.2: Tenant Branding & Customization
**Priority:** P2
**Story Points:** 8
**Owner:** Platform / Product

As a tenant owner, I want to customize my organization's branding so that the platform reflects our brand identity to our team and guests.

#### Acceptance Criteria

**Given** I am a tenant owner
**When** I navigate to the branding settings
**Then** I see editable fields for:
- Organization name
- Logo (image upload, max 5MB)
- Primary color (hex color picker)
- Secondary color (hex color picker)
- Custom domain (optional, for subdomain)

**Given** I upload a logo
**When** the upload completes
**Then** the image is:
- Stored in MinIO with a unique key (e.g., `tenants/{tenantId}/logo.png`)
- Resized to standard sizes (32px, 64px, 128px, 256px)
- Made accessible via a public URL with CDN caching

**Given** I set custom colors
**When** I save
**Then** the colors are validated as valid hex format

**Given** I set a custom subdomain
**When** I save
**Then** the system checks:
- Subdomain is unique
- Subdomain is not reserved
- DNS records point to Muntada servers

**Given** I complete branding customization
**When** I navigate to a room as a guest
**Then** the room UI displays:
- Custom logo in the header
- Custom colors in buttons, headers, etc.
- Organization name in page title

#### Definition of Done
- Tenant branding update endpoint: `PATCH /api/tenants/{tenantId}/branding`
- Logo upload with MinIO integration
- Image resizing pipeline (via background job)
- Color validation (hex format)
- Subdomain validation and uniqueness check
- DNS configuration guidance (documentation)
- CDN caching headers for logo
- Unit and integration tests

---

### US-2.3: Tenant Membership & Roles
**Priority:** P1
**Story Points:** 13
**Owner:** Platform / Product

As a tenant owner, I want to manage team members and their roles so that I can control who has access to what.

#### Acceptance Criteria

**Given** I am a tenant owner
**When** I navigate to the members management page
**Then** I see a list of all members with:
- Name, email, role (Owner, Admin, Member)
- Date joined
- Last activity
- Action buttons (Edit role, Remove)

**Given** I want to invite a new member
**When** I click "Invite member"
**Then** a dialog appears asking for:
- Email address (required)
- Role (Owner, Admin, Member) (default: Member)
- Notification message (optional)

**Given** I submit the invite
**When** the system processes it
**Then** a TenantMembership is created with:
- `TenantId`
- `UserId` (or `InvitedEmail` if user doesn't exist)
- `Role` (Owner, Admin, or Member)
- `Status: Pending` (until user accepts invite)
- `InvitedAt`
- `InvitedBy` (current user)

**Given** the invite is created
**When** an email is sent
**Then** the invitee receives an email with:
- Link to accept invite: `/join-tenant?token=...`
- Notification message (if provided)
- Sender's name and email

**Given** the invitee clicks the link
**When** they log in
**Then** the TenantMembership status transitions to Active

**Given** I want to change a member's role
**When** I select a new role in the member list
**Then** the system validates:
- I am Owner or Admin
- I cannot change my own role to non-Owner
- At least one Owner must remain

**Given** validation passes
**When** the role is saved
**Then** the member's access is updated immediately

**Given** I want to remove a member
**When** I click "Remove"
**Then** a confirmation dialog appears: "This user will lose access. Confirm?"

**Given** I confirm removal
**When** the member is removed
**Then** the TenantMembership status transitions to Inactive and the member can no longer access the tenant

#### Definition of Done
- Member list endpoint: `GET /api/tenants/{tenantId}/members`
- Member invite endpoint: `POST /api/tenants/{tenantId}/members/invite`
- Member role update endpoint: `PATCH /api/tenants/{tenantId}/members/{memberId}/role`
- Member removal endpoint: `DELETE /api/tenants/{tenantId}/members/{memberId}`
- Invite token generation and validation
- Email invite with customizable message
- Role validation (at least one Owner required)
- Integration event: `TenantMembershipCreated` published
- Audit logging of membership changes
- Unit and integration tests

---

### US-2.4: Plan Management & Limits
**Priority:** P1
**Story Points:** 21
**Owner:** Product / Billing

As a product team, I want to define plans with different feature limits so that we can offer tiered pricing and resource allocation.

#### Acceptance Criteria

**Given** I am the product team
**When** I define a plan
**Then** I specify:
- Plan name (e.g., "Starter", "Professional", "Enterprise")
- Plan tier (Free, Trial, Paid)
- Limits:
  - `MaxRoomsPerMonth` (e.g., 100)
  - `MaxParticipantsPerRoom` (e.g., 100)
  - `MaxStorageGB` (e.g., 100)
  - `MaxRecordingHoursPerMonth` (e.g., 10)
  - `MaxDataRetentionDays` (e.g., 90)
  - Feature permissions (e.g., `AllowRecording`, `AllowGuestAccess`, `AllowCustomBranding`)

**Given** plans are defined
**When** a tenant is created
**Then** they are assigned a default plan (e.g., Trial plan)

**Given** a tenant has an active plan
**When** they attempt to create a room
**Then** the system checks:
- Current room count < `MaxRoomsPerMonth`
- If limit exceeded, room creation is rejected with error "Room limit reached for this month"

**Given** a room is live
**When** a participant joins
**Then** the system checks:
- Current participant count < `MaxParticipantsPerRoom`
- If limit exceeded, participant is rejected with error "Room is at capacity"

**Given** a recording completes
**When** it's stored
**Then** the system checks:
- Current usage + recording size <= `MaxStorageGB`
- If limit exceeded, recording is rejected with error "Storage limit exceeded. Upgrade your plan."

**Given** a plan defines `AllowRecording: false`
**When** a user attempts to record a room
**Then** the recording button is disabled and an error "Recording is not available in your plan" is shown

**Given** a plan's retention period is 90 days
**When** the background cleanup job runs
**Then** recordings, files, and chat older than 90 days are permanently deleted

#### Definition of Done
- Plan definition schema (database or configuration)
- Plan assignment endpoint: `POST /api/tenants/{tenantId}/plan`
- Plan retrieval endpoint: `GET /api/tenants/{tenantId}/plan`
- Usage tracking and enforcement at room/recording creation
- Limit check logic (reusable utility)
- Storage quota enforcement
- Retention policy enforcement (background job)
- Audit logging of plan changes
- Unit and integration tests

---

### US-2.5: Usage Tracking & Reporting
**Priority:** P2
**Story Points:** 13
**Owner:** Product / Analytics

As a tenant admin, I want to see my usage against plan limits so that I can manage resources and plan upgrades.

#### Acceptance Criteria

**Given** I am a tenant admin
**When** I navigate to the usage dashboard
**Then** I see:
- Room count (current month): X / Y limit
- Participant peak: X
- Storage used: X / Y GB
- Recording hours used: X / Y hours
- Data retention setting

**Given** the dashboard is displayed
**When** I hover over each metric
**Then** I see:
- Current usage
- Limit
- % of quota used
- Visual progress bar (color changes at 80%, 95%, 100%)

**Given** I exceed a limit (e.g., 95% of storage)
**When** the threshold is breached
**Then** a warning notification is sent to all tenant owners

**Given** a limit is exceeded (100%)
**When** the next resource creation is attempted
**Then** the request is rejected and user is prompted to upgrade or contact support

**Given** I want to see historical usage
**When** I click "View history"
**Then** I see a graph showing:
- Daily room count
- Daily storage usage
- Daily recording hours
- Trends over 30 days

#### Definition of Done
- Usage dashboard endpoint: `GET /api/tenants/{tenantId}/usage`
- Usage history endpoint: `GET /api/tenants/{tenantId}/usage/history`
- Usage tracking in background jobs
- Threshold alerts (email notifications)
- GraphQL/REST API for usage metrics
- Unit and integration tests

---

### US-2.6: Retention Policies & Data Lifecycle
**Priority:** P2
**Story Points:** 13
**Owner:** Platform / Compliance

As a compliance officer, I want to define retention policies for my organization so that we can meet legal and operational requirements.

#### Acceptance Criteria

**Given** I am a tenant owner
**When** I navigate to data retention settings
**Then** I see configurable retention periods for:
- Recordings (default: 90 days)
- Chat messages (default: 1 year)
- Files (default: 1 year)
- Audit logs (default: 7 years for compliance)
- User activity logs (default: 1 year)

**Given** I set retention period to 30 days for recordings
**When** I save
**Then** the system validates:
- Retention is between 1 day and max (configurable, e.g., 10 years)
- If less than plan's default, plan is checked for feature flag

**Given** retention period is configured
**When** a background job runs daily at midnight
**Then** it:
- Identifies data older than retention period
- Marks data as "scheduled for deletion" (soft delete)
- After 7-day grace period, permanently deletes data
- Logs deletion in audit trail

**Given** data is scheduled for deletion
**When** a user requests it during grace period
**Then** they can restore it with a button "Restore" in the UI

**Given** permanent deletion occurs
**When** the data is gone
**Then** a record is kept in audit logs (what was deleted, when, by whom)

#### Definition of Done
- Retention policy update endpoint: `PATCH /api/tenants/{tenantId}/retention-policies`
- Retention policy retrieval endpoint: `GET /api/tenants/{tenantId}/retention-policies`
- Data lifecycle management background job
- Grace period implementation (soft delete then hard delete)
- Audit logging of deletions
- Restoration capability during grace period
- Unit and integration tests

---

### US-2.7: Feature Toggles & Gradual Rollout
**Priority:** P3
**Story Points:** 8
**Owner:** Product / Engineering

As a product team, I want to use feature toggles to gradually enable features across tenants so that we can control rollout and gather feedback.

#### Acceptance Criteria

**Given** a new feature is developed (e.g., "LiveKit 3D Visualization")
**When** the feature is deployed
**Then** it's disabled by default via a feature toggle

**Given** I am the product team
**When** I want to enable the feature for select tenants
**Then** I can:
- Enable for specific tenants (list of IDs)
- Enable for specific user roles (Owner, Admin, Member)
- Enable for users in specific regions
- Enable for % of tenants (canary: 5%, then 25%, then 100%)

**Given** a user is part of a tenant
**When** they make a request
**Then** the middleware checks feature flags:
- If feature is disabled, feature UI is hidden or API returns 403

**Given** a feature toggle is enabled for a tenant
**When** the feature is accessed
**Then** the user sees the feature and can use it

**Given** a feature toggle is disabled for a tenant
**When** the user attempts to access the feature
**Then** an error "This feature is not available in your plan or region" is shown

#### Definition of Done
- Feature toggle storage (database or Redis)
- Feature flag check middleware
- Admin API for feature toggle management (internal)
- Feature flag evaluation logic (supports %, users, regions, tenants)
- Audit logging of feature toggle changes
- Documentation with examples
- Unit and integration tests

---

### US-2.8: Plan Upgrades & Downgrades
**Priority:** P2
**Story Points:** 13
**Owner:** Billing / Product

As a tenant owner, I want to upgrade or downgrade my plan so that I can adjust my resource allocation based on needs.

#### Acceptance Criteria

**Given** I am a tenant owner
**When** I navigate to billing settings
**Then** I see:
- Current plan (e.g., "Professional")
- Next billing date
- Current charges this month
- Available plans with feature comparison

**Given** I select a higher-tier plan
**When** I click "Upgrade"
**Then** a confirmation dialog shows:
- New plan features
- Price difference
- Pro-rated charges (if billing period in progress)

**Given** I confirm upgrade
**When** the plan change is processed
**Then** the new plan is immediately active and limits are updated

**Given** I select a lower-tier plan
**When** I click "Downgrade"
**Then** a confirmation dialog shows:
- Warning if current usage exceeds new plan limits
- Downgrade date options (immediate or at next billing cycle)

**Given** I confirm downgrade with "Immediate" option
**When** the plan change is processed
**Then** if current usage exceeds new limits, the user is warned and given options:
- Keep current limit usage (may be over quota)
- Reduce usage (clean up data)
- Cancel downgrade

**Given** a plan change is processed
**When** the integration event is published
**Then** the Billing module receives `PlanUpgraded` or `PlanDowngraded` event

#### Definition of Done
- Plan upgrade endpoint: `POST /api/tenants/{tenantId}/plan/upgrade`
- Plan downgrade endpoint: `POST /api/tenants/{tenantId}/plan/downgrade`
- Plan feature comparison (UI component)
- Pro-ration calculation logic
- Usage validation during downgrade
- Integration event: `PlanUpgraded`, `PlanDowngraded` published
- Audit logging of plan changes
- Unit and integration tests

---

## Functional Requirements

### Tenant Management

**FR-2.1:** Each tenant shall be uniquely identified by a Tenant ID (opaque, prefix `tnt_`). A tenant may also have a unique slug for subdomain usage (e.g., `company.muntada.com`).

**FR-2.2:** A tenant shall have a billing status (Active, Trial, Suspended, Cancelled) managed by the Billing module. The Tenancy module enforces restrictions based on status (e.g., Trial tenants cannot create unlimited rooms).

**FR-2.3:** Tenant data shall be isolated at the SQL Server schema level. Each tenant's data lives in the same module schemas but is filtered via TenantId foreign key and row-level security policies (if enabled).

**FR-2.4:** A tenant shall support custom branding including logo (stored in MinIO), colors (hex), and custom subdomain (DNS must be configured). Branding is optional and defaults to Muntada brand.

### Membership Management

**FR-2.5:** Tenant membership shall support three roles with distinct permissions:
- **Owner:** Full control, can manage members, billing, and delete tenant
- **Admin:** Can manage rooms, members (except ownership), and view billing
- **Member:** Can create and participate in rooms, no admin access

**FR-2.6:** A tenant must have at least one Owner at all times. Ownership cannot be transferred by removing the last Owner. Only the last Owner can be removed if they request account deletion.

**FR-2.7:** User invitations shall be sent via email with a time-limited link (valid for 7 days). Invitations can be resent and revoked before acceptance.

**FR-2.8:** Users can belong to multiple tenants. Each user's role is independent per tenant. Tenant switching is managed via JWT claims or explicit `X-Tenant-ID` header.

### Plan & Limits

**FR-2.9:** A plan defines hard limits on resources: MaxRoomsPerMonth, MaxParticipantsPerRoom, MaxStorageGB, MaxRecordingHoursPerMonth. Exceeding a hard limit prevents resource creation/usage.

**FR-2.10:** Plan tiers are: Free (limited), Trial (14 days, full features), Paid (multiple levels), Enterprise (custom). Plan assignment is immutable until upgrade/downgrade.

**FR-2.11:** Usage is tracked and aggregated daily via background jobs. Usage metrics are available in real-time via cache (Redis). Hard enforcement prevents over-quota usage.

**FR-2.12:** When a plan is upgraded immediately, any overage charges are calculated pro-rata and billed immediately (handled by Billing module). When downgraded, the effective date is configurable (immediate or next billing cycle).

### Retention & Data Lifecycle

**FR-2.13:** Retention policies are configurable per tenant and data type. Allowed ranges: 1 day to 10 years (configurable per data type). Audit logs must retain minimum 7 years (for PDPL compliance).

**FR-2.14:** Data deletion follows a soft-delete-then-hard-delete pattern: (1) scheduled for deletion, (2) 7-day grace period, (3) permanent deletion. Audit trail of deletions is preserved.

**FR-2.15:** Deletion jobs run daily at off-peak hours. Failed deletions are retried with exponential backoff. Deletion errors are alerted to ops team.

### Feature Toggles

**FR-2.16:** Feature toggles are stored in a centralized location (database or Redis). Each toggle can be configured per tenant, per user role, per region, or as a percentage rollout.

**FR-2.17:** Feature toggle checks are enforced in middleware and at the API level. Disabled features return 403 Forbidden with message "Feature not available."

**FR-2.18:** Feature toggle evaluation is cached in Redis for performance. Cache TTL is configurable (default 5 minutes). Toggle changes take effect within cache TTL.

### Integration Events

**FR-2.19:** The following integration events shall be published to RabbitMQ:
- `TenantCreated` (when new tenant is created)
- `TenantMembershipCreated` (when user joins tenant)
- `TenantMembershipRemoved` (when user is removed)
- `PlanAssigned` (when plan is set or changed)
- `UsageLimitExceeded` (when usage hits 95% or 100% of limit)
- `RetentionPolicyChanged` (when retention is updated)
- `FeatureToggleChanged` (when feature is enabled/disabled for tenant)

---

## Key Entities

### Tenant

```csharp
public class Tenant : AggregateRoot<TenantId>
{
    public string Name { get; set; }                       // Organization name
    public string Slug { get; set; }                       // Unique, URL-safe slug
    public TenantBranding Branding { get; set; }           // Logo, colors, etc.
    public TenantStatus Status { get; set; }               // Active, Suspended, Deleted
    public BillingStatus BillingStatus { get; set; }       // Active, Trial, Cancelled
    public DateTime? TrialEndsAt { get; set; }             // Trial expiration
    public UserId CreatedBy { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }
    public ICollection<TenantMembership> Memberships { get; set; }
    public Plan Plan { get; set; }                         // Current plan
    public RetentionPolicy RetentionPolicy { get; set; }
}

public class TenantBranding
{
    public string? LogoUrl { get; set; }                   // MinIO URL
    public string? PrimaryColor { get; set; }              // Hex color
    public string? SecondaryColor { get; set; }            // Hex color
    public string? CustomDomain { get; set; }              // Optional CNAME
}

public enum TenantStatus
{
    Active,
    Suspended,
    Deleted
}

public enum BillingStatus
{
    Active,
    Trial,
    Overdue,
    Cancelled
}
```

### TenantMembership

```csharp
public class TenantMembership : Entity<TenantMembershipId>
{
    public TenantId TenantId { get; set; }
    public UserId UserId { get; set; }
    public TenantRole Role { get; set; }                   // Owner, Admin, Member
    public TenantMembershipStatus Status { get; set; }     // Active, Pending, Inactive
    public DateTime JoinedAt { get; set; }
    public DateTime? InvitedAt { get; set; }
    public UserId? InvitedBy { get; set; }
    public DateTime UpdatedAt { get; set; }
}

public enum TenantRole
{
    Owner,
    Admin,
    Member
}

public enum TenantMembershipStatus
{
    Active,           // Accepted and active
    Pending,          // Invited, awaiting acceptance
    Inactive          // Removed or soft-deleted
}
```

### Plan

```csharp
public class Plan : Entity<PlanId>
{
    public TenantId TenantId { get; set; }
    public string Name { get; set; }                       // "Starter", "Professional", etc.
    public PlanTier Tier { get; set; }                     // Free, Trial, Paid, Enterprise
    public decimal MonthlyPrice { get; set; }              // USD
    public PlanLimits Limits { get; set; }
    public List<string> FeatureFlags { get; set; }         // Enabled features
    public DateTime StartDate { get; set; }
    public DateTime? EndDate { get; set; }                 // Null if active
}

public class PlanLimits
{
    public int MaxRoomsPerMonth { get; set; }              // 0 = unlimited
    public int MaxParticipantsPerRoom { get; set; }
    public int MaxStorageGB { get; set; }
    public int MaxRecordingHoursPerMonth { get; set; }
    public int MaxDataRetentionDays { get; set; }
    public bool AllowRecording { get; set; }
    public bool AllowGuestAccess { get; set; }
    public bool AllowCustomBranding { get; set; }
}

public enum PlanTier
{
    Free,
    Trial,
    Starter,
    Professional,
    Enterprise
}
```

### RetentionPolicy

```csharp
public class RetentionPolicy : Entity<RetentionPolicyId>
{
    public TenantId TenantId { get; set; }
    public int RecordingRetentionDays { get; set; }        // Default 90
    public int ChatMessageRetentionDays { get; set; }      // Default 365
    public int FileRetentionDays { get; set; }             // Default 365
    public int AuditLogRetentionDays { get; set; }         // Min 2555 (7 years)
    public int UserActivityLogRetentionDays { get; set; }  // Default 365
    public DateTime UpdatedAt { get; set; }
}
```

### FeatureToggle

```csharp
public class FeatureToggle : Entity<FeatureToggleId>
{
    public string FeatureName { get; set; }                // Unique name
    public bool IsEnabled { get; set; }
    public FeatureToggleScope Scope { get; set; }          // Global, PerTenant, PerUser, PerRegion
    public Dictionary<string, bool> TenantOverrides { get; set; }  // TenantId -> enabled
    public int CanaryPercentage { get; set; }              // 0-100, % of users in canary
    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }
}

public enum FeatureToggleScope
{
    Global,
    PerTenant,
    PerUser,
    PerRegion,
    Canary
}
```

---

## Success Criteria

- Tenants can be created in < 2 minutes
- Branding is applied consistently across all rooms and guest pages
- Members can be invited and roles enforced on every request
- Plan limits are enforced (room creation blocked at limit)
- Usage dashboard shows accurate metrics updated within 5 minutes
- Retention policies delete old data on schedule with zero data loss
- Feature toggles take effect within 5 minutes of configuration change
- Multi-tenant isolation is enforced (no data leakage between tenants)
- All plan changes are audited and reconciled with billing
- Performance: all plan/usage endpoints respond in < 200ms p95

---

## Edge Cases

1. **Last Owner Removal:** If a user is the only Owner and requests removal, the system prevents it. Only account deletion can remove the last Owner.

2. **Concurrent Plan Change:** If two admins attempt plan change simultaneously, the later one fails with "Plan was just changed. Please refresh."

3. **Over-Quota Room Creation:** If quota is exceeded, room creation fails cleanly. User is prompted to upgrade or delete old rooms.

4. **Mid-Month Plan Upgrade:** Pro-ration calculation: if upgraded mid-month, charge the difference for remaining days at new plan's daily rate.

5. **Grace Period During Deletion:** If data is in grace period and user requests restore, it's immediately available. If grace period expires during restore request, an error occurs.

6. **Retention Policy Change Retroactive:** If retention is reduced from 90 to 30 days, existing data older than 30 days is scheduled for deletion immediately.

7. **Feature Toggle Race Condition:** If feature is disabled while user is using it, in-flight requests complete. Next request is rejected with 403.

8. **Membership Invite Collision:** If user invites an email already pending, the old invite is cancelled and new one is created.

9. **Branding Logo Expiration:** If logo is deleted from MinIO before database record is cleaned, a broken image is returned. 404 is handled gracefully.

10. **Multi-Tenant Query Performance:** If a query doesn't filter by TenantId, it returns no results (fail-safe). No cross-tenant data leakage.

---

## Assumptions

1. **Single Tenant Context:** Each request is scoped to a single tenant (via JWT claim or header). Multi-tenant queries are not performed.

2. **Plan Data:** Plan definitions are bootstrapped on deployment. New plans can be added via admin API (internal, requires code change or config update).

3. **Billing Integration:** Billing module (separate) handles payment processing and billing cycles. Tenancy module only manages plan assignment.

4. **Email Delivery:** Invites are sent via configured SMTP/email service. Delivery is assumed to succeed. Failed sends are retried by email service.

5. **MinIO Availability:** Logo uploads are stored in MinIO. MinIO is assumed available with configured S3 credentials.

6. **Data Consistency:** Background jobs for usage tracking, retention cleanup, and feature toggle cache are assumed to run consistently. Short delays (minutes) are acceptable.

7. **Audit Logging:** All changes to tenants, memberships, plans, and policies are logged via Serilog. Audit logs are retained per retention policy.

8. **SQL Server:** Database schema enforces unique constraints on tenant slug and membership. Row-level security (optional) can enforce tenant isolation at SQL layer.

9. **Redis Cache:** Feature toggles and usage metrics are cached in Redis. Cache invalidation is manual or via TTL expiry.

10. **Compliance:** PDPL audit log retention (7 years minimum) is enforced. Data residency is handled by infrastructure (Epic 0).

---

## Implementation Notes

- **Tenant Isolation:** Use TenantId as a required field in all data tables. Add database-level constraints (foreign keys) to prevent orphaned data.
- **Multi-Tenancy Filtering:** Always filter queries by `CurrentTenantId` from JWT/header. Use a middleware to set this context on every request.
- **Plan Enforcement:** Check plan limits at creation time. Return clear errors: "Limit reached. Upgrade plan or contact support."
- **Usage Tracking:** Implement background jobs that run daily (off-peak) to aggregate usage metrics. Cache results in Redis.
- **Retention Cleanup:** Use a scheduled job (Hangfire, Quartz) that runs daily at midnight. Implement soft-delete-then-hard-delete pattern with logging.
- **Feature Toggles:** Use a library like FeatureManagement (.NET) or LaunchDarkly (cloud). Evaluate on every request for consistency.
- **Testing:** Unit tests for plan enforcement, integration tests for multi-tenant isolation, load tests for quota checks.
- **Documentation:** Provide clear API docs with examples for plan upgrade, membership invite, and retention policy changes.
