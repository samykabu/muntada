# Tasks: Rooms & Scheduling

**Input**: Design documents from `/specs/003-rooms-scheduling/`
**Prerequisites**: plan.md, spec.md, research.md, data-model.md, contracts/

**Tests**: Included per Constitution (Principles IV, XI) — TDD for state machine transitions, authorization policies, invite token validation. xUnit for unit tests, Playwright for integration/E2E.

**Organization**: Tasks grouped by user story to enable independent implementation and testing.

## Format: `[ID] [P?] [Story] Description`

- **[P]**: Can run in parallel (different files, no dependencies)
- **[Story]**: Which user story this task belongs to (e.g., US1, US2, US3)
- Include exact file paths in descriptions

---

## Phase 1: Setup (Shared Infrastructure)

**Purpose**: Create the Rooms module project structure, database context, and shared domain types

- [ ] T001 Create Rooms module project `backend/src/Modules/Rooms/Rooms.csproj` with dependencies: EF Core, MediatR, FluentValidation, MassTransit, Stateless, Ical.Net, StackExchange.Redis, AWSSDK.S3
- [ ] T002 Create Rooms test project `backend/src/Modules/Rooms.Tests/Rooms.Tests.csproj` with xUnit, Moq dependencies
- [ ] T003 [P] Create RoomsDbContext with `[rooms]` schema in `backend/src/Modules/Rooms/Infrastructure/RoomsDbContext.cs`
- [ ] T004 [P] Create shared domain types: RoomSettings value object in `backend/src/Modules/Rooms/Domain/Template/RoomSettings.cs`
- [ ] T005 [P] Create all ID types: RoomTemplateId, RoomSeriesId, RoomOccurrenceId, RoomInviteId, RoomParticipantStateId, RecordingId in `backend/src/Modules/Rooms/Domain/` (one file per aggregate subfolder)
- [ ] T006 [P] Create all enumerations: RoomSeriesStatus, RoomOccurrenceStatus, RoomInviteStatus, RoomInviteType, ParticipantRole, MediaState, RecordingStatus, RecordingVisibility, TranscriptStatus in `backend/src/Modules/Rooms/Domain/`
- [ ] T007 [P] Create integration events in `backend/src/Modules/Rooms/Domain/Events/RoomsEvents.cs` (all 16 events per data-model.md)
- [ ] T008 [P] Create RoomsLogging high-performance logging in `backend/src/Modules/Rooms/Infrastructure/RoomsLogging.cs`
- [ ] T009 [P] Create RoomsTelemetry OpenTelemetry activity source in `backend/src/Modules/Rooms/Infrastructure/RoomsTelemetry.cs`
- [ ] T010 Register Rooms module in Aspire AppHost `aspire/Muntada.AppHost/AppHost.cs` and API `Program.cs` (DbContext, MediatR, services)

**Checkpoint**: Module compiles, database schema created, shared types available

---

## Phase 2: Foundational (Blocking Prerequisites)

**Purpose**: Core domain aggregates and infrastructure services that ALL user stories depend on

**CRITICAL**: No user story work can begin until this phase is complete

