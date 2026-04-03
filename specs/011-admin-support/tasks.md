# Epic 11: Admin & Support Module - Task Breakdown

**Document Version:** 1.0
**Last Updated:** 2026-04-03
**Module:** Admin
**Dependencies:** Epic 1 (Identity), Epic 2 (Tenancy), Epic 3 (Rooms), Epic 10 (Reporting & Audit)

---

## Phase 1: Setup & Infrastructure

### T001: Create Admin Module Structure
- **Description:** Initialize Admin module directory structure and base configuration
- **Files to Create:**
  - `backend/src/Modules/Admin/Domain/` (entities, value objects)
  - `backend/src/Modules/Admin/Application/` (commands, queries, handlers)
  - `backend/src/Modules/Admin/Infrastructure/` (persistence, integrations)
  - `backend/src/Modules/Admin/Api/` (controllers, DTOs)
- **Acceptance:** Module scaffold created with proper folder structure

### T002: Define Admin Domain Models
- **Description:** Create core domain entities: AdminAction, ImpersonationSession, TenantSuspension
- **Files:**
  - `backend/src/Modules/Admin/Domain/AdminAction.cs`
  - `backend/src/Modules/Admin/Domain/ImpersonationSession.cs`
  - `backend/src/Modules/Admin/Domain/TenantSuspension.cs`
  - `backend/src/Modules/Admin/Domain/Enums/ActionType.cs`
  - `backend/src/Modules/Admin/Domain/Enums/ImpersonationStatus.cs`
- **Acceptance:** All entities with immutability, proper validation, audit trails

### T003: Create Database Schema and Migrations
- **Description:** Create SQL Server schema for Admin module
- **Files:**
  - `backend/src/Modules/Admin/Infrastructure/Persistence/AdminDbContext.cs`
  - `backend/src/Modules/Admin/Infrastructure/Persistence/Migrations/` (initial schema)
- **Acceptance:** Schema with proper relationships, justification text columns, immutability enforcement

### T004: Implement Admin Authorization Middleware
- **Description:** Create middleware to enforce admin role and permissions
- **Files:**
  - `backend/src/SharedKernel/Middleware/AdminAuthorizationMiddleware.cs`
  - `backend/src/Modules/Admin/Infrastructure/AdminPermissionValidator.cs`
- **Acceptance:** Middleware checks platform_admin role, enforces short session timeouts (15 min)

### T005: Implement Session Token Blacklist Service
- **Description:** Create Redis-based blacklist for revoked session tokens
- **Files:**
  - `backend/src/Modules/Admin/Infrastructure/SessionTokenBlacklist.cs`
  - `backend/src/Modules/Admin/Infrastructure/TokenRevocationService.cs`
- **Acceptance:** Blacklist checked on every request, revoked tokens rejected immediately

---

## Phase 2: Tenant Management

### T006: [P] Implement Tenant Listing and Search API [US-11.1]
- **Description:** Create paginated API for listing all tenants with filters
- **Files:**
  - `backend/src/Modules/Admin/Api/TenantManagementController.cs`
  - `backend/src/Modules/Admin/Application/Queries/GetTenantsQuery.cs`
  - `backend/src/Modules/Admin/Application/Handlers/GetTenantsQueryHandler.cs`
- **Acceptance:** Endpoint supports pagination, filtering by name/status, sorting by all columns

### T007: [P] Implement Tenant Details API [US-11.1]
- **Description:** Create endpoint to get detailed tenant profile with audit history
- **Files:**
  - `backend/src/Modules/Admin/Application/Queries/GetTenantDetailsQuery.cs`
  - `backend/src/Modules/Admin/Application/Handlers/GetTenantDetailsQueryHandler.cs`
- **Acceptance:** Returns tenant profile, subscription, billing, users list, room count, storage, audit trail

### T008: [P] Implement Tenant Suspension [US-11.1]
- **Description:** Create workflow to suspend tenant (requires justification)
- **Files:**
  - `backend/src/Modules/Admin/Application/Commands/SuspendTenantCommand.cs`
  - `backend/src/Modules/Admin/Application/Handlers/SuspendTenantHandler.cs`
  - `backend/src/Modules/Admin/Api/TenantSuspensionController.cs`
