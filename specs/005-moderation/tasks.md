# Epic 5: Moderation Module - Task Breakdown

**Version:** 1.0
**Epic Owner:** Room Management & Safety
**Last Updated:** 2026-04-03

---

## Execution Overview

This epic manages server-authoritative controls over room participants. Depends on Rooms (Epic 3) and Realtime Orchestration (Epic 4). Tasks organize into 4 phases: infrastructure, hand raise queue, speaker management, and roster/controls.

---

## Phase 1: Module Setup & Entities

### T501: Moderation Module Structure [P]
**User Story:** US-5.1
**Priority:** P1
**Effort:** 5 pts
**Dependencies:** Epic 0, Epic 3, Epic 4

Create Moderation module structure and database schema.

**File Locations:**
- `backend/src/Modules/Moderation/Moderation.csproj`
- `backend/src/Modules/Moderation/Infrastructure/ModerationDbContext.cs`
- SQL schema: `[moderation]`

---

### T502: Hand Raise Request Entity [P]
**User Story:** US-5.1
**Priority:** P1
**Effort:** 3 pts
**Dependencies:** T501

Implement hand raise entity.

**Deliverables:**
- `HandRaiseRequest` (RoomId, ParticipantId, Status, CreatedAt, Position, ExpiresAt, RejectionReason)
- `HandRaiseStatus` enum (Queued, Approved, Rejected, Cancelled, Expired)

**File Locations:**
- `backend/src/Modules/Moderation/Domain/HandRaise/HandRaiseRequest.cs`

---

### T503: Speaker Grant Entity [P]
**User Story:** US-5.2
**Priority:** P1
**Effort:** 3 pts
**Dependencies:** T501

Implement speaker grant entity.

**Deliverables:**
- `SpeakerGrant` (HandRaiseId, ParticipantId, RoomId, Status, GrantedAt, ExpiresAt, GrantedBy, RevokedAt, RevokedBy)
- `SpeakerGrantStatus` enum (Active, Revoked, Expired, Completed)

**File Locations:**
- `backend/src/Modules/Moderation/Domain/SpeakerGrant/SpeakerGrant.cs`

---

### T504: Moderator Assignment Entity [P]
**User Story:** US-5.5
**Priority:** P1
**Effort:** 2 pts
**Dependencies:** T501

Implement moderator assignment tracking.

**Deliverables:**
- `ModeratorAssignment` (RoomId, ParticipantId, AssignedAt, AssignedBy, EndedAt)

**File Locations:**
- `backend/src/Modules/Moderation/Domain/Moderator/ModeratorAssignment.cs`

---

## Phase 2: Hand Raise Queue Management

### T505: Create Hand Raise Command [P]
**User Story:** US-5.1
**Priority:** P1
**Effort:** 8 pts
**Dependencies:** T502

Implement hand raise submission.

**Deliverables:**
- `RaiseHandCommand` & handler
- Check if participant already speaking (prevent duplicate)
- Generate request with position calculation
- Timeout configuration (default 30 minutes)
- Publish `HandRaiseQueued` event
- Broadcast to room via SignalR

**File Locations:**
- `backend/src/Modules/Moderation/Application/Commands/RaiseHandCommand.cs`

**Acceptance:**
- Hand raise created and queued
- Position calculated correctly
- Event published and broadcast
- Duplicate checks work

---

### T506: Cancel Hand Raise Command [P]
**User Story:** US-5.1
**Priority:** P1
**Effort:** 5 pts
**Dependencies:** T505

Implement hand raise cancellation.

**Deliverables:**
- `CancelHandRaiseCommand` & handler
- Participant can cancel own, moderator can cancel any
- Re-order queue after cancellation
- Publish `HandRaiseCancelled` event
- Broadcast queue changes

**File Locations:**
- `backend/src/Modules/Moderation/Application/Commands/CancelHandRaiseCommand.cs`

**Acceptance:**
- Cancellation works
- Queue re-ordered
- Events broadcast

---

### T507: Hand Raise Expiry Job [P]
**User Story:** US-5.1
**Priority:** P1
**Effort:** 5 pts
**Dependencies:** T505

