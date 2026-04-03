# Epic 11: Admin & Support Module

## Overview
The Admin & Support Module provides platform administrators with tools to manage tenants, intervene in live rooms, remove participants, impersonate users for support, and revoke sessions. All admin actions are justified, fully audited, and visible in audit trails. Impersonation is never silent—admins cannot hide their actions. Emergency revocation enables rapid response to security incidents.

## User Stories

### US-11.1: Platform Tenant Management
**Priority:** P0 (Critical)
**Story Points:** 5

As a platform admin, I want to view, search, and manage all tenants on the platform so that I can oversee tenant health and take corrective actions.

**Acceptance Scenarios:**

**Scenario 1: List All Tenants**
```gherkin
Given I am a platform admin
When I navigate to the Admin console → Tenants
Then I see a paginated table of all tenants with columns:
  - Tenant name
  - Tenant ID
  - Subscription status (Active, Suspended, Cancelled)
  - Created date
  - Last activity date
  - Current concurrent rooms / plan limit
  - Storage used / plan limit
  - OTP sends this month / limit
  - Number of users
  - Payment status (Current, Overdue, Never Charged)
And I can sort by any column or search by tenant name/ID
```

**Scenario 2: View Tenant Details**
```gherkin
Given I am viewing the tenants list
When I click on a tenant
Then I see detailed tenant profile:
  - Tenant name, ID, region
  - Contact email and phone
  - Subscription plan and tier
  - Billing details (current invoice, last payment)
  - Users: count, list of admins
  - Rooms: total created, currently active
  - Storage: used, quota
  - Recorded: minutes recorded, quota
  - Files: uploaded, downloaded, rejected by malware
  - Audit trail: recent actions (last 100 events)
And I can perform actions (suspend, reactivate, delete, change plan)
```

**Scenario 3: Suspend Tenant**
```gherkin
Given I am viewing a tenant's detail page
When I click "Suspend Tenant"
Then a modal appears asking for justification
  And I enter reason (e.g., "Payment overdue 30+ days")
  And I confirm suspension
Then the tenant's subscription transitions to "Suspended"
  And all room creation is blocked
  And existing rooms continue until empty
  And a tenant_suspended audit event is created with justification
  And the admin's user ID, IP, and timestamp are recorded
```

**Scenario 4: Reactivate Tenant**
```gherkin
Given a tenant is in "Suspended" status
When I click "Reactivate Tenant"
Then a modal appears asking for justification
  And I enter reason (e.g., "Payment received")
  And I confirm reactivation
Then the tenant's subscription transitions to "Active"
  And room creation is immediately re-enabled
  And a tenant_reactivated audit event is created
```

**Scenario 5: Permanently Delete Tenant (Hard Delete)**
```gherkin
Given I am viewing a tenant's detail page
When I click "Permanently Delete Tenant"
Then a confirmation modal appears warning "This action is irreversible"
  And I must enter a justification text
  And I must check a confirmation checkbox
  And I must enter my own admin password (re-authentication required for security)
Then the tenant and all associated data are permanently deleted:
  - All users under tenant
  - All rooms
  - All files and recordings
  - All audit events
  And a tenant_permanently_deleted audit event is created before deletion
  And MinIO buckets and database records are purged
```

### US-11.2: Live Room Intervention
**Priority:** P0 (Critical)
**Story Points:** 5

As a platform admin, I want to terminate any live room immediately if there's a security incident or policy violation.

**Acceptance Scenarios:**

**Scenario 1: Terminate Room**
```gherkin
Given I am viewing the operational dashboard
When I see a room flagged as concerning (e.g., suspected abuse)
  And I click "Terminate Room"
Then a modal appears asking for justification
  And I enter reason (e.g., "Policy violation detected")
  And I confirm termination
Then:
  - The room transitions to "Terminated" status
  - All participants are immediately disconnected from LiveKit
  - Participants see message "Room terminated by administrator"
  - Recording (if active) is stopped and marked as terminated
  - A room_terminated audit event is created
  - Email notification sent to room creator and tenant admin
```

**Scenario 2: Terminate Room with Participant Notification**
```gherkin
Given a room is terminated
When participants are disconnected
Then they receive in-app notification: "This room was terminated by a platform administrator."
  And the notification includes the termination reason (if appropriate)
  And the reason is logged in audit trail but may be withheld from user message
```

