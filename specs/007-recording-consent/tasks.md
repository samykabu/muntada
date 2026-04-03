# Epic 7: Recording & Consent Module - Task Breakdown

**Document Version:** 1.0
**Last Updated:** 2026-04-03
**Module:** Recording
**Dependencies:** Epic 3 (Rooms), Epic 4 (Realtime Orchestration), Epic 5 (Moderation)

---

## Phase 1: Setup & Infrastructure

### T001: Create Recording Module Structure
- **Description:** Initialize Recording module directory structure and base configuration
- **Files to Create:**
  - `backend/src/Modules/Recording/Domain/` (entities, value objects, aggregates)
  - `backend/src/Modules/Recording/Application/` (commands, queries, handlers)
  - `backend/src/Modules/Recording/Infrastructure/` (persistence, LiveKit integration, MinIO)
  - `backend/src/Modules/Recording/Api/` (controllers, DTOs)
- **Acceptance:** Module scaffold created with proper folder structure and base DbContext

### T002: Define Recording Domain Models and Entities
- **Description:** Create core Recording domain entities: RecordingSession, RecordingManifest, RecordingArtifact, ConsentEvent
- **Files:**
  - `backend/src/Modules/Recording/Domain/RecordingSession.cs`
  - `backend/src/Modules/Recording/Domain/RecordingManifest.cs`
  - `backend/src/Modules/Recording/Domain/RecordingArtifact.cs`
  - `backend/src/Modules/Recording/Domain/ConsentEvent.cs`
  - `backend/src/Modules/Recording/Domain/RecordingState.cs` (enum)
  - `backend/src/Modules/Recording/Domain/ConsentStatus.cs` (enum)
- **Acceptance:** All entities with proper validation, state enums, and aggregate root pattern

### T003: Create Database Schema and Migrations
- **Description:** Create SQL Server schema for Recording module with all tables and indexes
- **Files:**
  - `backend/src/Modules/Recording/Infrastructure/Persistence/RecordingDbContext.cs`
  - `backend/src/Modules/Recording/Infrastructure/Persistence/Migrations/` (initial schema migration)
- **Acceptance:** Schema created with proper relationships, constraints, and 7-year retention tracking

### T004: Implement LiveKit Integration Service
- **Description:** Create wrapper for LiveKit Egress API for starting/stopping server recordings
- **Files:**
  - `backend/src/Modules/Recording/Infrastructure/LiveKitEgressClient.cs`
  - `backend/src/Modules/Recording/Infrastructure/LiveKitModels.cs`
- **Acceptance:** Service can start/stop egress, validate responses, and handle errors

### T005: Implement MinIO Integration for Recording Storage
- **Description:** Create wrapper for MinIO storage operations for recording artifacts
- **Files:**
  - `backend/src/Modules/Recording/Infrastructure/RecordingStorageService.cs`
  - `backend/src/Modules/Recording/Infrastructure/Strategies/PresignedUrlGenerator.cs`
- **Acceptance:** Can upload, download, delete recordings with pre-signed URLs

### T006: Setup Recording Redis Cache Layer
- **Description:** Initialize Redis operations for active recorder counter and caching
- **Files:**
  - `backend/src/Modules/Recording/Infrastructure/RedisRecordingCache.cs`
- **Acceptance:** Redis connection working, counter atomic operations tested

---

## Phase 2: Core Recording Functionality

### T007: [P] Implement Local Recording Start/Stop API Endpoints [US-7.1]
- **Description:** Create endpoints for participants to register local recording sessions
- **Files:**
  - `backend/src/Modules/Recording/Api/LocalRecordingController.cs`
  - `backend/src/Modules/Recording/Application/Commands/RegisterLocalRecordingStartCommand.cs`
  - `backend/src/Modules/Recording/Application/Commands/RegisterLocalRecordingStopCommand.cs`
  - `backend/src/Modules/Recording/Application/Handlers/RegisterLocalRecordingStartHandler.cs`
  - `backend/src/Modules/Recording/Application/Handlers/RegisterLocalRecordingStopHandler.cs`
- **Acceptance:** Endpoints POST /rooms/{roomId}/recordings/local and POST /rooms/{roomId}/recordings/{sessionId}/stop working with counter updates