Implement expiry cleanup.

**Deliverables:**
- `HandRaiseExpiryJob` - runs every 60 seconds
- Mark requests with createdAt + timeout as Expired
- Re-order queue
- Publish events
- Participant notification

**File Locations:**
- `backend/src/Modules/Moderation/Application/BackgroundJobs/HandRaiseExpiryJob.cs`

**Acceptance:**
- Expired requests cleaned up
- Notifications sent
- Queue updated

---

### T508: Get Hand Raise Queue Query [P]
**User Story:** US-5.1
**Priority:** P1
**Effort:** 5 pts
**Dependencies:** T505

Implement queue listing.

**Deliverables:**
- `GetHandRaiseQueueQuery` & handler
- Ordered by position
- Include estimated wait times
- Authorization check (only moderators see details)

**File Locations:**
- `backend/src/Modules/Moderation/Application/Queries/GetHandRaiseQueueQuery.cs`

**Acceptance:**
- Queue returns ordered list
- Wait times calculated
- Authorization enforced

---

## Phase 3: Speaker Management

### T509: Approve Hand Raise Command [P]
**User Story:** US-5.2
**Priority:** P1
**Effort:** 10 pts
**Dependencies:** T506

Implement speaker approval.

**Deliverables:**
- `ApproveHandRaiseCommand` & handler
- Moderator authorization check
- Create `SpeakerGrant` with status Active
- Optional expiry duration
- Call Realtime Orchestration to issue new token with publish claim
- Send token via SignalR: {event: "token.refreshed", token, reason: "speaker_grant_approved"}
- Update HandRaiseRequest status to Approved
- Re-order queue
- Publish `HandRaiseApproved`, `SpeakerGrantCreated` events
- Broadcast to room

**File Locations:**
- `backend/src/Modules/Moderation/Application/Commands/ApproveHandRaiseCommand.cs`

**Acceptance:**
- Grant created
- Token issued within 1 second
- Participant can publish
- Queue updated
- Events broadcast

---

### T510: Reject Hand Raise Command [P]
**User Story:** US-5.2
**Priority:** P1
**Effort:** 5 pts
**Dependencies:** T509

Implement hand raise rejection.

**Deliverables:**
- `RejectHandRaiseCommand` & handler
- Moderator authorization
- Update status to Rejected
- Optional rejection reason
- Re-order queue
- Publish `HandRaiseRejected` event
- Broadcast to queue

**File Locations:**
- `backend/src/Modules/Moderation/Application/Commands/RejectHandRaiseCommand.cs`

**Acceptance:**
- Rejection works
- Queue updated
- Participant notified

---

### T511: Revoke Speaker Grant Command [P]
**User Story:** US-5.3
**Priority:** P1
**Effort:** 10 pts
**Dependencies:** T509

Implement speaker revocation.

**Deliverables:**
- `RevokeSpeakerGrantCommand` & handler
- Moderator authorization
- Update status to Revoked
- Issue new token without publish claim
- Send token via SignalR: {event: "token.refreshed", token, reason: "speaker_grant_revoked"}
- Publish `SpeakerGrantRevoked` event
- Audit logging (moderator, participant, reason)

**File Locations:**
- `backend/src/Modules/Moderation/Application/Commands/RevokeSpeakerGrantCommand.cs`

**Acceptance:**
- Grant revoked
- New token issued within 1 second
- Participant cannot publish
- Audit logged

---

### T512: Speaker Grant Expiry Job [P]
**User Story:** US-5.8
**Priority:** P2
**Effort:** 5 pts
**Dependencies:** T509

Implement grant expiration.

**Deliverables:**
- `SpeakerGrantExpiryJob` - runs every 30 seconds
- Mark grants with expiresAt < now as Expired
- Issue new token without publish claim
- Publish `SpeakerGrantExpired` event
- Broadcast to participant

**File Locations:**
- `backend/src/Modules/Moderation/Application/BackgroundJobs/SpeakerGrantExpiryJob.cs`

**Acceptance:**
- Grants expire correctly
- Token updated
- Participant cannot publish after expiry

---