**Scenario 3: Terminated Room Data Preservation**
```gherkin
Given a room is terminated
When I query the room record
Then the room remains in database with status = "Terminated"
  And terminated_at = termination timestamp
  And terminated_by_admin_id = admin's user ID
  And all associated data (recordings, files, chat) is preserved
  And the room is excluded from active room lists
```

### US-11.3: Participant Removal by Admin
**Priority:** P0 (Critical)
**Story Points:** 3

As a platform admin, I want to remove a specific participant from a room immediately if they are violating policies or engaging in abuse.

**Acceptance Scenarios:**

**Scenario 1: Remove Participant from Room**
```gherkin
Given a room is active
When I navigate to Admin → Room Details → Participants
  And I see participant "Jane Doe" is in the room
  And I click "Remove Participant" on Jane's entry
Then a modal appears asking for justification
  And I enter reason (e.g., "Violating community standards")
  And I confirm removal
Then:
  - The participant is disconnected from LiveKit
  - Participant receives message "You were removed from this room"
  - A participant_removed_by_admin audit event is created
  - participant_removal creates entry in AdminAction table
  - Other room participants do not see the removal (internal admin action)
```

**Scenario 2: Participant Ban (Temporary or Permanent)**
```gherkin
Given a participant is removed from a room
When the removal reason is "Abuse"
  And I select "Ban this participant from the tenant"
Then:
  - A ban record is created with ban_type = "tenant_scope" or "platform_scope"
  - ban_duration = 24 hours (configurable, or permanent)
  - The participant cannot rejoin any rooms for the tenant
  - A participant_banned audit event is created
```

**Scenario 3: Remove Multiple Participants**
```gherkin
Given I am in a room with 10 participants
When I select multiple participants (checkboxes)
  And I click "Remove All Selected"
Then a confirmation modal asks for single justification (applies to all)
  And each participant is removed individually
  And separate audit events are created for each removal
  And the justification is shared across all removal events
```

### US-11.4: Admin Impersonation
**Priority:** P1 (High)
**Story Points:** 8

As a platform admin, I want to impersonate a user within a tenant so that I can provide support, troubleshoot issues, and see what the user sees. Impersonation is never silent and is fully visible in audit trails.

**Acceptance Scenarios:**

**Scenario 1: Initiate Impersonation**
```gherkin
Given I am a platform admin
When I navigate to Admin → Users → [User Details]
  And I click "Impersonate This User"
Then a modal appears requiring:
  - Justification text (e.g., "Helping user troubleshoot room creation")
  - Confirmation checkbox "I understand this will be visible in audit trail"
  And I click "Start Impersonation"
Then:
  - An ImpersonationSession is created with:
    - impersonating_admin_id = my admin user ID
    - impersonated_user_id = target user ID
    - impersonation_reason = my justification text
    - session_start_time = now
    - ip_address = my IP
  - My next request includes special header "X-Impersonating: true"
  - API calls made by me are attributed to impersonated user but marked as impersonated
  - Audit events include: admin_user_id, impersonating_admin_id, impersonation_reason
```

**Scenario 2: Impersonated Session Behavior**
```gherkin
Given I am impersonating a user
When I create a room (as the impersonated user)
Then:
  - The room is created with creator = impersonated user
  - Audit event records: room.created | actor = impersonated_user, but also includes impersonating_admin_id
  - The room appears in impersonated user's dashboard
  - Other users in the room see the room created by the impersonated user (no indication of impersonation)
  - BUT: In admin audit logs, the event is marked as impersonated with admin details visible
```

**Scenario 3: End Impersonation**
```gherkin
Given I am impersonating a user
When I click "End Impersonation" (always visible in UI)
  Or my session expires after 1 hour of inactivity
Then:
  - The ImpersonationSession transitions to status = "Ended"
  - session_end_time = now
  - I am logged back in as myself (admin user)
  - An audit event "impersonation_ended" is created
  - No further actions are attributed to the impersonated user
```

