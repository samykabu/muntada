# Epic 3: Rooms & Scheduling Module - Task Breakdown

**Version:** 1.0
**Epic Owner:** Product / Engineering
**Last Updated:** 2026-04-03

---

## Execution Overview

This epic implements room management and scheduling. Depends on Identity (Epic 1), Tenancy (Epic 2), and Shared Kernel (Epic 0). Tasks organize into 6 phases: infrastructure, room templates, scheduling, lifecycle, invites, and recording.

---

## Phase 1: Module Setup & Entities

### T301: Rooms Module Structure & Database [P]
**User Story:** US-3.1
**Priority:** P1
**Effort:** 5 pts
**Dependencies:** Epic 0, Epic 1, Epic 2

Create Rooms module structure and database schema.

**File Locations:**
- `backend/src/Modules/Rooms/Rooms.csproj`
- `backend/src/Modules/Rooms/Infrastructure/RoomsDbContext.cs`
- SQL schema: `[rooms]`

---

### T302: Room Template Entity [P]
**User Story:** US-3.1
**Priority:** P1
**Effort:** 3 pts
**Dependencies:** T301

Implement RoomTemplate aggregate.

**Deliverables:**
- `RoomTemplate` (TenantId, Name, Description, Settings, CreatedBy, CreatedAt, UpdatedAt)
- `RoomSettings` value object
- Immutable name field

**File Locations:**
- `backend/src/Modules/Rooms/Domain/Template/RoomTemplate.cs`

---

### T303: Room Series & Recurrence Entities [P]
**User Story:** US-3.2
**Priority:** P1
**Effort:** 5 pts
**Dependencies:** T301

Implement RoomSeries aggregate.

**Deliverables:**
- `RoomSeries` (TenantId, TemplateId, Title, RecurrenceRule, StartsAt, EndsAt, Status, CreatedBy)
- `RoomOccurrence` (TenantId, RoomSeriesId, Title, ScheduledAt, Status, ModeratorAssignment, Settings)
- iCal recurrence rule support
- Status enums

**File Locations:**
- `backend/src/Modules/Rooms/Domain/Series/RoomSeries.cs`
- `backend/src/Modules/Rooms/Domain/Occurrence/RoomOccurrence.cs`

---

### T304: Room Invite Entity [P]
**User Story:** US-3.4
**Priority:** P1
**Effort:** 3 pts
**Dependencies:** T303

Implement room invites.

**Deliverables:**
- `RoomInvite` (RoomOccurrenceId, InvitedEmail, InvitedUserId, InviteToken, Status, InvitedBy, ExpiresAt)
- Status enum (Pending, Accepted, Revoked, Expired)

**File Locations:**
- `backend/src/Modules/Rooms/Domain/Invite/RoomInvite.cs`

---

### T305: Room Participant State Entity [P]
**User Story:** US-3.6
**Priority:** P1
**Effort:** 3 pts
**Dependencies:** T303

Implement participant tracking.

**Deliverables:**
- `RoomParticipantState` (RoomOccurrenceId, UserId, DisplayName, JoinedAt, LeftAt, AudioState, VideoState, LiveKitParticipantId)
- `MediaState` enum (Muted, Unmuted, Off, On)

**File Locations:**
- `backend/src/Modules/Rooms/Domain/Participant/RoomParticipantState.cs`

---

### T306: Recording Entity [P]
**User Story:** US-3.7
**Priority:** P1
**Effort:** 3 pts
**Dependencies:** T303

Implement recording metadata.

**Deliverables:**
- `Recording` (RoomOccurrenceId, TenantId, S3Path, FileSizeBytes, DurationSeconds, Status, Visibility, CreatedAt)
- `Transcript` (Language, S3Path, Status, CreatedAt)
- Status and visibility enums

**File Locations:**
- `backend/src/Modules/Rooms/Domain/Recording/Recording.cs`

---

## Phase 2: Room Templates & Series

### T307: Create Room Template Command [P]
**User Story:** US-3.1
**Priority:** P1
**Effort:** 5 pts
**Dependencies:** T302

Implement template creation.

**Deliverables:**
- `CreateRoomTemplateCommand` & handler
- Validation (unique name per tenant, plan limits)
- Publish `RoomTemplateCreated` event

**File Locations:**
- `backend/src/Modules/Rooms/Application/Commands/CreateRoomTemplateCommand.cs`

---

### T308: Create Room Series Command [P]
**User Story:** US-3.2
**Priority:** P1
**Effort:** 10 pts
**Dependencies:** T303, T307