- [ ] T011 Create RoomTemplate aggregate root in `backend/src/Modules/Rooms/Domain/Template/RoomTemplate.cs` with TenantId, Name (immutable), Description, Settings, CreatedBy, timestamps, Version
- [ ] T012 [P] Create RoomSeries aggregate root in `backend/src/Modules/Rooms/Domain/Series/RoomSeries.cs` with TenantId, TemplateId, Title, RecurrenceRule, OrganizerTimeZoneId, StartsAt, EndsAt, Status, timestamps
- [ ] T013 [P] Create RoomOccurrence aggregate root with Stateless state machine in `backend/src/Modules/Rooms/Domain/Occurrence/RoomOccurrence.cs` — states: Draft, Scheduled, Live, Grace, Ended, Archived with all valid transitions and guard clauses
- [ ] T014 [P] Create ModeratorAssignment value object in `backend/src/Modules/Rooms/Domain/Occurrence/ModeratorAssignment.cs`
- [ ] T015 [P] Create RoomInvite entity in `backend/src/Modules/Rooms/Domain/Invite/RoomInvite.cs` with token, status, type, expiry
- [ ] T016 [P] Create RoomParticipantState entity in `backend/src/Modules/Rooms/Domain/Participant/RoomParticipantState.cs` with role, media state, LiveKit ID
- [ ] T017 [P] Create Recording aggregate root with Transcript owned collection in `backend/src/Modules/Rooms/Domain/Recording/Recording.cs`
- [ ] T018 Configure EF Core entity mappings in RoomsDbContext: all entities, owned types (RoomSettings, ModeratorAssignment, Transcript), indexes, enum conversions
- [ ] T019 Generate initial database migration using `dotnet ef migrations add InitialRoomsSchema` (CLI only — do NOT generate migration files via AI)
- [ ] T020 [P] Write unit tests for RoomOccurrence state machine (all valid transitions, all invalid transitions rejected) in `backend/src/Modules/Rooms.Tests/Unit/Domain/RoomOccurrenceStateMachineTests.cs`
- [ ] T021 [P] Write unit tests for RoomTemplate domain logic (immutable name, settings validation) in `backend/src/Modules/Rooms.Tests/Unit/Domain/RoomTemplateTests.cs`

**Checkpoint**: All domain entities defined, state machine tested, database schema ready. User story implementation can begin.

---

## Phase 3: User Story 1 — Create and Reuse Room Templates (Priority: P1) MVP

**Goal**: Admins/Owners can create, list, get, and update room templates with plan limit validation

**Independent Test**: Create a template via API, verify it persists, list templates, update description, verify name is immutable

### Tests for User Story 1

- [ ] T022 [P] [US1] Write unit tests for CreateRoomTemplateCommandHandler (valid creation, duplicate name rejection, plan limit validation) in `backend/src/Modules/Rooms.Tests/Unit/Application/CreateRoomTemplateTests.cs`
- [ ] T023 [P] [US1] Write unit tests for UpdateRoomTemplateCommandHandler (update allowed fields, name immutability enforced) in `backend/src/Modules/Rooms.Tests/Unit/Application/UpdateRoomTemplateTests.cs`

### Implementation for User Story 1

- [ ] T024 [P] [US1] Implement CreateRoomTemplateCommandHandler with FluentValidation (name uniqueness per tenant, max participants <= plan limit, recording gated by plan) in `backend/src/Modules/Rooms/Application/Commands/CreateRoomTemplateCommandHandler.cs`
- [ ] T025 [P] [US1] Implement CreateRoomTemplateValidator in `backend/src/Modules/Rooms/Application/Validators/CreateRoomTemplateValidator.cs`
- [ ] T026 [US1] Implement UpdateRoomTemplateCommandHandler (all fields except name) in `backend/src/Modules/Rooms/Application/Commands/UpdateRoomTemplateCommandHandler.cs`
- [ ] T027 [P] [US1] Implement GetRoomTemplateQueryHandler in `backend/src/Modules/Rooms/Application/Queries/GetRoomTemplateQueryHandler.cs`
- [ ] T028 [P] [US1] Implement ListRoomTemplatesQueryHandler with pagination in `backend/src/Modules/Rooms/Application/Queries/ListRoomTemplatesQueryHandler.cs`
- [ ] T029 [US1] Create TemplatesController with POST, GET (list), GET (single), PATCH endpoints in `backend/src/Modules/Rooms/Api/Controllers/TemplatesController.cs`
- [ ] T030 [P] [US1] Create RoomTemplateDto, CreateRoomTemplateRequest, UpdateRoomTemplateRequest DTOs in `backend/src/Modules/Rooms/Api/Dtos/RoomTemplateDto.cs`
- [ ] T031 [US1] Publish RoomTemplateCreated integration event on successful creation

**Checkpoint**: Templates CRUD fully functional. Admins can create/list/update templates with plan validation.

---

## Phase 4: User Story 2 — Create a One-Off Room (Priority: P1)

**Goal**: Admins/Owners can create a standalone room occurrence with template, scheduled time, timezone, and moderator

**Independent Test**: Create a one-off room via API, verify it appears in occurrence list with status Scheduled, verify moderator is assigned

### Tests for User Story 2