- **Acceptance:** Requires justification (min 10 chars), subscription suspended, room creation blocked, audit logged

### T009: [P] Implement Tenant Reactivation [US-11.1]
- **Description:** Create workflow to reactivate suspended tenant
- **Files:**
  - `backend/src/Modules/Admin/Application/Commands/ReactivateTenantCommand.cs`
  - `backend/src/Modules/Admin/Application/Handlers/ReactivateTenantHandler.cs`
- **Acceptance:** Requires justification, subscription reactivated, room creation re-enabled, audit logged

### T010: [P] Implement Tenant Deletion (Hard Delete) [US-11.1]
- **Description:** Create workflow for permanent tenant deletion with cascading deletes
- **Files:**
  - `backend/src/Modules/Admin/Application/Commands/DeleteTenantCommand.cs`
  - `backend/src/Modules/Admin/Application/Handlers/DeleteTenantHandler.cs`
  - `backend/src/Modules/Admin/Api/TenantDeletionController.cs`
- **Acceptance:** Requires justification, admin re-authentication, cascades delete all related data, audit logged before deletion

---

## Phase 3: Room Intervention

### T011: [P] Implement Room Termination API [US-11.2]
- **Description:** Create endpoint for admin to terminate any live room
- **Files:**
  - `backend/src/Modules/Admin/Application/Commands/TerminateRoomCommand.cs`
  - `backend/src/Modules/Admin/Application/Handlers/TerminateRoomHandler.cs`
  - `backend/src/Modules/Admin/Api/RoomInterventionController.cs`
  - `backend/src/Modules/Admin/Infrastructure/LiveKitRoomTerminator.cs`
- **Acceptance:** Requires justification, disconnects all participants, stops recording, broadcasts termination message

### T012: [P] Implement Room Participants List API [US-11.2]
- **Description:** Create endpoint to list all participants in a room with roles
- **Files:**
  - `backend/src/Modules/Admin/Application/Queries/GetRoomParticipantsQuery.cs`
  - `backend/src/Modules/Admin/Application/Handlers/GetRoomParticipantsQueryHandler.cs`
- **Acceptance:** Returns participant IDs, names, email, roles, join times

### T013: [P] Implement Participant Removal API [US-11.3]
- **Description:** Create endpoint to remove specific participant from room
- **Files:**
  - `backend/src/Modules/Admin/Application/Commands/RemoveParticipantCommand.cs`
  - `backend/src/Modules/Admin/Application/Handlers/RemoveParticipantHandler.cs`
  - `backend/src/Modules/Admin/Api/ParticipantRemovalController.cs`
- **Acceptance:** Requires justification, disconnects participant from LiveKit, notifies participant, audit logged

### T014: [P] Implement Participant Ban Workflow [US-11.3]
- **Description:** Create workflow to temporarily or permanently ban participant from tenant
- **Files:**
  - `backend/src/Modules/Admin/Application/Commands/BanParticipantCommand.cs`
  - `backend/src/Modules/Admin/Application/Handlers/BanParticipantHandler.cs`
  - `backend/src/Modules/Admin/Domain/ParticipantBan.cs`
- **Acceptance:** Ban created with optional duration (24h or permanent), participant blocked from joining rooms

### T015: [P] Implement Bulk Participant Removal [US-11.3]
- **Description:** Create endpoint to remove multiple participants at once
- **Files:**
  - `backend/src/Modules/Admin/Application/Commands/BulkRemoveParticipantsCommand.cs`
  - `backend/src/Modules/Admin/Application/Handlers/BulkRemoveParticipantsHandler.cs`
- **Acceptance:** Accepts list of participant IDs, single justification, each removal logged separately

---

## Phase 4: Admin Impersonation