Implement recurring room series creation.

**Deliverables:**
- `CreateRoomSeriesCommand` & handler
- iCal.NET library integration for RRULE parsing
- Occurrence generation (30 days ahead)
- Validation and error handling
- Publish `RoomSeriesCreated`, `RoomOccurrenceGenerated` events

**File Locations:**
- `backend/src/Modules/Rooms/Application/Commands/CreateRoomSeriesCommand.cs`
- `backend/src/Modules/Rooms/Application/Services/RecurrenceService.cs`

**Acceptance:**
- Series created with valid RRULE
- Occurrences generated 30 days ahead
- Parsing errors handled gracefully

---

### T309: Occurrence Generation Background Job [P]
**User Story:** US-3.2
**Priority:** P1
**Effort:** 5 pts
**Dependencies:** T308

Implement scheduled occurrence generation.

**Deliverables:**
- `OccurrenceGenerationJob` - runs hourly
- Idempotent (safe to re-run)
- Handles series end dates
- Publishes events for new occurrences

**File Locations:**
- `backend/src/Modules/Rooms/Application/BackgroundJobs/OccurrenceGenerationJob.cs`

---

### T310: Single Occurrence Override [P]
**User Story:** US-3.2
**Priority:** P2
**Effort:** 5 pts
**Dependencies:** T309

Implement per-instance modifications.

**Deliverables:**
- `UpdateRoomOccurrenceCommand` for title, max participants, cancelled flag
- Affects only single occurrence
- Does not affect series or other occurrences

**File Locations:**
- `backend/src/Modules/Rooms/Application/Commands/UpdateRoomOccurrenceCommand.cs`

---

## Phase 3: Room Lifecycle & State Machine

### T311: Room Lifecycle State Machine [P]
**User Story:** US-3.3
**Priority:** P1
**Effort:** 10 pts
**Dependencies:** T303

Implement room state transitions.

**Deliverables:**
- State machine: Draft → Scheduled → Live → Grace → Ended → Archived
- Grace period timer (default 5 minutes, configurable)
- Transition validation (prevent invalid transitions)
- Persistence of state changes
- Publish lifecycle events

**File Locations:**
- `backend/src/Modules/Rooms/Domain/Occurrence/RoomOccurrenceStateMachine.cs`
- `backend/src/Modules/Rooms/Domain/Events/RoomLiveEvent.cs`, etc.

**Acceptance:**
- State transitions validated
- Grace period enforced
- Events published for each transition

---

### T312: Grace Period Management [P]
**User Story:** US-3.3
**Priority:** P1
**Effort:** 8 pts
**Dependencies:** T311

Implement grace period logic for moderator disconnect.

**Deliverables:**
- Grace period starts when last moderator disconnects
- Countdown timer (configurable)
- Remaining participants see countdown
- Moderator reconnect returns to Live
- Timeout expires → Ended

**File Locations:**
- `backend/src/Modules/Rooms/Application/Services/GracePeriodService.cs`

---

### T313: Moderator Assignment [P]
**User Story:** US-3.5
**Priority:** P1
**Effort:** 5 pts
**Dependencies:** T303

Implement moderator designation and handover.

**Deliverables:**
- `ModeratorAssignment` class (UserId, AssignedAt, DisconnectedAt)
- `AssignModeratorCommand` & handler
- `HandoverModeratorCommand` & handler
- Publish `ModeratorAssigned`, `ModeratorHandover` events

**File Locations:**
- `backend/src/Modules/Rooms/Application/Commands/AssignModeratorCommand.cs`

---

## Phase 4: Room Invites & Access Control

### T314: Room Invite Generation [P]
**User Story:** US-3.4
**Priority:** P1
**Effort:** 8 pts
**Dependencies:** T304

Implement invite creation and email sending.

**Deliverables:**
- `GenerateRoomInviteCommand` & handler (single or bulk via CSV)
- Invite token generation
- Email sending
- Rate limiting (max 100 per room per day)
- Publish `RoomInviteSent` event

**File Locations:**
- `backend/src/Modules/Rooms/Application/Commands/GenerateRoomInviteCommand.cs`

---

### T315: Room Join & Validation [P]
**User Story:** US-3.4
**Priority:** P1
**Effort:** 8 pts
**Dependencies:** T314

Implement join validation and participant addition.

**Deliverables:**
- `JoinRoomCommand` & handler
- Token validation (invite or direct link)
- Room state validation (Scheduled or Live)
- Participant count check against plan limit
- Publish `ParticipantJoined` event