- [ ] T032 [P] [US2] Write unit tests for CreateRoomOccurrenceCommandHandler (one-off creation, moderator required, timezone stored, plan limit validation) in `backend/src/Modules/Rooms.Tests/Unit/Application/CreateRoomOccurrenceTests.cs`

### Implementation for User Story 2

- [ ] T033 [US2] Implement CreateRoomOccurrenceCommandHandler for standalone rooms (inherits template settings, sets moderator, stores timezone, transitions to Scheduled) in `backend/src/Modules/Rooms/Application/Commands/CreateRoomOccurrenceCommandHandler.cs`
- [ ] T034 [P] [US2] Implement CreateRoomOccurrenceValidator in `backend/src/Modules/Rooms/Application/Validators/CreateRoomOccurrenceValidator.cs`
- [ ] T035 [P] [US2] Implement GetRoomOccurrenceQueryHandler in `backend/src/Modules/Rooms/Application/Queries/GetRoomOccurrenceQueryHandler.cs`
- [ ] T036 [P] [US2] Implement ListRoomOccurrencesQueryHandler with date range and status filters in `backend/src/Modules/Rooms/Application/Queries/ListRoomOccurrencesQueryHandler.cs`
- [ ] T037 [US2] Create OccurrencesController with POST, GET (list), GET (single), PATCH endpoints in `backend/src/Modules/Rooms/Api/Controllers/OccurrencesController.cs`
- [ ] T038 [P] [US2] Create RoomOccurrenceDto, CreateRoomOccurrenceRequest, UpdateRoomOccurrenceRequest DTOs in `backend/src/Modules/Rooms/Api/Dtos/RoomOccurrenceDto.cs`
- [ ] T039 [US2] Implement UpdateRoomOccurrenceCommandHandler for single-occurrence overrides (title, settings, cancel) in `backend/src/Modules/Rooms/Application/Commands/UpdateRoomOccurrenceCommandHandler.cs`
- [ ] T040 [US2] Publish RoomScheduled integration event on successful creation

**Checkpoint**: One-off rooms can be created, listed, and updated. Calendar queries work with date range filters.

---

## Phase 5: User Story 3 — Schedule Recurring Room Series (Priority: P1)

**Goal**: Admins/Owners can create recurring room series with iCal RRULE, generating occurrences 30 days ahead, with a background job for ongoing generation

**Independent Test**: Create a series with weekly pattern, verify 30 days of occurrences generated, cancel a single occurrence, verify others unaffected

### Tests for User Story 3

- [ ] T041 [P] [US3] Write unit tests for RecurrenceService (daily, weekly, monthly, custom RRULE patterns, DST handling, invalid RRULE rejection) in `backend/src/Modules/Rooms.Tests/Unit/Application/RecurrenceServiceTests.cs`
- [ ] T042 [P] [US3] Write unit tests for CreateRoomSeriesCommandHandler in `backend/src/Modules/Rooms.Tests/Unit/Application/CreateRoomSeriesTests.cs`

### Implementation for User Story 3

- [ ] T043 [US3] Implement IRecurrenceService interface and RecurrenceService using Ical.Net (RRULE parsing, timezone-aware occurrence generation, DST handling) in `backend/src/Modules/Rooms/Infrastructure/Services/RecurrenceService.cs`
- [ ] T044 [US3] Implement CreateRoomSeriesCommandHandler (validates RRULE, stores timezone, generates initial occurrences 30 days ahead) in `backend/src/Modules/Rooms/Application/Commands/CreateRoomSeriesCommandHandler.cs`
- [ ] T045 [P] [US3] Implement CreateRoomSeriesValidator in `backend/src/Modules/Rooms/Application/Validators/CreateRoomSeriesValidator.cs`
- [ ] T046 [US3] Implement UpdateRoomSeriesCommandHandler (update recurrence, regenerate future occurrences) in `backend/src/Modules/Rooms/Application/Commands/UpdateRoomSeriesCommandHandler.cs`
- [ ] T047 [US3] Implement OccurrenceGenerationJob background job (runs hourly, idempotent, maintains 30-day window) in `backend/src/Modules/Rooms/Application/BackgroundJobs/OccurrenceGenerationJob.cs`
- [ ] T048 [US3] Create SeriesController with POST, GET (list), GET (single), PATCH, POST /end endpoints in `backend/src/Modules/Rooms/Api/Controllers/SeriesController.cs`
- [ ] T049 [P] [US3] Create RoomSeriesDto, CreateRoomSeriesRequest DTOs in `backend/src/Modules/Rooms/Api/Dtos/RoomSeriesDto.cs`
- [ ] T050 [US3] Publish RoomSeriesCreated and RoomOccurrenceGenerated integration events