### T016: [P] Implement Impersonation Initiation [US-11.4]
- **Description:** Create workflow to start admin impersonation of user
- **Files:**
  - `backend/src/Modules/Admin/Application/Commands/StartImpersonationCommand.cs`
  - `backend/src/Modules/Admin/Application/Handlers/StartImpersonationHandler.cs`
  - `backend/src/Modules/Admin/Api/ImpersonationController.cs`
- **Acceptance:** Requires justification, creates ImpersonationSession, sets expiry (1 hour), audit logged

### T017: [P] Implement Impersonation Header Validation [US-11.4]
- **Description:** Create middleware to validate X-Impersonating header and enforce impersonation rules
- **Files:**
  - `backend/src/SharedKernel/Middleware/ImpersonationMiddleware.cs`
  - `backend/src/Modules/Admin/Infrastructure/ImpersonationValidator.cs`
- **Acceptance:** Validates admin has active impersonation session, enforces impersonation user's permissions (not admin permissions)

### T018: [P] Implement Impersonation Audit Visibility [US-11.4]
- **Description:** Ensure impersonation is visible in all audit events
- **Files:**
  - `backend/src/Modules/Admin/Infrastructure/ImpersonationAuditEnricher.cs`
- **Acceptance:** All audit events during impersonation include both impersonated_user_id and impersonating_admin_id

### T019: [P] Implement Impersonation End Endpoint [US-11.4]
- **Description:** Create endpoint to end impersonation session
- **Files:**
  - `backend/src/Modules/Admin/Application/Commands/EndImpersonationCommand.cs`
  - `backend/src/Modules/Admin/Application/Handlers/EndImpersonationHandler.cs`
- **Acceptance:** Session marked "Ended", admin logs back in, audit event recorded

### T020: [P] Implement Impersonation Auto-Timeout [US-11.4]
- **Description:** Create background job to auto-end impersonation sessions after 1 hour or 30 min inactivity
- **Files:**
  - `backend/src/Modules/Admin/Infrastructure/Jobs/ImpersonationTimeoutJob.cs`
  - `backend/src/Modules/Admin/Application/Commands/ExpireImpersonationCommand.cs`
- **Acceptance:** Job runs every 5 minutes, expires sessions > 1 hour or inactive > 30 min, audit logged

---

## Phase 5: Session Revocation

### T021: [P] Implement User Session Revocation API [US-11.5]
- **Description:** Create endpoint to revoke all sessions for a user
- **Files:**
  - `backend/src/Modules/Admin/Application/Commands/RevokeUserSessionsCommand.cs`
  - `backend/src/Modules/Admin/Application/Handlers/RevokeUserSessionsHandler.cs`
  - `backend/src/Modules/Admin/Api/SessionRevocationController.cs`
- **Acceptance:** Requires justification, all tokens blacklisted, WebSocket connections closed, rooms terminated, audit logged

### T022: [P] Implement Tenant Session Revocation API [US-11.5]
- **Description:** Create endpoint to revoke all sessions for entire tenant
- **Files:**
  - `backend/src/Modules/Admin/Application/Commands/RevokeTenantSessionsCommand.cs`
  - `backend/src/Modules/Admin/Application/Handlers/RevokeTenantSessionsHandler.cs`
- **Acceptance:** Revokes all user sessions in tenant, all rooms terminated, audit logged with escalation flag

### T023: [P] Implement LiveKit Connection Termination [US-11.5]
- **Description:** Create service to close WebSocket connections for revoked users
- **Files:**
  - `backend/src/Modules/Admin/Infrastructure/WebSocketTerminator.cs`
  - `backend/src/Modules/Admin/Infrastructure/LiveKitConnectionTerminator.cs`
- **Acceptance:** Connections closed gracefully with reason code, participant receives disconnect message

---

## Phase 6: Admin Console UI

### T024: [P] Implement Admin Navigation and Layout [US-11.6]
- **Description:** Create admin console page structure and navigation menu
- **Files:**
  - `frontend/src/features/admin/AdminConsole.tsx`
  - `frontend/src/features/admin/AdminNavigation.tsx`
  - `frontend/src/features/admin/AdminLayout.tsx`
  - `frontend/src/shared/navigation/adminMenuItems.ts`