### T008: [P] Implement Server Recording Start/Stop API Endpoints [US-7.2]
- **Description:** Create endpoints for moderators to start/stop server-side recordings via LiveKit
- **Files:**
  - `backend/src/Modules/Recording/Api/ServerRecordingController.cs`
  - `backend/src/Modules/Recording/Application/Commands/StartServerRecordingCommand.cs`
  - `backend/src/Modules/Recording/Application/Commands/StopServerRecordingCommand.cs`
  - `backend/src/Modules/Recording/Application/Handlers/StartServerRecordingHandler.cs`
  - `backend/src/Modules/Recording/Application/Handlers/StopServerRecordingHandler.cs`
- **Acceptance:** Endpoints POST /rooms/{roomId}/recordings and POST /rooms/{roomId}/recordings/{sessionId}/stop working with LiveKit integration

### T009: [P] Implement Active Recorder Counter with Redis [US-7.3]
- **Description:** Implement atomic counter tracking active recordings and announcement logic
- **Files:**
  - `backend/src/Modules/Recording/Infrastructure/ActiveRecorderCounterService.cs`
  - `backend/src/Modules/Recording/Application/Events/RecordingCounterChangedEvent.cs`
  - `backend/src/Modules/Recording/Application/Handlers/RecordingCounterTransitionHandler.cs`
- **Acceptance:** Counter increments/decrements atomically, 0→1 and 1→0 transitions trigger announcements via SignalR

### T010: [P] Implement Consent Workflow - Pre-Join Acknowledgement [US-7.4]
- **Description:** Create consent prompt and consent event logging before room join
- **Files:**
  - `backend/src/Modules/Recording/Api/ConsentController.cs`
  - `backend/src/Modules/Recording/Application/Commands/RecordConsentCommand.cs`
  - `backend/src/Modules/Recording/Application/Handlers/RecordConsentHandler.cs`
  - `frontend/src/features/recording/ConsentDialog.tsx`
  - `frontend/src/features/recording/useRecordingConsent.ts`
- **Acceptance:** Dialog shown for rooms with recordingConsent=true, consent event persisted, join blocked until consent given

### T011: [P] Implement Recording Manifest Generation [US-7.5]
- **Description:** Create manifest with metadata when server recording completes
- **Files:**
  - `backend/src/Modules/Recording/Application/Commands/GenerateRecordingManifestCommand.cs`
  - `backend/src/Modules/Recording/Application/Handlers/GenerateRecordingManifestHandler.cs`
  - `backend/src/Modules/Recording/Infrastructure/RecordingManifestGenerator.cs`
- **Acceptance:** Manifest created with duration, participants, file size, checksums on LiveKit completion webhook

### T012: [P] Implement Download Scope Selection [US-7.6]
- **Description:** Create API to set and enforce recording download access control
- **Files:**
  - `backend/src/Modules/Recording/Api/DownloadScopeController.cs`
  - `backend/src/Modules/Recording/Application/Commands/SetDownloadScopeCommand.cs`
  - `backend/src/Modules/Recording/Application/Handlers/SetDownloadScopeHandler.cs`
  - `backend/src/Modules/Recording/Application/Queries/CanDownloadRecordingQuery.cs`
  - `backend/src/Modules/Recording/Application/Handlers/CanDownloadRecordingQueryHandler.cs`
- **Acceptance:** Download scope (all/moderator_only/specific_users) enforced on GET /download endpoint

### T013: [P] Implement Recording State Machine [US-7.8]
- **Description:** Ensure valid RecordingSession state transitions
- **Files:**
  - `backend/src/Modules/Recording/Domain/RecordingStateTransition.cs`
  - `backend/src/Modules/Recording/Domain/Services/RecordingStateValidator.cs`
- **Acceptance:** Invalid transitions blocked, all valid transitions allowed, audit logs invalid attempts

---

## Phase 3: Recording Lifecycle & Retention

### T014: [P] Implement Recording Download Endpoint [US-7.5]
- **Description:** Create download endpoint with pre-signed URL generation and expiry
- **Files:**
  - `backend/src/Modules/Recording/Api/RecordingDownloadController.cs`
  - `backend/src/Modules/Recording/Application/Queries/GetDownloadUrlQuery.cs`
  - `backend/src/Modules/Recording/Application/Handlers/GetDownloadUrlQueryHandler.cs`
  - `frontend/src/features/recording/RecordingDownloadButton.tsx`
- **Acceptance:** Pre-signed URLs generated, valid for 24 hours, download scope enforced, authorization checked

### T015: [P] Implement Recording Retention Policy Cleanup Job [US-7.7]
- **Description:** Create background job for automatic deletion of recordings per retention policy
- **Files:**
  - `backend/src/Modules/Recording/Infrastructure/Jobs/RecordingRetentionCleanupJob.cs`
  - `backend/src/Modules/Recording/Application/Commands/DeleteExpiredRecordingsCommand.cs`
  - `backend/src/Modules/Recording/Application/Handlers/DeleteExpiredRecordingsHandler.cs`