**Checkpoint**: Recurring series functional. Occurrences auto-generated, single-occurrence overrides work, background job runs.

---

## Phase 6: User Story 4 — Room Lifecycle Management (Priority: P1)

**Goal**: Rooms follow strict state machine: Scheduled → Live → Grace → Ended → Archived with automatic transitions

**Independent Test**: Transition a room through all states: Scheduled → Live (first participant) → Grace (moderator disconnect) → Live (reconnect) → Ended (explicit). Verify invalid transitions rejected.

### Tests for User Story 4

- [ ] T051 [P] [US4] Write unit tests for TransitionRoomStatusCommandHandler (all valid transitions, invalid transitions rejected, concurrent state check) in `backend/src/Modules/Rooms.Tests/Unit/Application/TransitionRoomStatusTests.cs`
- [ ] T052 [P] [US4] Write unit tests for GracePeriodService (start timer, cancel on reconnect, expire to Ended) in `backend/src/Modules/Rooms.Tests/Unit/Application/GracePeriodServiceTests.cs`

### Implementation for User Story 4

- [ ] T053 [US4] Implement TransitionRoomStatusCommandHandler (validates transition via state machine, updates timestamps, publishes lifecycle events) in `backend/src/Modules/Rooms/Application/Commands/TransitionRoomStatusCommandHandler.cs`
- [ ] T054 [US4] Implement IGracePeriodService and GracePeriodService using MassTransit ScheduleSend (schedule delayed message on grace start, cancel on moderator reconnect) in `backend/src/Modules/Rooms/Infrastructure/Services/GracePeriodService.cs`
- [ ] T055 [US4] Implement GracePeriodExpiryJob consumer (processes scheduled grace expiry message, transitions room to Ended) in `backend/src/Modules/Rooms/Application/BackgroundJobs/GracePeriodExpiryJob.cs`
- [ ] T056 [US4] Implement RetentionCleanupJob background job (archives ended rooms per retention policy, deletes expired archived rooms) in `backend/src/Modules/Rooms/Application/BackgroundJobs/RetentionCleanupJob.cs`
- [ ] T057 [US4] Add POST /transition endpoint to OccurrencesController in `backend/src/Modules/Rooms/Api/Controllers/OccurrencesController.cs`
- [ ] T058 [US4] Publish RoomLive, RoomGraceStarted, RoomEnded, RoomArchived integration events on transitions

**Checkpoint**: Full lifecycle operational. State machine enforced, grace period works, retention cleanup runs.

---

## Phase 7: User Story 5 — Invite Participants to Rooms (Priority: P1)

**Goal**: Organizers can invite via email, direct link, or guest magic link. Invitees join with validated tokens. Invites can be revoked.

**Independent Test**: Create invites for a scheduled room (email + direct + guest), validate join with each token type, revoke an invite and verify token invalidated

### Tests for User Story 5

- [ ] T059 [P] [US5] Write unit tests for GenerateRoomInviteCommandHandler (token generation, email/direct/guest types, rate limiting, room state validation) in `backend/src/Modules/Rooms.Tests/Unit/Application/GenerateRoomInviteTests.cs`
- [ ] T060 [P] [US5] Write unit tests for JoinRoomCommandHandler (token validation, role assignment, participant count check, expired/revoked rejection) in `backend/src/Modules/Rooms.Tests/Unit/Application/JoinRoomTests.cs`

### Implementation for User Story 5