**Scenario 4: Impersonation Visibility in Audit Trail**
```gherkin
Given I am impersonating user "Bob"
When I view the audit trail for Bob
Then every event shows:
  - Primary actor = Bob (impersonated_user_id)
  - Secondary actor = my admin ID (impersonating_admin_id)
  - Impersonation reason
  - Impersonation visible: "YES" or flagged with special marker
And the audit trail makes it absolutely clear that Bob did not perform these actions
```

**Scenario 5: Impersonation Duration Limit**
```gherkin
Given I start impersonation at 14:00 UTC
When 1 hour passes without any API calls from the impersonated account
Then the ImpersonationSession is automatically ended
  And I am prompted to re-authenticate if I try to make another API call
  And an audit event "impersonation_auto_ended_timeout" is created
```

### US-11.5: Emergency Session Revocation
**Priority:** P0 (Critical)
**Story Points:** 3

As a platform admin, I want to revoke all active sessions for a user or entire tenant in case of security incident, compromised credentials, or policy violation.

**Acceptance Scenarios:**

**Scenario 1: Revoke All User Sessions**
```gherkin
Given I identify a user with compromised credentials
When I navigate to Admin → Users → [User Details]
  And I click "Revoke All Sessions"
Then a modal appears asking for justification
  And I enter reason (e.g., "Password compromised, user reports suspected breach")
  And I confirm revocation
Then:
  - All active session tokens for the user are immediately invalidated
  - All WebSocket connections for the user are closed
  - All active room participations are terminated
  - Participant receives message "Your session was revoked"
  - A session_revoked_all audit event is created
  - User must re-authenticate to access platform
```

**Scenario 2: Revoke All Tenant Sessions**
```gherkin
Given a tenant is experiencing a security incident
When I navigate to Admin → Tenants → [Tenant Details]
  And I click "Revoke All Tenant Sessions"
Then a modal appears asking for justification
  And I confirm revocation
Then:
  - All active sessions for all users under this tenant are revoked
  - All active rooms in the tenant are terminated
  - All participants are disconnected
  - A tenant_sessions_revoked audit event is created
  - All users must re-authenticate
```

**Scenario 3: Revocation Audit Trail**
```gherkin
Given all sessions are revoked
When I view the audit trail
Then I see:
  - session_revoked events for each revoked session
  - Justification text for the revocation
  - Admin user ID and timestamp
  - IP address of admin who issued revocation
```

### US-11.6: Admin Console UI
**Priority:** P1 (High)
**Story Points:** 8

As a platform admin, I want a dedicated admin console in the React SPA so that I can perform admin tasks without separate tools.

**Acceptance Scenarios:**

**Scenario 1: Admin Navigation Menu**
```gherkin
Given I am logged in as a platform admin
When I navigate to the app
Then I see a new menu item "Admin Console" in the main navigation
  And clicking it opens an admin-only section with sub-sections:
  - Dashboard (operational metrics)
  - Tenants (tenant management)
  - Users (user management, impersonation)
  - Rooms (live room intervention)
  - Support (tickets, escalations)
  - Audit Logs (search, view, export)
  - System Health (infrastructure status)
```

**Scenario 2: Operational Dashboard Widget**
```gherkin
Given I am viewing the admin dashboard
Then I see real-time widgets:
  - Total active rooms (all tenants)
  - Total active participants
  - Total concurrent users
  - Rooms created today (count)
  - Payment transactions today (count, total SAR)
  - Errors in last hour (count, top 5)
  - System resource usage (CPU, memory, disk)
And each widget is updated via SignalR every 10 seconds
```

**Scenario 3: Quick Actions**
```gherkin
Given I am viewing the admin console
When I see a concerning situation (e.g., spike in errors)
Then I can perform quick actions:
  - Terminate a room (with justification modal)
  - Suspend a tenant (with justification modal)
  - Revoke user sessions (with justification modal)
  - View audit trail for a user/tenant/room
  - Check MinIO/RabbitMQ/Database health
All from the dashboard without navigating away
```

## Functional Requirements

### Admin Authentication & Authorization
1. **Admin Role Enforcement**: Only users with "platform_admin" role (set in identity system) can access admin console. Access enforced at API layer (AuthorizeAttribute) and UI layer (route guards).
2. **Admin Permissions Matrix**: Admin console checks user role for each action (e.g., suspend_tenant, revoke_sessions, impersonate_user). Permissions are declarative, not role-based. Role can have multiple permissions.
3. **Admin Session Security**: Admin sessions have shorter timeout (15 minutes of inactivity) vs. regular sessions (24 hours). Admin re-authentication required for sensitive actions (delete tenant, revoke sessions).

