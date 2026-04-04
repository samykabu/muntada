# Feature Specification: Tenancy & Plans

**Feature Branch**: `002-tenancy-plans`
**Created**: 2026-04-03
**Status**: Draft
**Input**: User description: "Tenancy and Plans Module - Multi-tenancy support, plan management, membership, retention policies, and feature toggles"

## User Scenarios & Testing *(mandatory)*

### User Story 1 - Tenant Creation & Onboarding (Priority: P1)

An authenticated user creates a new organization (tenant) to establish a workspace for their team. The onboarding flow collects the organization name, generates a unique URL-safe slug for subdomain access, and optionally captures industry and team size. Upon creation, the user automatically becomes the organization Owner, is assigned a 14-day free trial plan, and can immediately begin creating rooms and inviting others.

**Why this priority**: This is the foundational capability. No other tenancy feature works without the ability to create an organization. It unblocks all downstream user stories.

**Independent Test**: Can be fully tested by completing the onboarding form and verifying the organization appears in the dashboard with the creator as Owner and a trial plan active.

**Acceptance Scenarios**:

1. **Given** I am an authenticated user, **When** I submit the tenant creation form with a valid organization name, **Then** a tenant is created with a unique slug, I am assigned as Owner, a 14-day trial plan is activated, and I am redirected to the tenant dashboard.
2. **Given** I am creating a tenant, **When** I enter a slug that is already taken or uses a reserved word (e.g., "admin", "api", "www"), **Then** the system rejects it with a clear error and suggests alternatives.
3. **Given** a tenant is created, **When** I access the dashboard, **Then** I can immediately create rooms and invite team members.
4. **Given** I am creating a tenant, **When** I provide an organization name with invalid characters or length outside 3-100 characters, **Then** validation errors are displayed inline before submission.

---

### User Story 2 - Tenant Membership & Roles (Priority: P1)

A tenant owner manages team members by inviting users via email, assigning roles (Owner, Admin, Member), updating roles, and removing members. Invitations are sent via email with a time-limited link. Role-based access controls ensure Owners have full control, Admins can manage rooms and members, and Members can participate but not administer.

**Why this priority**: Membership management is essential for team collaboration. Without inviting members and controlling access, the tenant is unusable for its core purpose.

**Independent Test**: Can be fully tested by inviting a user via email, verifying they receive the invite, accepting it, and confirming their role grants appropriate access.

**Acceptance Scenarios**:

1. **Given** I am a tenant Owner, **When** I invite a user by email with the role "Member", **Then** the invitee receives an email with a link valid for 7 days, and upon acceptance their membership becomes active with the assigned role.
2. **Given** I am a tenant Owner, **When** I change a member's role from Member to Admin, **Then** their permissions are updated immediately and they can manage rooms.
3. **Given** I am a tenant Owner attempting to remove the only Owner, **When** I submit the removal, **Then** the system prevents it with the error "At least one Owner must remain."
4. **Given** an invite is already pending for an email address, **When** I send a new invite to the same address, **Then** the old invite is cancelled and replaced with the new one.
5. **Given** I am an Admin, **When** I try to change another member's role to Owner, **Then** the system rejects it because only Owners can assign Owner role.

---

### User Story 3 - Plan Management & Limit Enforcement (Priority: P1)

The product team defines tiered plans (Free, Trial, Starter, Professional, Enterprise) with specific resource limits: rooms per month, participants per room, storage capacity, recording hours, and feature permissions. Tenants are assigned plans, and the system enforces hard limits at resource creation time, preventing usage beyond quota with clear upgrade prompts.

**Why this priority**: Plan limits define the business model and revenue structure. Enforcement ensures fair resource allocation and drives upgrade conversions.

**Independent Test**: Can be fully tested by assigning a plan to a tenant, consuming resources up to the limit, and verifying that exceeding the limit is blocked with an appropriate message.

**Acceptance Scenarios**:

1. **Given** a tenant is on the Starter plan with a limit of 10 rooms per month, **When** they attempt to create an 11th room, **Then** room creation is rejected with "Room limit reached for this month. Upgrade your plan or contact support."
2. **Given** a room has reached its participant limit, **When** an additional participant attempts to join, **Then** they are rejected with "Room is at capacity."
3. **Given** a plan does not allow recording, **When** a user attempts to record, **Then** the recording option is disabled and a message says "Recording is not available in your plan."
4. **Given** a tenant's storage usage exceeds the plan quota, **When** a recording completes, **Then** storage is rejected with "Storage limit exceeded. Upgrade your plan."

---

### User Story 4 - Plan Upgrades & Downgrades (Priority: P2)

A tenant owner can upgrade or downgrade their plan from the billing settings. Upgrades take effect immediately with pro-rated charges. Downgrades can be immediate or scheduled for the next billing cycle. If a downgrade would reduce limits below current usage, the user is warned and given options to reduce usage, proceed over-quota, or cancel.

**Why this priority**: Plan changes are essential for revenue growth and customer flexibility, but the system can function on a fixed plan initially.

**Independent Test**: Can be fully tested by upgrading a plan and verifying new limits apply immediately, then downgrading and confirming the warning flow when usage exceeds new limits.

**Acceptance Scenarios**:

1. **Given** I am on the Starter plan, **When** I upgrade to Professional mid-billing-cycle, **Then** the new limits apply immediately and I am charged a pro-rated amount for the remaining days.
2. **Given** I am on Professional and using 80GB of 100GB storage, **When** I downgrade to Starter with 50GB limit, **Then** I see a warning that current usage exceeds the new limit and I can choose to proceed, reduce usage, or cancel.
3. **Given** two admins attempt a plan change simultaneously, **When** the second request arrives, **Then** it fails with "Plan was just changed. Please refresh."

---

### User Story 5 - Usage Tracking & Reporting (Priority: P2)

A tenant admin views a usage dashboard showing current consumption against plan limits: room count, participant peaks, storage used, recording hours, and data retention settings. Visual progress bars change color at 80%, 95%, and 100% thresholds. Alerts are sent to tenant owners when usage approaches or exceeds limits. Historical usage trends over 30 days are available.

**Why this priority**: Usage visibility drives informed upgrade decisions and prevents surprise limit blocks, but the core system works without a dashboard.

**Independent Test**: Can be fully tested by consuming resources and verifying the dashboard displays accurate metrics, progress bars reflect correct percentages, and alerts fire at thresholds.

**Acceptance Scenarios**:

1. **Given** I am a tenant admin, **When** I view the usage dashboard, **Then** I see current usage vs. limits for all resource types with percentage indicators and colored progress bars.
2. **Given** storage usage reaches 95%, **When** the threshold is breached, **Then** all tenant owners receive a warning notification.
3. **Given** I click "View history", **When** the history loads, **Then** I see daily trends for rooms, storage, and recording hours over the past 30 days.

---

### User Story 6 - Retention Policies & Data Lifecycle (Priority: P2)

A tenant owner configures retention periods for different data types: recordings, chat messages, files, audit logs, and user activity logs. The system enforces minimum retention for audit logs (7 years for compliance). Data past its retention period is soft-deleted with a 7-day grace period for restoration, then permanently deleted with an audit trail preserved.

**Why this priority**: Retention policies are critical for compliance and data governance, but default retention periods allow the system to function without custom configuration initially.

**Independent Test**: Can be fully tested by setting a short retention period, waiting for the cleanup cycle, verifying data is soft-deleted, restoring within grace period, and confirming permanent deletion after grace.

**Acceptance Scenarios**:

1. **Given** I set recording retention to 30 days, **When** a recording is 31 days old and the cleanup cycle runs, **Then** it is marked as scheduled for deletion and enters a 7-day grace period.
2. **Given** data is in its grace period, **When** I click "Restore", **Then** the data is immediately available again.
3. **Given** the grace period expires, **When** permanent deletion occurs, **Then** the data is removed and an audit record is preserved documenting what was deleted, when, and by whom.
4. **Given** I attempt to set audit log retention below 7 years, **When** I save, **Then** the system rejects it with "Audit log retention must be at least 7 years for compliance."
5. **Given** retention is reduced from 90 to 30 days, **When** the policy is saved, **Then** existing data older than 30 days is immediately scheduled for deletion.

---

### User Story 7 - Tenant Branding & Customization (Priority: P2)

A tenant owner customizes the organization's visual identity: uploading a logo, setting brand colors, and optionally configuring a custom subdomain. The branding is displayed consistently across all rooms and guest-facing pages, reflecting the organization's identity.

**Why this priority**: Branding enhances the professional appearance and white-label experience, but the platform functions fully with default branding.

**Independent Test**: Can be fully tested by uploading a logo, setting colors, and verifying they appear on the room UI and guest pages.

**Acceptance Scenarios**:

1. **Given** I am a tenant owner, **When** I upload a logo (max 5MB image), **Then** it is stored, resized to standard sizes (32px, 64px, 128px, 256px), and displayed in the header across all tenant pages.
2. **Given** I set primary and secondary colors, **When** I save, **Then** the colors are validated as hex format and applied to buttons, headers, and UI accents.
3. **Given** I configure a custom subdomain, **When** I save, **Then** the system validates uniqueness and that it is not a reserved word.

---

### User Story 8 - Feature Toggles & Gradual Rollout (Priority: P3)

The product team controls feature availability per tenant using feature toggles. Toggles support multiple scopes: specific tenants, user roles, geographic regions, and percentage-based canary rollouts. Disabled features are hidden from the UI and return appropriate errors from the API. Toggle changes take effect within minutes.

**Why this priority**: Feature toggles enable safe rollouts and A/B testing, but all features can launch fully enabled without this capability initially.

**Independent Test**: Can be fully tested by creating a toggle, enabling it for a specific tenant, and verifying the feature is accessible for that tenant but blocked for others.

**Acceptance Scenarios**:

1. **Given** a new feature is deployed with a toggle set to disabled, **When** a user in a non-enabled tenant requests the feature, **Then** the UI hides the feature and the API returns "This feature is not available in your plan or region."
2. **Given** a feature toggle is enabled for a specific tenant, **When** a user in that tenant accesses the feature, **Then** it works normally.
3. **Given** a feature is enabled at 5% canary, **When** requests arrive, **Then** approximately 5% of tenants see the feature enabled.
4. **Given** a feature is disabled while a user is actively using it, **When** their current request completes, **Then** the next request is rejected gracefully.

---

### Edge Cases

- **Last Owner removal**: System prevents removing the sole Owner. Only full account deletion can remove the last Owner.
- **Concurrent plan changes**: Second simultaneous plan change request fails with a conflict error prompting refresh.
- **Over-quota on downgrade**: Users are warned and given options when downgrading would put them over new limits.
- **Mid-month upgrade pro-ration**: Charges are calculated for remaining days at the new plan's daily rate.
- **Grace period race condition**: If grace period expires during a restore request, an error is returned and the user is informed.
- **Retroactive retention reduction**: Reducing retention from 90 to 30 days immediately schedules older data for deletion.
- **Feature toggle in-flight requests**: In-flight requests complete normally; only subsequent requests are blocked.
- **Duplicate invite**: Re-inviting the same email cancels the previous pending invite.
- **Broken logo reference**: If the logo file is removed before the database record is cleaned, the system gracefully handles the 404.
- **Cross-tenant data isolation**: Queries missing tenant context return no results as a fail-safe rather than leaking data.
- **Trial expiration with over-quota usage**: When a trial tenant is auto-downgraded to Free and their current usage exceeds Free tier limits (e.g., storage), existing data is preserved but new resource creation is blocked until usage is reduced or plan is upgraded.
- **Suspended tenant access**: Members of a suspended tenant can view existing rooms, recordings, files, and chat history (read-only). All creation operations (rooms, recordings, file uploads, invites) are blocked until suspension is resolved.