- [ ] T061 [US5] Implement GenerateRoomInviteCommandHandler (crypto token generation, email/direct/guest types, rate limit 100/room/day) in `backend/src/Modules/Rooms/Application/Commands/GenerateRoomInviteCommandHandler.cs`
- [ ] T062 [P] [US5] Implement GenerateRoomInviteValidator in `backend/src/Modules/Rooms/Application/Validators/GenerateRoomInviteValidator.cs`
- [ ] T063 [US5] Implement RevokeRoomInviteCommandHandler (invalidate token, set status to Revoked) in `backend/src/Modules/Rooms/Application/Commands/RevokeRoomInviteCommandHandler.cs`
- [ ] T064 [US5] Implement JoinRoomCommandHandler (validate token, check room state Scheduled/Live, check participant count vs plan limit, assign role based on invite type, generate LiveKit join token) in `backend/src/Modules/Rooms/Application/Commands/JoinRoomCommandHandler.cs`
- [ ] T065 [P] [US5] Implement ListRoomInvitesQueryHandler with status filter in `backend/src/Modules/Rooms/Application/Queries/ListRoomInvitesQueryHandler.cs`
- [ ] T066 [US5] Create InvitesController with POST, POST /bulk, GET, DELETE, POST /join endpoints in `backend/src/Modules/Rooms/Api/Controllers/InvitesController.cs`
- [ ] T067 [P] [US5] Create RoomInviteDto, CreateRoomInviteRequest, JoinRoomRequest, JoinRoomResponse DTOs in `backend/src/Modules/Rooms/Api/Dtos/RoomInviteDto.cs`
- [ ] T068 [US5] Publish RoomInviteSent, ParticipantJoined integration events

**Checkpoint**: Full invite flow operational. Email, direct link, and guest magic link invite types work. Join validates tokens and enforces limits.

---

## Phase 8: User Story 6 — Moderator Assignment and Handover (Priority: P1)

**Goal**: Rooms require exactly one moderator. Moderator can be changed pre-Live and handed over during Grace period.

**Independent Test**: Assign moderator, change moderator in Scheduled state, simulate handover during Grace, verify room returns to Live

### Tests for User Story 6

- [ ] T069 [P] [US6] Write unit tests for AssignModeratorCommandHandler and HandoverModeratorCommandHandler (role validation, state constraints, handover during Grace only) in `backend/src/Modules/Rooms.Tests/Unit/Application/ModeratorTests.cs`

### Implementation for User Story 6

- [ ] T070 [US6] Implement AssignModeratorCommandHandler (validates user is Admin/Owner, room in Draft/Scheduled) in `backend/src/Modules/Rooms/Application/Commands/AssignModeratorCommandHandler.cs`
- [ ] T071 [US6] Implement HandoverModeratorCommandHandler (validates room in Grace, new user is pre-authorized, transitions room back to Live) in `backend/src/Modules/Rooms/Application/Commands/HandoverModeratorCommandHandler.cs`
- [ ] T072 [US6] Add PATCH /moderator and POST /moderator/handover endpoints to OccurrencesController in `backend/src/Modules/Rooms/Api/Controllers/OccurrencesController.cs`
- [ ] T073 [US6] Publish ModeratorAssigned, ModeratorHandover integration events

**Checkpoint**: Moderator management complete. Assignment, change, and handover all functional with proper state constraints.

---

## Phase 9: User Story 7 — Real-Time Participant Tracking (Priority: P1)

**Goal**: Track participant presence in real-time via Redis + SignalR. LiveKit webhooks drive state changes. Post-room analytics available.

**Independent Test**: Connect to SignalR hub, simulate LiveKit webhook for participant join, verify broadcast received, verify Redis state updated, check analytics after room ends

### Tests for User Story 7

- [ ] T074 [P] [US7] Write unit tests for LiveKitWebhookHandler (participant_joined, participant_left, track changes, HMAC verification, idempotency) in `backend/src/Modules/Rooms.Tests/Unit/Infrastructure/LiveKitWebhookHandlerTests.cs`
- [ ] T075 [P] [US7] Write unit tests for ParticipantStateCache (Redis hash operations, TTL management) in `backend/src/Modules/Rooms.Tests/Unit/Infrastructure/ParticipantStateCacheTests.cs`

### Implementation for User Story 7