### Justification and Audit Trail
4. **Justification Requirements**: Every admin action (suspend, terminate, remove, impersonate, revoke) requires justification text (min 10 characters, max 1000). No blank justifications.
5. **AdminAction Recording**: Each admin action creates an AdminAction record: action_id, admin_user_id, action_type, resource_id, resource_type, justification, ip_address, user_agent, created_at. Also creates corresponding AuditEvent with event_type = admin.*.
6. **Immutable Action Records**: AdminAction records cannot be updated or deleted. Correction requires new action with explanation.

### Impersonation Security
7. **Impersonation Header**: During impersonation, API requests include X-Impersonating: {impersonated_user_id} header. Backend middleware validates that admin has impersonation session for this user.
8. **Impersonation Audit Visibility**: Every audit event during impersonation includes both impersonated_user_id and impersonating_admin_id. Dashboard and audit export make both IDs visible.
9. **Impersonation Duration Limit**: ImpersonationSession max duration is 1 hour (or 8 hours configurable for enterprise). Inactive for 30 minutes auto-ends session.
10. **Impersonation Logging**: Start and end of every impersonation session is logged. Reason is immutable.

### Session Revocation
11. **Session Token Blacklist**: Revoked session tokens are added to Redis blacklist. Every request validates token against blacklist before processing.
12. **Connection Termination**: All WebSocket connections for revoked users are closed gracefully (close code 4000, reason "Session revoked").
13. **Cascading Effects**: Revoking user sessions also terminates any active rooms the user is participating in (room status = "Terminated").

### Room Intervention
14. **Room Termination Mechanics**: Terminating a room changes room status to "Terminated" and closes all LiveKit connections. Recording is stopped and marked as "terminated".
15. **Participant Removal Mechanics**: Removing a participant closes their LiveKit connection but leaves room active for other participants.

### Key Entities

**AdminAction**
- `action_id` (UUID, PK)
- `admin_user_id` (UUID, FK → User)
- `action_type` (enum: suspend_tenant, reactivate_tenant, delete_tenant, terminate_room, remove_participant, ban_participant, impersonate_user, revoke_user_sessions, revoke_tenant_sessions, emergency_revoke)
- `resource_id` (UUID)
- `resource_type` (enum: Tenant, Room, User, Participant)
- `justification` (string, 1000)
- `metadata` (JSON, nullable)
- `ip_address` (string, 45)
- `user_agent` (string, 1000)
- `created_at_utc` (datetime)

**ImpersonationSession**
- `session_id` (UUID, PK)
- `impersonating_admin_id` (UUID, FK → User)
- `impersonated_user_id` (UUID, FK → User)
- `impersonation_reason` (string, 1000)
- `session_start_time` (datetime)
- `session_end_time` (datetime, nullable)
- `admin_ip_address` (string, 45)
- `status` (enum: Active, Ended, Expired)
- `last_activity_time` (datetime)
- `created_at_utc` (datetime)
- `updated_at_utc` (datetime)

**TenantSuspension**
- `suspension_id` (UUID, PK)
- `tenant_id` (UUID, FK → Tenant)
- `admin_user_id` (UUID, FK → User)
- `suspension_reason` (string, 1000)
- `suspension_timestamp` (datetime)
- `reactivation_timestamp` (datetime, nullable)
- `reactivation_reason` (string, nullable)
- `reactivation_admin_user_id` (UUID, FK → User, nullable)
- `status` (enum: Active, Suspended, Reactivated)
- `created_at_utc` (datetime)
- `updated_at_utc` (datetime)

## Success Criteria

- [ ] Tenant suspension is enforced immediately (room creation blocked within 1 second)
- [ ] Room termination disconnects all participants within 2 seconds
- [ ] Participant removal from room completes within 1 second
- [ ] Impersonation session starts within 500ms and includes proper audit trail
- [ ] Session revocation invalidates tokens and closes connections within 2 seconds
- [ ] All admin actions are logged with justification and cannot be edited or deleted
- [ ] Admin console dashboard loads in < 3 seconds with live data
- [ ] No admin action can be performed without justification text
- [ ] Impersonation is visible in audit trail for 100% of cases
- [ ] Impersonation sessions auto-end after 1 hour or 30 minutes of inactivity