## Phase 4: Roster & Room Controls

### T513: Room Participant State Management [P]
**User Story:** US-5.4
**Priority:** P1
**Effort:** 8 pts
**Dependencies:** T501

Implement roster state tracking.

**Deliverables:**
- Update room state cache with: role, speakingStatus, recordingStatus, qualityScore, speakerGrantId
- Sync with room state from Realtime module
- Include moderator status
- Broadcast roster changes

**File Locations:**
- `backend/src/Modules/Moderation/Infrastructure/Cache/RosterStateCache.cs`

**Acceptance:**
- Roster accurately reflects room state
- Updates on permission/status changes
- Broadcasting works

---

### T514: Get Room Roster Query [P]
**User Story:** US-5.4
**Priority:** P1
**Effort:** 5 pts
**Dependencies:** T513

Implement roster retrieval.

**Deliverables:**
- `GetRoomRosterQuery` & handler
- Authorization-based filtering (guest listeners get reduced info)
- Includes: participantId, role, speakingStatus, recordingStatus, qualityScore, speakerGrantInfo

**File Locations:**
- `backend/src/Modules/Moderation/Application/Queries/GetRoomRosterQuery.cs`

**Acceptance:**
- Roster returned correctly
- Authorization enforced
- Guest restrictions applied

---

### T515: Moderator Handover Command [P]
**User Story:** US-5.5
**Priority:** P1
**Effort:** 8 pts
**Dependencies:** T504

Implement moderator transfer.

**Deliverables:**
- `HandoverModeratorCommand` & handler
- Current moderator authorization
- Create new assignment for target
- End current assignment
- Issue new token to new moderator with admin_controls claim
- Publish `ModeratorChanged` event
- Broadcast to room
- Audit logging

**File Locations:**
- `backend/src/Modules/Moderation/Application/Commands/HandoverModeratorCommand.cs`

**Acceptance:**
- Handover atomic
- New moderator gets token within 1 second
- Old moderator's controls removed
- Event broadcast

---

### T516: Lock Room Command [P]
**User Story:** US-5.6
**Priority:** P1
**Effort:** 5 pts
**Dependencies:** T501

Implement room locking.

**Deliverables:**
- `LockRoomCommand` & handler
- Moderator authorization
- Update room state: locked = true
- Broadcast `room.locked` event
- Rejecting joins in Realtime module (F4.1.3)

**File Locations:**
- `backend/src/Modules/Moderation/Application/Commands/LockRoomCommand.cs`

**Acceptance:**
- Room locked
- New joins rejected
- Existing participants unaffected

---

### T517: Remove Participant Command [P]
**User Story:** US-5.7
**Priority:** P1
**Effort:** 10 pts
**Dependencies:** T513

Implement participant removal.

**Deliverables:**
- `RemoveParticipantCommand` & handler
- Moderator authorization
- Update participant state: removed = true, removedBy, removedAt
- Auto-revoke any active SpeakerGrant
- Signal participant to disconnect (via SignalR)
- Remove from roster
- Publish `ParticipantRemoved` event
- Audit logging

**File Locations:**
- `backend/src/Modules/Moderation/Application/Commands/RemoveParticipantCommand.cs`

**Acceptance:**
- Participant removed
- Disconnected immediately
- Grants revoked
- Event broadcast

---

## Phase 5: API Endpoints

### T518: Moderation API - Hand Raises [P]
**User Story:** US-5.1, US-5.2
**Priority:** P1
**Effort:** 8 pts
**Dependencies:** T505, T509, T510

Create REST endpoints for hand raises.

**Deliverables:**
- `POST /api/rooms/{roomId}/handRaises` - raise hand
- `POST /api/rooms/{roomId}/handRaises/{handRaiseId}/cancel` - cancel
- `GET /api/rooms/{roomId}/handRaises` - list queue
- `POST /api/rooms/{roomId}/handRaises/{handRaiseId}/approve` - approve
- `POST /api/rooms/{roomId}/handRaises/{handRaiseId}/reject` - reject

**File Locations:**
- `backend/src/Modules/Moderation/Api/Controllers/HandRaisesController.cs`