## Clarifications

### Session 2026-04-04

- Q: What happens when a tenant's 14-day trial expires? → A: Tenant is automatically downgraded to Free tier; all data preserved but limits reduced to Free plan levels.
- Q: Is there a limit on how many tenants a single user can belong to? → A: Maximum 10 tenants per user (configurable, can be raised for enterprise).
- Q: What access do members have when a tenant is suspended? → A: Read-only — members can view existing data but cannot create rooms, recordings, or upload files.

## Requirements *(mandatory)*

### Functional Requirements

- **FR-001**: System MUST allow authenticated users to create tenants with a unique name and URL-safe slug, automatically assigning the creator as Owner with a default trial plan.
- **FR-002**: System MUST enforce slug uniqueness and reject reserved words (admin, api, www, etc.) during tenant creation and updates.
- **FR-003**: System MUST support three tenant membership roles (Owner, Admin, Member) with distinct permission levels, where Owners have full control, Admins manage rooms and members, and Members can participate.
- **FR-004**: System MUST ensure at least one Owner exists per tenant at all times, preventing the removal or role-change of the last Owner.
- **FR-005**: System MUST send time-limited invitation emails (valid for 7 days) with accept links, supporting resend and revocation before acceptance.
- **FR-006**: System MUST allow users to belong to up to 10 tenants (configurable limit, can be raised for enterprise) with independent roles per tenant, switching context via explicit tenant selection.
- **FR-007**: System MUST define plans with hard limits on resources: rooms per month, participants per room, storage capacity, recording hours per month, and data retention period.
- **FR-008**: System MUST enforce hard limits at resource creation/usage time, rejecting requests that would exceed quota with clear user-facing error messages and upgrade prompts.
- **FR-009**: System MUST support plan tiers: Free (limited), Trial (14-day full features), Starter, Professional, and Enterprise (custom), with each tier having distinct resource limits and feature permissions.
- **FR-010**: System MUST support immediate plan upgrades with pro-rated billing for the remainder of the billing cycle.
- **FR-011**: System MUST support plan downgrades with options for immediate or next-billing-cycle effective date, warning users when current usage exceeds new plan limits.
- **FR-012**: System MUST track resource usage (rooms, storage, recording hours) and provide real-time metrics via a dashboard with visual progress indicators.
- **FR-013**: System MUST send threshold alert notifications to tenant owners when usage reaches 95% and 100% of any limit.
- **FR-014**: System MUST provide 30-day historical usage trends for rooms, storage, and recording hours.
- **FR-015**: System MUST support configurable retention policies per tenant and data type (recordings, chat, files, audit logs, activity logs), with a minimum of 7 years for audit logs to meet compliance requirements.
- **FR-016**: System MUST implement a soft-delete-then-hard-delete data lifecycle: data is marked for deletion, enters a 7-day grace period (restorable), then is permanently deleted with an audit trail.
- **FR-017**: System MUST support tenant branding: logo upload with automatic resizing, hex color customization, and optional custom subdomain configuration.
- **FR-018**: System MUST support feature toggles with multiple scopes: per-tenant, per-user-role, per-region, and percentage-based canary rollout.
- **FR-019**: System MUST disable features for non-enabled tenants both in UI (hidden) and API (returns appropriate error).
- **FR-020**: System MUST publish integration events for key state changes: tenant created, membership changes, plan changes, usage limit exceeded, retention policy changes, and feature toggle changes.
- **FR-021**: System MUST enforce tenant data isolation, ensuring no cross-tenant data access. Queries without tenant context return no results as a fail-safe.
- **FR-022**: System MUST log all changes to tenants, memberships, plans, policies, and feature toggles in an audit trail.

### Key Entities