**File Locations:**
- `backend/src/Modules/Rooms/Application/Commands/JoinRoomCommand.cs`

---

## Phase 5: Real-Time Participant Tracking

### T316: Participant Real-Time State in Redis [P]
**User Story:** US-3.6
**Priority:** P1
**Effort:** 8 pts
**Dependencies:** T305

Implement ephemeral participant state caching.

**Deliverables:**
- Redis key: `room:{roomId}:participants:{participantId}`
- Fields: id, identity, role, joinedAt, audioEnabled, speakingState, qualityScore
- Update on LiveKit webhook events
- TTL: room duration + 1 hour buffer

**File Locations:**
- `backend/src/Modules/Rooms/Infrastructure/Cache/ParticipantStateCache.cs`

---

### T317: Participant List Endpoint & Real-Time Broadcast [P]
**User Story:** US-3.6
**Priority:** P1
**Effort:** 8 pts
**Dependencies:** T316

Create participant API and WebSocket broadcasting.

**Deliverables:**
- `GET /api/rooms/{roomId}/participants` endpoint
- SignalR room hub: `room-{roomId}`
- Broadcast on join/leave/status change
- WebSocket push updates (< 100ms latency)

**File Locations:**
- `backend/src/Modules/Rooms/Api/Controllers/ParticipantsController.cs`
- `backend/src/Modules/Rooms/Api/Hubs/RoomHub.cs`

---

### T318: Room Analytics Endpoint [P]
**User Story:** US-3.6
**Priority:** P2
**Effort:** 8 pts
**Dependencies:** T317

Implement analytics queries.

**Deliverables:**
- `GET /api/rooms/{roomId}/analytics` - post-room stats
- Metrics: total participants, peak concurrent, dwell time, quality metrics
- Aggregation from RoomParticipantState and LiveKit data

**File Locations:**
- `backend/src/Modules/Rooms/Application/Queries/GetRoomAnalyticsQuery.cs`

---

## Phase 6: Recording & Scheduling

### T319: Recording Settings & Auto-Start [P]
**User Story:** US-3.7
**Priority:** P1
**Effort:** 5 pts
**Dependencies:** T306

Implement recording configuration.

**Deliverables:**
- `UpdateRoomRecordingSettingsCommand` & handler
- Settings: AllowRecording, Visibility (Private, Shared, Public), AllowTranscription, Language, AutoStartRecording
- Publish `RecordingSettingsUpdated` event

**File Locations:**
- `backend/src/Modules/Rooms/Application/Commands/UpdateRoomRecordingSettingsCommand.cs`

---

### T320: Recording Lifecycle & MinIO Integration [P]
**User Story:** US-3.7
**Priority:** P1
**Effort:** 10 pts
**Dependencies:** T319

Implement recording capture and storage.

**Deliverables:**
- `StartRecordingCommand` & handler
- `StopRecordingCommand` & handler
- MinIO integration (S3 upload)
- Recording metadata storage
- Status: Processing → Ready or Failed
- Publish `RecordingStarted`, `RecordingCompleted` events

**File Locations:**
- `backend/src/Modules/Rooms/Application/Commands/StartRecordingCommand.cs`
- `backend/src/Modules/Rooms/Infrastructure/Services/MinIORecordingService.cs`

---

### T321: Transcription Service Integration [P]
**User Story:** US-3.7
**Priority:** P2
**Effort:** 10 pts
**Dependencies:** T320

Implement transcription workflow.

**Deliverables:**
- Transcription service integration (Google Speech-to-Text, AssemblyAI, etc.)
- Job queuing to RabbitMQ
- Transcript status: Processing → Ready or Failed
- VTT and text output formats
- Publish `TranscriptionCompleted` event

**File Locations:**
- `backend/src/Modules/Rooms/Application/Services/TranscriptionService.cs`
- `backend/src/Modules/Rooms/Application/BackgroundJobs/TranscriptionJob.cs`

---

### T322: Recording Retrieval & Visibility [P]
**User Story:** US-3.7
**Priority:** P2
**Effort:** 5 pts
**Dependencies:** T320

Implement recording download/stream.

**Deliverables:**
- `GET /api/rooms/{roomId}/recording` - retrieve recording
- `GET /api/rooms/{roomId}/transcript` - retrieve transcript
- Visibility enforcement (Private, Shared, Public)
- Pre-signed S3 URLs for download

**File Locations:**
- `backend/src/Modules/Rooms/Api/Controllers/RecordingsController.cs`

---

## Phase 7: API Endpoints