- **Acceptance:** Admin menu visible only to admins, sub-sections accessible, routing working

### T025: [P] Implement Admin Dashboard [US-11.6]
- **Description:** Create operational dashboard with live metrics
- **Files:**
  - `frontend/src/features/admin/AdminDashboard.tsx` (or use from Epic 10)
  - `frontend/src/features/admin/AdminMetricsWidgets.tsx`
  - `frontend/src/features/admin/useAdminDashboard.ts`
- **Acceptance:** Shows active rooms, participants, errors, resource usage, updated via SignalR

### T026: [P] Implement Tenant Management UI [US-11.1]
- **Description:** Create tenant list, search, and detail views
- **Files:**
  - `frontend/src/features/admin/TenantListPage.tsx`
  - `frontend/src/features/admin/TenantDetailsPage.tsx`
  - `frontend/src/features/admin/TenantSearchBar.tsx`
  - `frontend/src/features/admin/TenantActionModals.tsx` (suspend, reactivate, delete)
  - `frontend/src/features/admin/useTenantManagement.ts`
- **Acceptance:** List searchable/filterable, details show all info, modals for actions with justification required

### T027: [P] Implement Room Management UI [US-11.2]
- **Description:** Create room listing and intervention tools
- **Files:**
  - `frontend/src/features/admin/RoomListPage.tsx`
  - `frontend/src/features/admin/RoomParticipantsModal.tsx`
  - `frontend/src/features/admin/ParticipantRemovalModal.tsx`
  - `frontend/src/features/admin/RoomTerminationModal.tsx`
  - `frontend/src/features/admin/useRoomIntervention.ts`
- **Acceptance:** Lists live rooms, shows participants, modals for termination/removal with justification

### T028: [P] Implement User Management UI [US-11.4, US-11.5]
- **Description:** Create user listing and impersonation/revocation tools
- **Files:**
  - `frontend/src/features/admin/UserListPage.tsx`
  - `frontend/src/features/admin/UserDetailsPage.tsx`
  - `frontend/src/features/admin/ImpersonationModal.tsx`
  - `frontend/src/features/admin/SessionRevocationModal.tsx`
  - `frontend/src/features/admin/useAdminUserActions.ts`
- **Acceptance:** User list searchable, details show tenant/roles, modals for impersonation and revocation

### T029: [P] Implement Quick Actions Panel [US-11.6]
- **Description:** Create quick action buttons on dashboard for common operations
- **Files:**
  - `frontend/src/features/admin/QuickActionsPanel.tsx`
  - `frontend/src/features/admin/QuickActionButton.tsx`
- **Acceptance:** Buttons for terminate room, suspend tenant, revoke sessions, all open modals with justification

### T030: [P] Implement Audit Trail Viewer [US-11.6]
- **Description:** Create searchable audit log viewer for admin operations
- **Files:**
  - `frontend/src/features/admin/AuditTrailViewer.tsx`
  - `frontend/src/features/admin/AuditEventTable.tsx`
  - `frontend/src/features/admin/useAuditTrail.ts`
- **Acceptance:** Searchable by user/action/resource, sortable by timestamp, shows all details

---

## Phase 7: Testing & Documentation

### T031: [P] Write Unit Tests for Admin Domain and Application
- **Description:** Comprehensive unit tests for all commands, handlers, permissions
- **Files:**
  - `backend/src/Modules/Admin/Tests/Domain/AdminActionTests.cs`
  - `backend/src/Modules/Admin/Tests/Domain/ImpersonationSessionTests.cs`
  - `backend/src/Modules/Admin/Tests/Application/TenantSuspensionTests.cs`
  - `backend/src/Modules/Admin/Tests/Application/ImpersonationTests.cs`
  - `backend/src/Modules/Admin/Tests/Application/SessionRevocationTests.cs`
- **Acceptance:** > 85% code coverage, all scenarios tested