- [ ] T076 [US7] Implement ParticipantStateCache (Redis hash per room, JSON participant state, TTL management) in `backend/src/Modules/Rooms/Infrastructure/Cache/ParticipantStateCache.cs`
- [ ] T077 [US7] Implement LiveKitWebhookHandler (HMAC-SHA256 signature verification, participant_joined/left/track events, idempotent via event ID, dual-write to Redis + SQL, trigger state transitions) in `backend/src/Modules/Rooms/Infrastructure/Services/LiveKitWebhookHandler.cs`
- [ ] T078 [US7] Create RoomHub SignalR hub with JoinRoomGroup/LeaveRoomGroup methods and broadcast events (ParticipantJoined, ParticipantLeft, ParticipantMediaChanged, RoomStatusChanged, ModeratorChanged) in `backend/src/Modules/Rooms/Api/Hubs/RoomHub.cs`
- [ ] T079 [US7] Implement ListParticipantsQueryHandler (reads from Redis during Live, falls back to SQL for Ended rooms) in `backend/src/Modules/Rooms/Application/Queries/ListParticipantsQueryHandler.cs`
- [ ] T080 [US7] Implement GetRoomAnalyticsQueryHandler (total participants, peak concurrent, dwell times, audio/video participation rates) in `backend/src/Modules/Rooms/Application/Queries/GetRoomAnalyticsQueryHandler.cs`
- [ ] T081 [US7] Create ParticipantsController with GET /participants and GET /analytics endpoints in `backend/src/Modules/Rooms/Api/Controllers/ParticipantsController.cs`
- [ ] T082 [P] [US7] Create ParticipantDto, RoomAnalyticsDto DTOs in `backend/src/Modules/Rooms/Api/Dtos/ParticipantDto.cs`
- [ ] T083 [US7] Register LiveKit webhook endpoint at POST /api/v1/webhooks/livekit
- [ ] T084 [US7] Publish ParticipantJoined, ParticipantLeft integration events from webhook handler

**Checkpoint**: Real-time tracking operational. Participants tracked in Redis, SignalR broadcasts work, analytics available post-room.

---

## Phase 10: User Story 8 — Recording and Transcription (Priority: P2)

**Goal**: Moderators can record sessions (manual or auto-start). Recordings stored in MinIO. Optional transcription via external service.

**Independent Test**: Enable recording on a room, start/stop recording, verify file in MinIO, verify metadata in database, trigger transcription, verify transcript stored

### Tests for User Story 8

- [ ] T085 [P] [US8] Write unit tests for StartRecordingCommandHandler and StopRecordingCommandHandler (plan validation, visibility enforcement, status tracking) in `backend/src/Modules/Rooms.Tests/Unit/Application/RecordingTests.cs`

### Implementation for User Story 8

- [ ] T086 [US8] Implement IRecordingService and MinIoRecordingService (start LiveKit egress, upload to MinIO, generate pre-signed download URLs) in `backend/src/Modules/Rooms/Infrastructure/Services/MinIoRecordingService.cs`
- [ ] T087 [US8] Implement StartRecordingCommandHandler (validates room is Live, plan allows recording, starts egress) in `backend/src/Modules/Rooms/Application/Commands/StartRecordingCommandHandler.cs`
- [ ] T088 [US8] Implement StopRecordingCommandHandler (stops egress, records metadata) in `backend/src/Modules/Rooms/Application/Commands/StopRecordingCommandHandler.cs`
- [ ] T089 [US8] Implement ITranscriptionService and TranscriptionService (AssemblyAI integration behind interface, submit job, poll status) in `backend/src/Modules/Rooms/Infrastructure/Services/TranscriptionService.cs`
- [ ] T090 [US8] Implement TranscriptionJob MassTransit consumer (processes recording completed event, submits transcription, stores VTT + text in MinIO) in `backend/src/Modules/Rooms/Application/BackgroundJobs/TranscriptionJob.cs`
- [ ] T091 [US8] Create RecordingsController with GET /recording (metadata + pre-signed URLs), visibility enforcement in `backend/src/Modules/Rooms/Api/Controllers/RecordingsController.cs`
- [ ] T092 [P] [US8] Create RecordingDto DTOs in `backend/src/Modules/Rooms/Api/Dtos/RecordingDto.cs`
- [ ] T093 [US8] Publish RecordingStarted, RecordingCompleted, TranscriptionCompleted integration events