- **Tenant**: An organization workspace identified by a unique ID and URL slug. Contains branding settings, billing status (Active, Trial, Overdue, Cancelled), operational status (Active, Suspended, Deleted), and is the root container for all organizational data. When suspended, the tenant enters read-only mode: members can view existing data but cannot create new resources.
- **Tenant Membership**: The relationship between a user and a tenant, defining their role (Owner, Admin, Member) and status (Active, Pending, Inactive). Users can have memberships across multiple tenants.
- **Plan**: The subscription tier assigned to a tenant, defining resource limits (rooms, participants, storage, recording hours), feature permissions (recording, guest access, custom branding), pricing, and validity period.
- **Plan Limits**: A set of resource caps and feature permissions associated with a plan, including maximum rooms per month, participants per room, storage in GB, recording hours per month, and boolean feature flags.
- **Retention Policy**: Per-tenant configuration defining how long each data type is retained before deletion (recordings, chat messages, files, audit logs, user activity logs), with compliance-driven minimums.
- **Feature Toggle**: A named flag controlling feature availability, supporting multiple evaluation scopes (global, per-tenant, per-user, per-region, canary percentage) with centralized management and caching.

## Success Criteria *(mandatory)*

### Measurable Outcomes

- **SC-001**: Users can complete tenant creation and onboarding (name, slug, optional branding) in under 2 minutes.
- **SC-002**: Branding (logo, colors) is displayed consistently across all rooms and guest-facing pages within 1 minute of configuration.
- **SC-003**: Team member invitations are delivered and can be accepted, with role-based access enforced on every subsequent request.
- **SC-004**: Plan limits are enforced at 100% accuracy - resource creation is blocked when limits are reached, with zero false positives (wrongly blocking allowed actions).
- **SC-005**: Usage dashboard displays metrics accurate to within 5 minutes of actual usage, with visual threshold indicators at 80%, 95%, and 100%.
- **SC-006**: Retention policies delete expired data on schedule with zero unintended data loss, and all deletions are recorded in the audit trail.
- **SC-007**: Feature toggles take effect within 5 minutes of configuration change for all affected tenants.
- **SC-008**: Multi-tenant data isolation is absolute - no data from one tenant is ever accessible to another tenant.
- **SC-009**: All plan changes (upgrades, downgrades) are audited and produce correct integration events for downstream billing reconciliation.
- **SC-010**: System supports at least 10,000 concurrent active tenants without degradation in plan enforcement or usage tracking accuracy.

## Assumptions

- **Identity dependency**: The Identity module (Epic 1) provides user authentication, sessions, and JWT-based authorization. Tenant context is derived from JWT claims or explicit request headers.
- **Trial plan defaults**: New tenants start on a 14-day free trial with full feature access. Upon trial expiration, the tenant is automatically downgraded to the Free tier — all data is preserved but resource limits are reduced to Free plan levels. Users see upgrade prompts but are never locked out.
- **Plan definitions are pre-configured**: Plan tiers and their limits are defined by the product team and seeded during deployment. Changes to plan definitions require administrative action.
- **Billing is external**: The Tenancy module manages plan assignment and state; actual payment processing, invoicing, and billing cycles are handled by a separate Billing module.
- **Email delivery**: Invitation emails are sent via the platform's configured email service. Delivery reliability is managed by the email provider.
- **Object storage availability**: Logo and file uploads rely on S3-compatible object storage being available and configured.
- **Background processing**: Daily aggregation of usage metrics, retention cleanup jobs, and feature toggle cache refresh run reliably via the platform's background job infrastructure.
- **Compliance baseline**: Audit log retention minimum of 7 years is based on PDPL (Personal Data Protection Law) requirements. No additional region-specific compliance beyond this is assumed for v1.
- **Single-tenant request context**: Each API request is scoped to exactly one tenant. Multi-tenant queries are not supported.
- **Default retention periods**: Recordings default to 90 days, chat/files to 1 year, audit logs to 7 years, activity logs to 1 year. These can be customized per tenant within allowed ranges.