### T032: [P] Write Integration Tests for Admin APIs
- **Description:** Integration tests for tenant management, room intervention, impersonation
- **Files:**
  - `backend/src/Modules/Admin/Tests/Integration/TenantManagementWorkflowTests.cs`
  - `backend/src/Modules/Admin/Tests/Integration/RoomInterventionTests.cs`
  - `backend/src/Modules/Admin/Tests/Integration/ImpersonationWorkflowTests.cs`
  - `backend/src/Modules/Admin/Tests/Integration/SessionRevocationTests.cs`
- **Acceptance:** Full workflows tested, all edge cases covered

### T033: [P] Write Frontend Component Tests
- **Description:** Unit and integration tests for React components
- **Files:**
  - `frontend/src/features/admin/__tests__/TenantListPage.test.tsx`
  - `frontend/src/features/admin/__tests__/RoomManagementUI.test.tsx`
  - `frontend/src/features/admin/__tests__/ImpersonationModal.test.tsx`
  - `frontend/src/features/admin/__tests__/AdminDashboard.test.tsx`
- **Acceptance:** > 80% coverage, all user interactions tested

### T034: Create Admin Module Documentation
- **Description:** API documentation, security guidelines, operational procedures
- **Files:**
  - `docs/modules/admin/README.md`
  - `docs/modules/admin/api.md`
  - `docs/modules/admin/impersonation-guide.md`
  - `docs/modules/admin/incident-response.md`
  - `docs/modules/admin/audit-trail.md`
  - `docs/modules/admin/SECURITY_GUIDELINES.md`
- **Acceptance:** Complete documentation with examples and security best practices

---

## Checkpoint 1: Tenant & Room Management (After T015)
**Criteria:**
- Tenant listing/details/suspension/reactivation working
- Room termination working
- Participant removal and ban working
- Bulk operations working
- All Phase 2-3 tests passing

## Checkpoint 2: Impersonation & Revocation (After T023)
**Criteria:**
- Impersonation sessions created and validated
- Header enforcement working
- Audit visibility correct
- Auto-timeout job running
- User/tenant session revocation working
- WebSocket termination working

## Checkpoint 3: Admin Console Complete (After T030)
**Criteria:**
- Admin navigation menu working
- Dashboard showing all metrics
- All management UIs implemented
- Quick actions panel functional
- Audit trail viewer working

## Checkpoint 4: Full Feature Complete (After T034)
**Criteria:**
- All 6 user stories implemented (US-11.1 through US-11.6)
- All tests passing > 85% coverage
- Admin console UI complete and responsive
- Security guidelines documented
- Audit trail comprehensive

---

## Dependencies Between Tasks

- **T001-T005** (Setup): Must complete before all other tasks
- **T006-T010**: Tenant management, can proceed in parallel
- **T011-T015**: Room intervention, can proceed in parallel
- **T016-T023**: Impersonation and revocation, some sequential (T016→T017)
- **T024-T030**: Frontend, depend on all backend APIs
- **T031-T034**: Testing/docs, depend on all implementations

---

## Success Metrics

- Tenant suspension enforced within 1 second
- Room termination disconnects all participants within 2 seconds
- Participant removal completes within 1 second
- Impersonation session starts within 500ms
- Session revocation invalidates tokens within 2 seconds
- All admin actions logged with justification (100% audit coverage)
- Admin console loads in < 3 seconds
- Justification required for all actions (0 admin actions without)
- Impersonation visible in audit trail (100% of cases)
- Auto-timeout enforced (no sessions > 1 hour)

---

**Notes:**
- Admin role checked at API layer with [Authorize(Roles = "platform_admin")]
- All admin actions create immutable AdminAction records + AuditEvent
- Justification text: min 10 chars, max 1000 chars, stored in plaintext
- Session timeout: 15 minutes for admin sessions, 24 hours for regular users
- Impersonation timeout: 1 hour or 30 minutes inactivity
- Session token blacklist: Redis with TTL matching original session expiry
- SQLServer indexes: (adminUserId, createdAt) on AdminAction, (tenantId) on TenantSuspension
- All sensitive operations require re-authentication or justification modal
- Tenant deletion cascades: users, rooms, files, recordings, subscriptions, billing records