**Checkpoint**: Recording and transcription pipeline operational. Files stored in MinIO, visibility enforced, transcription async.

---

## Phase 11: Frontend — Room Management UI (Priority: P2)

**Goal**: React UI for room creation, calendar view, and live room experience

**Independent Test**: Create a template via UI, schedule a room, view it on calendar, join a live room and see participant list update

- [ ] T094 [P] [FE] Create rooms API layer using RTK Query in `frontend/src/features/rooms/api/roomsApi.ts` (templates, occurrences, series endpoints)
- [ ] T095 [P] [FE] Create invites API layer in `frontend/src/features/rooms/api/invitesApi.ts`
- [ ] T096 [P] [FE] Create RoomTemplateForm component in `frontend/src/features/rooms/components/RoomTemplateForm.tsx`
- [ ] T097 [P] [FE] Create RoomStatusBadge component (Draft, Scheduled, Live, Grace, Ended, Archived) in `frontend/src/features/rooms/components/RoomStatusBadge.tsx`
- [ ] T098 [P] [FE] Create InviteDialog component (email, direct link, guest magic link, bulk CSV) in `frontend/src/features/rooms/components/InviteDialog.tsx`
- [ ] T099 [FE] Create CreateRoomPage (template selector, schedule picker, timezone selector, moderator picker) in `frontend/src/features/rooms/pages/CreateRoomPage.tsx`
- [ ] T100 [FE] Create RoomCalendar component (month/week/day view, occurrence display, series indicators) in `frontend/src/features/rooms/components/RoomCalendar.tsx`
- [ ] T101 [FE] Create RoomCalendarPage in `frontend/src/features/rooms/pages/RoomCalendarPage.tsx`
- [ ] T102 [FE] Create ParticipantList component with real-time SignalR updates in `frontend/src/features/rooms/components/ParticipantList.tsx`
- [ ] T103 [FE] Create LiveRoomPage (participant list, status, grace countdown, recording indicator, moderator controls) in `frontend/src/features/rooms/pages/LiveRoomPage.tsx`
- [ ] T104 [FE] Create useRoom hook for SignalR connection management and room state in `frontend/src/features/rooms/hooks/useRoom.ts`
- [ ] T105 [P] [FE] Create RecordingPlayer component (stream/download, transcript viewer) in `frontend/src/features/rooms/components/RecordingPlayer.tsx`

**Checkpoint**: Frontend complete. Room creation, calendar, live room view, and recording player all functional.

---

## Phase 12: Polish & Cross-Cutting Concerns

**Purpose**: Integration tests, documentation, performance, security hardening

- [ ] T106 [P] Write integration tests for room lifecycle (create template → create room → join → grace → end) in `backend/src/Modules/Rooms.Tests/Integration/RoomLifecycleTests.cs`
- [ ] T107 [P] Write integration tests for invite flow (send invite → join → revoke → reject) in `backend/src/Modules/Rooms.Tests/Integration/InviteFlowTests.cs`
- [ ] T108 [P] Write integration tests for recording lifecycle (start → stop → download → transcribe) in `backend/src/Modules/Rooms.Tests/Integration/RecordingLifecycleTests.cs`
- [ ] T109 Add XML documentation comments to all public types, methods, and properties across the Rooms module
- [ ] T110 [P] Add OpenTelemetry tracing spans to all critical paths (join flow, state transitions, recording, webhook processing)
- [ ] T111 [P] Add structured audit logging for all room state changes, moderator actions, and invite events
- [ ] T112 Security hardening: validate all inputs, enforce authorization on all endpoints, verify webhook signatures
- [ ] T113 Run quickstart.md validation — verify all steps work end-to-end
- [ ] T114 Performance validation: verify room creation < 200ms, join < 500ms, state transitions < 100ms, real-time updates < 2s

---

## Dependencies & Execution Order

### Phase Dependencies