- **Acceptance:** Job runs daily, deletes files > retentionDays, soft-deletes database records, logs audit events

### T016: Implement LiveKit Egress Completion Webhook Handler
- **Description:** Handle webhook from LiveKit when recording completes and file is ready
- **Files:**
  - `backend/src/Modules/Recording/Api/LiveKitWebhookController.cs`
  - `backend/src/Modules/Recording/Application/Commands/ProcessEgressCompletionCommand.cs`
  - `backend/src/Modules/Recording/Application/Handlers/ProcessEgressCompletionHandler.cs`
- **Acceptance:** Webhook received, manifest generated, artifact created, download URL provided, announcements broadcast

### T017: Implement Recording Artifact Integrity Verification
- **Description:** Add SHA256 checksum verification for uploaded recordings
- **Files:**
  - `backend/src/Modules/Recording/Infrastructure/RecordingIntegrityService.cs`
- **Acceptance:** Checksums computed on upload, stored in manifest, verified on download

### T018: Implement Recording Listing and Metadata Query Endpoints
- **Description:** Create endpoints for querying recording history and metadata
- **Files:**
  - `backend/src/Modules/Recording/Api/RecordingQueryController.cs`
  - `backend/src/Modules/Recording/Application/Queries/GetRoomRecordingsQuery.cs`
  - `backend/src/Modules/Recording/Application/Handlers/GetRoomRecordingsQueryHandler.cs`
  - `backend/src/Modules/Recording/Application/Queries/GetRecordingManifestQuery.cs`
  - `backend/src/Modules/Recording/Application/Handlers/GetRecordingManifestQueryHandler.cs`
  - `frontend/src/features/recording/RecordingHistory.tsx`
  - `frontend/src/features/recording/RecordingMetadata.tsx`
- **Acceptance:** List endpoint paginated with filters, manifest endpoint returns full metadata

---

## Phase 4: Frontend & Real-Time Updates

### T019: [P] Implement Recording Indicator and Counter Display [US-7.3]
- **Description:** Create UI components showing recording status and active recorder count
- **Files:**
  - `frontend/src/features/recording/RecordingIndicator.tsx`
  - `frontend/src/features/recording/ActiveRecorderCounter.tsx`
  - `frontend/src/features/recording/useRecordingStatus.ts`
  - `frontend/src/features/recording/recordingSignalRHub.ts`
- **Acceptance:** Indicator shows recording active/inactive, counter displays live count, updates via SignalR

### T020: [P] Implement Recording Controls for Moderators [US-7.2]
- **Description:** Create moderator UI panel for controlling server recording
- **Files:**
  - `frontend/src/features/recording/RecordingControlPanel.tsx`
  - `frontend/src/features/recording/useServerRecording.ts`
  - `frontend/src/features/recording/RecordingStatusDropdown.tsx`
- **Acceptance:** Moderator can start/stop recording, sees status, receives confirmations

### T021: [P] Implement Local Recording Registration UI [US-7.1]
- **Description:** Create UI for participants to register their local recording sessions
- **Files:**
  - `frontend/src/features/recording/LocalRecordingButton.tsx`
  - `frontend/src/features/recording/useLocalRecording.ts`
- **Acceptance:** Button shows local recording status, allows start/stop, sends registration to backend

### T022: [P] Implement Download Scope Management UI [US-7.6]
- **Description:** Create moderator panel for configuring recording download access
- **Files:**
  - `frontend/src/features/recording/DownloadScopeSelector.tsx`
  - `frontend/src/features/recording/useDownloadScope.ts`
  - `frontend/src/features/recording/DownloadScopeModal.tsx`
- **Acceptance:** Dropdown/modal to select scope (all/moderator_only/specific), UI to add/remove allowed users

### T023: [P] Implement Recording Status Transitions UI [US-7.8]
- **Description:** Display recording session state and transitions in UI
- **Files:**
  - `frontend/src/features/recording/RecordingStatusBadge.tsx`
  - `frontend/src/features/recording/RecordingStatusTimeline.tsx`
- **Acceptance:** Shows state (Pending/Active/Stopping/Completed/Failed), updates live

---

## Phase 5: Testing & Documentation