**Acceptance:**
- All endpoints functional
- Status codes correct
- Swagger docs complete

---

### T519: Moderation API - Speakers & Roster [P]
**User Story:** US-5.3, US-5.4
**Priority:** P1
**Effort:** 8 pts
**Dependencies:** T511, T513, T514

Create REST endpoints for speakers and roster.

**Deliverables:**
- `GET /api/rooms/{roomId}/roster` - get roster
- `GET /api/rooms/{roomId}/speakers` - list speakers
- `POST /api/rooms/{roomId}/speakers/{speakerGrantId}/revoke` - revoke

**File Locations:**
- `backend/src/Modules/Moderation/Api/Controllers/RosterController.cs`
- `backend/src/Modules/Moderation/Api/Controllers/SpeakersController.cs`

**Acceptance:**
- All endpoints functional
- Authorization enforced

---

### T520: Moderation API - Room Controls [P]
**User Story:** US-5.5, US-5.6, US-5.7
**Priority:** P1
**Effort:** 8 pts
**Dependencies:** T515, T516, T517

Create REST endpoints for room controls.

**Deliverables:**
- `POST /api/rooms/{roomId}/moderator/handover` - transfer role
- `POST /api/rooms/{roomId}/lock` - lock room
- `DELETE /api/rooms/{roomId}/participants/{participantId}` - remove participant

**File Locations:**
- `backend/src/Modules/Moderation/Api/Controllers/RoomControlsController.cs`

**Acceptance:**
- All endpoints functional
- Moderator authorization enforced

---

### T521: Frontend: Hand Raise Button & Queue [P]
**User Story:** US-5.1
**Priority:** P2
**Effort:** 8 pts
**Dependencies:** T518

Create UI for hand raising.

**Deliverables:**
- `frontend/src/features/moderation/components/HandRaiseButton.tsx`
- Queue display component
- Real-time updates via SignalR
- Position and estimated wait time

**File Locations:**
- `frontend/src/features/moderation/components/HandRaiseButton.tsx`
- `frontend/src/features/moderation/components/HandRaiseQueue.tsx`

**Acceptance:**
- Button renders correctly
- Queue shows in real-time
- Position updates on changes

---

### T522: Frontend: Moderator Controls [P]
**User Story:** US-5.2, US-5.5, US-5.6, US-5.7
**Priority:** P2
**Effort:** 10 pts
**Dependencies:** T519, T520

Create UI for moderator actions.

**Deliverables:**
- Moderator toolbar with buttons: approve/reject hand raise, revoke speaker, lock room, remove participant
- Confirmation dialogs for destructive actions
- Real-time status updates

**File Locations:**
- `frontend/src/features/moderation/components/ModeratorToolbar.tsx`
- `frontend/src/features/moderation/components/HandRaiseQueueActions.tsx`

**Acceptance:**
- Toolbar renders
- Actions functional
- Status updates reflected

---

## Phase 6: Integration Tests

### T523: Moderation Integration Tests [P]
**User Story:** All
**Priority:** P1
**Effort:** 21 pts
**Dependencies:** All tasks

Write comprehensive tests.

**Deliverables:**
- Tests for hand raise queue (FIFO, position, expiry)
- Tests for speaker approval/rejection
- Tests for speaker grant expiry
- Tests for moderator handover
- Tests for room locking
- Tests for participant removal
- Tests for authorization checks
- Tests for concurrency (simultaneous actions)

**File Locations:**
- `backend/src/Modules/Moderation.Tests/Integration/`

**Acceptance:**
- All scenarios tested
- Coverage > 80%
- Edge cases handled
- Concurrency tests included

---

## Success Metrics

- Hand raise queue FIFO accurate
- Speaker approval → publish within 1 second
- Revocation → blocked within 1 second
- Authorization: 100% of unauthorized actions rejected
- Roster consistent within 5 minutes
- All actions persisted before response
- Broadcasting: 100% of participants receive events within 100ms
- Audit trail: 100% of moderation actions logged
- Idempotency: duplicate requests produce same result
- Scalability: 500+ participants, 1000+ concurrent hand raises system-wide