### T323: Rooms API - Template Management [P]
**User Story:** US-3.1
**Priority:** P1
**Effort:** 5 pts
**Dependencies:** T307

Create REST endpoints for room templates.

**Deliverables:**
- `POST /api/rooms/templates` - create
- `GET /api/rooms/templates` - list
- `GET /api/rooms/templates/{templateId}` - retrieve
- `PATCH /api/rooms/templates/{templateId}` - update
- Swagger docs

**File Locations:**
- `backend/src/Modules/Rooms/Api/Controllers/TemplatesController.cs`

---

### T324: Rooms API - Series & Occurrence Management [P]
**User Story:** US-3.2, US-3.3
**Priority:** P1
**Effort:** 8 pts
**Dependencies:** T308

Create REST endpoints for series and occurrences.

**Deliverables:**
- `POST /api/rooms/series` - create series
- `GET /api/rooms/series` - list
- `PATCH /api/rooms/series/{seriesId}` - update series
- `GET /api/rooms/occurrences` - list
- `PATCH /api/rooms/occurrences/{occurrenceId}` - update occurrence
- `PATCH /api/rooms/occurrences/{occurrenceId}/status` - transition state

**File Locations:**
- `backend/src/Modules/Rooms/Api/Controllers/SeriesController.cs`
- `backend/src/Modules/Rooms/Api/Controllers/OccurrencesController.cs`

---

### T325: Rooms API - Invites & Joins [P]
**User Story:** US-3.4
**Priority:** P1
**Effort:** 8 pts
**Dependencies:** T314, T315

Create REST endpoints for invites.

**Deliverables:**
- `POST /api/rooms/{roomId}/invites` - create invite
- `GET /api/rooms/{roomId}/invites` - list invites
- `POST /api/rooms/{roomId}/join` - join room
- `DELETE /api/rooms/{roomId}/invites/{inviteId}` - revoke invite

**File Locations:**
- `backend/src/Modules/Rooms/Api/Controllers/InvitesController.cs`

---

### T326: Frontend: Room Creation & Templates [P]
**User Story:** US-3.1
**Priority:** P2
**Effort:** 10 pts
**Dependencies:** T323

Create React UI for room creation.

**Deliverables:**
- `frontend/src/features/rooms/pages/CreateRoomPage.tsx`
- Template selector
- Series configuration (recurrence picker)
- Settings form (max participants, recording, transcription)

**File Locations:**
- `frontend/src/features/rooms/pages/CreateRoomPage.tsx`
- `frontend/src/features/rooms/components/RoomForm.tsx`

---

### T327: Frontend: Room Calendar View [P]
**User Story:** US-3.2
**Priority:** P2
**Effort:** 12 pts
**Dependencies:** T324

Create calendar UI for scheduled rooms.

**Deliverables:**
- Calendar component (month/week/day view)
- Room occurrences displayed
- Click to view/edit room
- Series indicators

**File Locations:**
- `frontend/src/features/rooms/components/RoomCalendar.tsx`

---

### T328: Frontend: Live Room View [P]
**User Story:** US-3.3, US-3.6
**Priority:** P2
**Effort:** 15 pts
**Dependencies:** T316, T317

Create live room UI.

**Deliverables:**
- Participant list with real-time updates
- Speaking status indicators
- Quality indicators
- Room state display (Live, Grace, Ended)
- Recording indicator
- Grace period countdown

**File Locations:**
- `frontend/src/features/rooms/pages/LiveRoomPage.tsx`
- `frontend/src/features/rooms/components/ParticipantList.tsx`

---

## Phase 8: Integration Tests

### T329: Rooms Integration Tests [P]
**User Story:** All
**Priority:** P1
**Effort:** 21 pts
**Dependencies:** All tasks

Write comprehensive tests for rooms module.

**Deliverables:**
- Tests for template creation
- Tests for series generation
- Tests for room lifecycle
- Tests for invites and joins
- Tests for participant tracking
- Tests for recording

**File Locations:**
- `backend/src/Modules/Rooms.Tests/Integration/`

**Acceptance:**
- All scenarios tested
- Coverage > 80%
- Edge cases handled

---

## Success Metrics

- Room templates created in < 1 minute
- Series generation works for all recurrence patterns
- State transitions atomic and valid
- Participants join/leave in < 2 seconds
- Roster accurate within 5 minutes
- Recordings captured and transcribed within 5 minutes
- Grace period enforced correctly
- Performance: room endpoints < 200ms p95
- Scalability: 1000+ participants per room, 100+ rooms