- **Setup (Phase 1)**: No dependencies — can start immediately
- **Foundational (Phase 2)**: Depends on Setup — BLOCKS all user stories
- **US1 Templates (Phase 3)**: Depends on Foundational — no other story dependencies
- **US2 One-Off Rooms (Phase 4)**: Depends on Foundational + US1 (needs templates)
- **US3 Recurring Series (Phase 5)**: Depends on Foundational + US1 (needs templates)
- **US4 Lifecycle (Phase 6)**: Depends on Foundational — can run parallel to US2/US3
- **US5 Invites (Phase 7)**: Depends on Foundational + US2 (needs occurrences)
- **US6 Moderator (Phase 8)**: Depends on Foundational + US4 (needs lifecycle/Grace state)
- **US7 Participants (Phase 9)**: Depends on Foundational + US4 (needs lifecycle transitions)
- **US8 Recording (Phase 10)**: Depends on US4 + US7 (needs Live state + participant tracking)
- **Frontend (Phase 11)**: Can start after US1 API is ready; incrementally adds UI as backend phases complete
- **Polish (Phase 12)**: Depends on all desired user stories being complete

### User Story Dependencies

```
US1 (Templates) ─────┬──→ US2 (One-Off Rooms) ──→ US5 (Invites)
                      │
                      └──→ US3 (Series)

US4 (Lifecycle) ──────┬──→ US6 (Moderator)
                      ├──→ US7 (Participants) ──→ US8 (Recording)
                      └──→ US5 (Invites, needs room state validation)
```

### Within Each User Story

- Tests MUST be written and FAIL before implementation
- Domain entities/value objects before command handlers
- Command handlers before query handlers
- Handlers before controllers
- Core implementation before integration event publishing
- Story complete and tested before moving to next

### Parallel Opportunities

- All Phase 1 tasks marked [P] can run in parallel
- All Phase 2 entity creation tasks (T011-T017) can run in parallel
- US2 and US3 can run in parallel (both depend on US1 but not on each other)
- US4 can run in parallel with US2/US3 (only depends on Foundational)
- US6 and US7 can run in parallel (both depend on US4 but not on each other)
- All frontend tasks marked [P] can run in parallel
- All integration tests (T106-T108) can run in parallel

---

## Parallel Example: Phase 2 (Foundational)

```
# Launch all entity creation tasks in parallel:
Task T011: "RoomTemplate aggregate root"
Task T012: "RoomSeries aggregate root"
Task T013: "RoomOccurrence aggregate root with state machine"
Task T014: "ModeratorAssignment value object"
Task T015: "RoomInvite entity"
Task T016: "RoomParticipantState entity"
Task T017: "Recording aggregate root"

# After entities complete, run in parallel:
Task T020: "State machine unit tests"
Task T021: "Template domain unit tests"
```

## Parallel Example: Frontend (Phase 11)

```
# Launch all API layers and independent components in parallel:
Task T094: "roomsApi.ts"
Task T095: "invitesApi.ts"
Task T096: "RoomTemplateForm.tsx"
Task T097: "RoomStatusBadge.tsx"
Task T098: "InviteDialog.tsx"
Task T105: "RecordingPlayer.tsx"
```

---

## Implementation Strategy

### MVP First (User Stories 1 + 2)

1. Complete Phase 1: Setup
2. Complete Phase 2: Foundational (CRITICAL — blocks all stories)
3. Complete Phase 3: User Story 1 (Templates)
4. Complete Phase 4: User Story 2 (One-Off Rooms)
5. **STOP and VALIDATE**: Create a template, create a one-off room, verify in database
6. Deploy/demo if ready — rooms can be created and scheduled

### Incremental Delivery

1. Setup + Foundational → Foundation ready
2. US1 (Templates) → Template management works → Demo
3. US2 (One-Off Rooms) + US3 (Series) → Full scheduling works → Demo
4. US4 (Lifecycle) + US5 (Invites) + US6 (Moderator) → Live rooms work → Demo
5. US7 (Participants) → Real-time tracking works → Demo
6. US8 (Recording) → Recording/transcription works → Demo
7. Frontend → Full UI experience → Demo
8. Polish → Production-ready → Release

---

## Notes

- [P] tasks = different files, no dependencies
- [Story] label maps task to specific user story for traceability
- Each user story should be independently completable and testable
- Verify tests fail before implementing (Red-Green-Refactor per Constitution IV)
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