### T024: [P] Write Unit Tests for Recording Domain and Application Layer
- **Description:** Comprehensive unit tests for all commands, handlers, queries, and domain logic
- **Files:**
  - `backend/src/Modules/Recording/Tests/Domain/RecordingSessionTests.cs`
  - `backend/src/Modules/Recording/Tests/Domain/RecordingStateTransitionTests.cs`
  - `backend/src/Modules/Recording/Tests/Application/Commands/StartServerRecordingCommandTests.cs`
  - `backend/src/Modules/Recording/Tests/Application/Handlers/RecordingCounterTransitionHandlerTests.cs`
  - `backend/src/Modules/Recording/Tests/Application/Queries/CanDownloadRecordingQueryHandlerTests.cs`
- **Acceptance:** > 85% code coverage, all scenarios from spec covered

### T025: [P] Write Integration Tests for Recording APIs
- **Description:** Integration tests for full API workflows
- **Files:**
  - `backend/src/Modules/Recording/Tests/Integration/LocalRecordingWorkflowTests.cs`
  - `backend/src/Modules/Recording/Tests/Integration/ServerRecordingWorkflowTests.cs`
  - `backend/src/Modules/Recording/Tests/Integration/ConsentWorkflowTests.cs`
  - `backend/src/Modules/Recording/Tests/Integration/DownloadScopeTests.cs`
- **Acceptance:** All user story scenarios automated, tests pass

### T026: [P] Write Frontend Component Tests
- **Description:** Unit and integration tests for React components
- **Files:**
  - `frontend/src/features/recording/__tests__/RecordingIndicator.test.tsx`
  - `frontend/src/features/recording/__tests__/ConsentDialog.test.tsx`
  - `frontend/src/features/recording/__tests__/RecordingControlPanel.test.tsx`
  - `frontend/src/features/recording/__tests__/DownloadScopeSelector.test.tsx`
- **Acceptance:** > 80% coverage, all user interactions tested

### T027: Create Recording Module Documentation
- **Description:** API documentation, state machine diagrams, integration guides
- **Files:**
  - `docs/modules/recording/README.md`
  - `docs/modules/recording/api.md`
  - `docs/modules/recording/state-machine.md`
  - `docs/modules/recording/retention-policy.md`
  - `docs/modules/recording/INTEGRATION_GUIDE.md`
- **Acceptance:** Documentation complete with examples and diagrams

---

## Checkpoint 1: Recording Sessions & Counter (After T013)
**Criteria:**
- Local and server recording start/stop working
- Active recorder counter tracks state accurately
- Announcements broadcast on 0→1 and 1→0 transitions
- State machine prevents invalid transitions
- All Phase 2 unit tests passing

## Checkpoint 2: Consent & Manifests (After T017)
**Criteria:**
- Consent dialog shown pre-join, logged to database
- Recording manifests generated on completion
- Download scope configured and enforced
- Pre-signed URLs working with 24-hour expiry
- LiveKit webhook integration complete

## Checkpoint 3: Full Feature Complete (After T027)
**Criteria:**
- All 6 user stories implemented (US-7.1 through US-7.8)
- Retention policy job running and deleting old recordings
- Frontend UI complete with live updates
- Integration tests covering all workflows
- Documentation complete

---

## Dependencies Between Tasks

- **T001-T006** (Setup): Must complete before any other tasks
- **T007-T009**: Core recording logic, can proceed in parallel, needed by T014-T015
- **T010**: Depends on Rooms module (Epic 3) for room validation
- **T011-T013**: Depend on T007-T009 for session state, can proceed in parallel
- **T014-T018**: Depend on T007-T013 for recording context
- **T019-T023**: Frontend tasks, depend on T007-T018 for API endpoints
- **T024-T026**: Testing, depend on T007-T023 for implementations
- **T027**: Documentation, depends on all implementations

---

## Success Metrics

- Recording sessions created/deleted within 500ms
- Active recorder counter updated atomically within 100ms
- Announcements broadcast within 1 second of counter transition
- Consent event persisted within 100ms
- Recording manifest generated within 2 seconds of LiveKit webhook
- Pre-signed URLs valid for exactly 24 hours
- Retention cleanup job completes for all recordings within 1 hour daily
- No state machine violations in production (100% audit coverage)

---

**Notes:**
- All recording operations include audit trail entries with timestamps and actor IDs
- Recording module integrates with Epic 10 (Reporting & Audit) for audit events
- MinIO bucket structure: `recordings/{tenantId}/{roomId}/{filename}`
- Redis keys: `room:{roomId}:active_recorders`, `recording:{sessionId}:status`
- SQLServer schema includes indexes on (roomId, createdAt) and (tenantId, deletedAt) for query performance