## Edge Cases

1. **Admin Impersonates Admin**: Admin A impersonates Admin B. Admin B's permissions are used (read-only view of console, no ability to take additional admin actions). Audit trail clearly marks this as impersonation.

2. **Impersonated User Performs Admin-Only Action**: User A (non-admin) is impersonated by Admin A. User A attempts to access admin console. Request is denied (user role is checked). Admin cannot escalate impersonated user to admin.

3. **Revocation During Impersonation**: Admin A is impersonating User B. User B's sessions are revoked (not by Admin A). Admin A's impersonation session is also ended (since impersonated user's session is invalid).

4. **Concurrent Terminations**: Admin terminates Room A. Room A has 5 participants. All 5 participants are disconnected simultaneously. No race condition in database state.

5. **Room Terminated Before Recording Finishes**: Recording is in progress. Room is terminated. Recording is stopped immediately. Recording file is marked as terminated.

6. **Justification Text Very Long**: Admin enters 5000 character justification (exceeds 1000 char limit). Form validation rejects with "Justification too long (max 1000 characters)".

7. **Admin Logs Out During Impersonation**: Admin is impersonating User B. Admin logs out (browser tab closed). ImpersonationSession is NOT automatically ended. Admin must explicitly end impersonation before logout. If admin logs back in, impersonation session is still active (if within 1 hour).

8. **Session Revocation Webhook Delivery**: When sessions are revoked, admin requests audit event production. Event is published to audit.queue. If consumer fails, event is retried. Audit trail is prioritized over perfect data consistency.

9. **Tenant Delete Cascades**: Deleting Tenant A deletes all rooms, users, files, audit events, admin actions related to Tenant A. Deletion audit event is created before cascade deletion.

10. **Admin Action Rate Limiting**: An admin repeatedly terminates rooms (10 in 1 minute). No rate limiting in v1, but actions are logged. Operational dashboard shows spike in admin terminations. Manual review recommended.

## Assumptions

1. **Admin Role is Centrally Managed**: Assume platform admin role is assigned in the Identity module. No federated or external admin roles in v1.

2. **Impersonation is Opt-In**: Admins must explicitly start impersonation. No passive shadowing.

3. **Justification Text is Not Encrypted**: Assume justification text is stored in plaintext in database. No encryption for audit performance.

4. **Session Blacklist is In-Memory**: Assume revoked tokens are cached in Redis. Fallback: database query if Redis unavailable.

5. **Admin Actions Cannot Be Undone**: Deletion is permanent. No restore mechanism.

6. **Audit Trail is Read-Only for Admins**: Admins can view audit logs but cannot modify them. Audit integrity is paramount.

7. **Impersonation End Time is Relative**: 1-hour limit and 30-minute inactivity timeout are calculated from server time (NTP-synchronized).

8. **Room Termination is Immediate**: No grace period or warning. Termination command triggers immediate disconnect.

9. **Tenant Deletion Requires Re-Authentication**: Assume admins must re-enter password to delete a tenant (2FA optional). This is not a login, just a verification.

10. **Admin Console is Not Available to Tenant Admins**: Only platform admins (role = platform_admin) access admin console. Tenant admins see different UI (tenant-only dashboard).

## Dependencies

- **Epic 1 (Identity)**: Admin role assignment, authentication, session management.
- **Epic 2 (Tenancy)**: Tenant management, suspension, reactivation tied to Subscription state.
- **Epic 3 (Rooms)**: Room termination requires LiveKit integration and room state transitions.
- **Epic 10 (Reporting & Audit)**: All admin actions produce audit events. Audit trail is central to justification tracking.
- **LiveKit OSS**: Room termination requires API calls to LiveKit to disconnect participants.
- **Redis**: Session token blacklist for revoked sessions.
- **SignalR**: Real-time operational dashboard updates.

---

**Document Version:** 1.0
**Last Updated:** 2026-04-03
**Module Owner:** Platform Operations Team
**Status:** Ready for Implementation
