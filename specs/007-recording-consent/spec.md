# Epic 7: Recording & Consent Module

**Version:** 1.0
**Last Updated:** 2026-04-03
**Status:** Specification
**Module Owner:** Compliance & Recording Management
**Dependencies:** Epic 3 (Rooms), Epic 4 (Realtime Orchestration), Epic 5 (Moderation)

---

## Overview

The Recording & Consent Module manages the lifecycle of audio recordings, both local (client-side) and server-side (via LiveKit Egress). It tracks active recording count, drives announcement logic (recording started/stopped), enforces consent workflows, manages recording manifests and artifacts, and applies retention policies. All recording sessions are auditable, participants are notified, and download scope is configurable per recording.

---

## User Stories

### US-7.1: Participant Registers Local Recording Start/Stop
**Priority:** P0 (Critical)
**Capability:** Participant notifies server when they start or stop local recording (no media uploaded); server updates active recorder counter and announcement state.

**As a** participant
**I want to** record the room locally on my device
**So that** I have my own copy of the audio for review or personal use

**Acceptance Scenarios:**

**Scenario 7.1.1: Register Local Recording Start**
```gherkin
Given Alice has joined a room
And Alice has enabled local recording on her device (e.g., via browser recording API or external tool)
When Alice's client calls POST /rooms/{roomId}/recordings/local with {action: "start", recordingType: "local"}
Then the server creates RecordingSession with:
  - id: unique UUID
  - roomId: room ID
  - participantId: Alice's ID
  - recordingType: "local"
  - status: "Active"
  - startedAt: current timestamp
  - startedBy: Alice's participant ID
And increments active_recorder_counter in Redis: room:{roomId}:active_recorders
And if counter transitions from 0 → 1, triggers announcement: "Recording in progress"
And broadcasts SignalR: {event: "recording.counter.changed", count: 1, recordingTypes: ["local"]}
And returns 201 Created with: {recordingSessionId, status: "Active"}
And the counter update is persisted to RecordingSession table for audit
```

**Scenario 7.1.2: Register Local Recording Stop**
```gherkin
Given Alice has an active local recording session
When Alice's client calls POST /rooms/{roomId}/recordings/{recordingSessionId}/stop with {action: "stop"}
Then the server updates RecordingSession.status = "Completed"
And sets stoppedAt = current timestamp
And decrements active_recorder_counter
And if counter transitions from 1 → 0, triggers announcement: "Recording ended"
And broadcasts SignalR: {event: "recording.counter.changed", count: 0, recordingTypes: []}
And returns 200 OK with: {recordingSessionId, status: "Completed", durationSeconds}
And server logs: {timestamp, action: "local_recording_stopped", participantId: "Alice", durationSeconds}
```

**Scenario 7.1.3: Multiple Local Recordings Simultaneously**
```gherkin
Given Alice and Bob both have local recordings active
When active_recorder_counter = 2
And counter is incremented to 3 when Carol starts local recording
Then counter = 3 (no announcement, already recording)
And announcement only triggers on 0→1 transition
When Alice stops: counter = 2 (no announcement, still recording)
When Bob stops: counter = 1 (no announcement, still recording)
When Carol stops: counter = 0, triggers announcement "Recording ended"
And SignalR broadcasts count after each change
```

**Scenario 7.1.4: Local Recording Session Timeout (Crash/Disconnect)**
```gherkin
Given Alice has active local recording session
When Alice's connection drops before calling stop
And the server doesn't receive stop request for > 30 minutes
Then background job marks session.status = "Lost" or auto-completes
And decrements counter
And audit log records: {action: "local_recording_lost", participantId: "Alice", reason: "no_heartbeat"}
```

---

### US-7.2: Moderator Starts Server-Side Recording via LiveKit
**Priority:** P0 (Critical)
**Capability:** Moderator initiates server recording using LiveKit Egress; recording session is tracked and announcement triggered.

**As a** moderator
**I want to** start a server recording of the entire room
**So that** I have an official recording of the discussion that's managed by the platform

**Acceptance Scenarios:**

**Scenario 7.2.1: Start Server Recording**
```gherkin
Given Carol is the room moderator
When Carol calls POST /rooms/{roomId}/recordings with {action: "start_server_recording"}
Then the server validates Carol is moderator (authorization check)
And calls LiveKit Egress API: POST /egress/room with {room: "room123", preset: "AUDIO_ONLY", output: {filepath: "path/to/minIO"}}
And LiveKit returns egress session ID and confirms recording started
And server creates RecordingSession with:
  - id: unique UUID (or LiveKit egress ID)
  - roomId: room ID
  - recordingType: "server"
  - status: "Active"
  - startedAt: current timestamp
  - startedBy: Carol's participant ID
  - egressSessionId: from LiveKit
And persists to database: Recording.RecordingSession table
And increments active_recorder_counter (if counter 0→1, triggers announcement "Recording in progress")
And broadcasts SignalR: {event: "recording.counter.changed", count: 1, recordingTypes: ["server"]}
And returns 201 Created with: {recordingSessionId, egressSessionId}
And server logs: {timestamp, action: "server_recording_started", moderatorId: "Carol", egressSessionId}
```

**Scenario 7.2.2: Stop Server Recording**
```gherkin
Given Carol has started a server recording
When Carol calls POST /rooms/{roomId}/recordings/{recordingSessionId}/stop
Then the server validates Carol is moderator
And calls LiveKit Egress API: DELETE /egress/{egressSessionId} to stop recording
And LiveKit confirms recording stopped and provides: duration, size, status
And server updates RecordingSession:
  - status: "Stopping" (intermediate state while file is being written)
  - stoppedAt: current timestamp
And decrements active_recorder_counter
And broadcasts: {event: "recording.counter.changed", count: updated_count, recordingTypes}
And after a brief delay (e.g., 2-3 seconds for LiveKit to finalize file), updates status = "Completed"
And broadcasts: {event: "file.ready", recordingId, fileName, downloadUrl, duration, participants_recorded}
And returns 200 OK with: {recordingSessionId, status: "Stopping"}
```

**Scenario 7.2.3: Server Recording Failure**
```gherkin
Given Carol has started server recording
When LiveKit fails to write file to storage (e.g., MinIO unavailable)
Or LiveKit egress process crashes
And server detects failure (via polling egress status or webhook)
Then server updates RecordingSession.status = "Failed"
And decrements active_recorder_counter
And broadcasts: {event: "recording.failed", recordingSessionId, reason: "egress_failure"}
And notifies Carol (moderator): "Recording failed. Please try again."
And audit log records: {timestamp, action: "server_recording_failed", reason}
```

**Scenario 7.2.4: Non-Moderator Attempts Server Recording - Rejected**
```gherkin
Given Bob is a panelist (not moderator)
When Bob calls POST /rooms/{roomId}/recordings with {action: "start_server_recording"}
Then the server returns 403 Forbidden with error "only_moderator_can_record"
And no recording is started
And audit log records: {timestamp, action: "unauthorized_recording_attempt", participantId: "Bob"}
```

---

### US-7.3: Active Recorder Counter Drives Announcement Logic
**Priority:** P0 (Critical)
**Capability:** Counter transitions (0→1 and 1→0) trigger automatic announcements; all participants are notified.

**As a** participant
**I want to** know when recording starts and stops
**So that** I can adjust my behavior if needed and am aware of recording status

**Acceptance Scenarios:**

**Scenario 7.3.1: Counter 0→1 Triggers Announcement**
```gherkin
Given active_recorder_counter = 0 (no recordings active)
When Alice registers local recording: counter increments to 1
Then orchestration detects transition 0→1
And broadcasts SignalR event: {event: "announcement.changed", announcement: "recording_started", timestamp, recordingTypes: ["local"]}
And all participants receive announcement
And UI shows notification: "Recording in progress"
And optionally plays audio notification: "This session is being recorded"
And servers record: {timestamp, action: "recording_announcement_started", triggerType: "counter_transition"}
```

**Scenario 7.3.2: Counter 1→0 Triggers Announcement**
```gherkin
Given active_recorder_counter = 1 (Alice has local recording)
When Alice stops local recording: counter decrements to 0
Then orchestration detects transition 1→0
And broadcasts: {event: "announcement.changed", announcement: "recording_stopped", timestamp}
And all participants receive announcement
And UI updates to remove "Recording in progress" indicator
And server logs: {timestamp, action: "recording_announcement_stopped", triggerType: "counter_transition"}
```

**Scenario 7.3.3: Mixed Recording Types - Announcement Logic**
```gherkin
Given counter starts at 0
When Alice registers local recording (counter = 1, announcement: recording_started with type "local")
When Carol starts server recording (counter = 2, no announcement, just counter.changed event)
And when Alice stops (counter = 1, no announcement, still recording)
And when Carol stops (counter = 0, announcement: recording_stopped)
And recordingTypes = ["local", "server"] during the period both active
And announcements show recordingTypes in event payload for clarity
```

**Scenario 7.3.4: Counter Consistency During Rapid Changes**
```gherkin
Given multiple participants start/stop recordings within 100ms
When counter increments/decrements in rapid succession
Then counter value is always accurate (Redis atomic operations)
And announcements may be consolidated or queued to avoid spam
And final state accurately reflects reality (no missed or duplicate transitions)
```

---

### US-7.4: Consent Workflow - Pre-Join Acknowledgement
**Priority:** P0 (Critical)
**Capability:** Participant acknowledges that recording may occur before joining; consent event is logged for audit and compliance.

**As a** platform administrator
**I want to** ensure participants consent to possible recording
**So that** I comply with regulations and have audit trail of consent

**Acceptance Scenarios:**

**Scenario 7.4.1: Consent Prompt Before Room Join**
```gherkin
Given a room is configured: recordingConsent = true
When participant Alice joins via invite link
Then client displays consent dialog before allowing join:
  "This room may be recorded. By continuing, you acknowledge recording may occur. Do you consent?"
And Alice must click "I consent" or "Decline"
When Alice clicks "I consent" and submits POST /rooms/{roomId}/join with invite token
Then server creates ConsentEvent with:
  - id: unique UUID
  - roomId: room ID
  - participantId: Alice's ID
  - consentType: "recording"
  - consentStatus: "Accepted"
  - consentedAt: current timestamp
  - ipAddress: client IP (for audit)
  - userAgent: client user agent (for audit)
And persists to database: Recording.ConsentEvent table
And allows Alice to join (normal flow continues)
And broadcasts: {event: "participant.consented", participantId: "Alice", consentType: "recording"}
```

**Scenario 7.4.2: Decline Consent - Access Denied**
```gherkin
Given Alice sees consent prompt
When Alice clicks "Decline" or closes dialog
And Alice's client does NOT call POST /rooms/{roomId}/join
Then Alice is not added to the room
And audit log records: {timestamp, action: "consent_declined", participantId: "Alice"}
And server may notify moderator (future: logging of rejections)
```

**Scenario 7.4.3: Consent Already Recorded - Skip Prompt**
```gherkin
Given Alice previously consented to recording in this room
And ConsentEvent exists with consentStatus = "Accepted" from past join
When Alice rejoins the room within same session or later
Then consent prompt is NOT shown again (or shown as reminder, policy-dependent)
And new join proceeds without additional consent collection
(Note: consent is per-room, not global; different rooms may have different policies)
```

**Scenario 7.4.4: Room Without Recording Consent Policy**
```gherkin
Given a room is configured: recordingConsent = false
When participant Bob joins
Then no consent prompt is shown
And no ConsentEvent is created
And room may still record, but no explicit consent is logged
```

---

### US-7.5: Recording Manifest and Artifact Management
**Priority:** P0 (Critical)
**Capability:** Server recording generates manifest with metadata; artifact is stored in MinIO; manifest is retrievable.

**As a** moderator
**I want to** access recording metadata and download the recording
**So that** I can manage, share, or archive the recording

**Acceptance Scenarios:**

**Scenario 7.5.1: Generate Recording Manifest on Completion**
```gherkin
Given server recording has completed
And LiveKit confirms file is ready in MinIO
When server receives webhook or polling confirmation from LiveKit
Then orchestration creates RecordingManifest with:
  - id: unique UUID
  - recordingSessionId: FK to RecordingSession
  - roomId: room ID
  - fileName: e.g., "room123_2026-04-03_10-05-30.m4a"
  - fileSize: bytes (from LiveKit)
  - duration: seconds
  - startedAt: room start time
  - endedAt: room end time (actual recording may be shorter)
  - participants_recorded: [list of participant IDs/names who were in room during recording]
  - tracks: {audio: {codec, bitrate, sampleRate}, video: {codec, bitrate, resolution}} (audio-only for Phase 1)
  - format: "m4a" (audio container, configurable)
  - artifactUrl: MinIO path to file
  - checksumSha256: for integrity verification
  - createdAt: current timestamp
And persists to database: Recording.RecordingManifest table
And uploads manifest JSON to MinIO for archival
```

**Scenario 7.5.2: Recording Artifact Upload to MinIO**
```gherkin
Given LiveKit Egress has written recording to MinIO
When LiveKit confirms recording completion
Then server verifies file exists and is readable
And server creates or updates RecordingArtifact:
  - id: unique UUID
  - recordingId: FK to RecordingManifest
  - storageType: "minIO"
  - bucketName: "artifacts"
  - objectPath: /recordings/{roomId}/{fileName}
  - fileSize: bytes
  - checksumSha256: computed by server
  - uploadedAt: current timestamp
  - expiresAt: current timestamp + retentionDays
And broadcasts: {event: "file.ready", recordingId, fileName, downloadUrl, duration}
And moderator can download via GET /rooms/{roomId}/recordings/{recordingId}/download with pre-signed URL
```

**Scenario 7.5.3: Retrieve Recording Manifest**
```gherkin
Given a server recording has completed and manifest exists
When moderator Carol calls GET /rooms/{roomId}/recordings/{recordingId}/manifest
Then server returns RecordingManifest JSON with all metadata:
  {
    id, fileName, fileSize, duration, startedAt, endedAt,
    participants_recorded: ["Alice", "Bob", "Carol"],
    tracks: {audio: {codec: "aac", bitrate: "128k", ...}},
    downloadUrl: (pre-signed MinIO URL),
    expiresAt: manifest expiry
  }
And authorization: only moderator or owner of room can access
```

**Scenario 7.5.4: Download Link Expiry**
```gherkin
Given Carol downloads a recording via pre-signed URL
When download link is pre-signed with expiry = 24 hours
Then download works for 24 hours after URL generation
When 24 hours have passed
Then URL is no longer valid
And Carol sees error: "Download link expired"
And Carol can request a new download link (regenerates pre-signed URL)
```

---

### US-7.6: Download Scope Selection
**Priority:** P1 (High)
**Capability:** Moderator configures who can download recording (all participants, moderator-only, specific users).

**As a** moderator
**I want to** control who can download the recording
**So that** I can manage access and privacy appropriately

**Acceptance Scenarios:**

**Scenario 7.6.1: Moderator Sets Download Scope on Recording Completion**
```gherkin
Given server recording has completed
And file is ready for download
When Carol (moderator) calls POST /rooms/{roomId}/recordings/{recordingId}/download-scope with {scope: "all" | "moderator_only" | "specific_users"}
Then the server updates RecordingManifest.downloadScope = scope
And optionally allowed_users = ["Alice", "Bob"] if scope = "specific_users"
And persists to database
And broadcasts: {event: "recording.download_scope_changed", recordingId, scope, allowedUsers}
```

**Scenario 7.6.2: Download Scope - All Participants Can Access**
```gherkin
Given recording downloadScope = "all"
When any participant calls GET /rooms/{roomId}/recordings/{recordingId}/download
Then server authorizes: all participants can download
And returns download URL (pre-signed)
```

**Scenario 7.6.3: Download Scope - Moderator Only**
```gherkin
Given recording downloadScope = "moderator_only"
When Alice (panelist) calls GET /rooms/{roomId}/recordings/{recordingId}/download
Then server checks Alice's role vs. moderator role
And returns 403 Forbidden
When Carol (moderator) calls GET /rooms/{roomId}/recordings/{recordingId}/download
Then server authorizes and returns URL
```

**Scenario 7.6.4: Download Scope - Specific Users**
```gherkin
Given recording downloadScope = "specific_users" with allowedUsers = ["Alice", "Bob"]
When Alice calls GET /rooms/{roomId}/recordings/{recordingId}/download
Then server checks if Alice in allowedUsers
And authorizes (returns URL)
When Carol calls GET /rooms/{roomId}/recordings/{recordingId}/download
Then server checks if Carol in allowedUsers
And returns 403 Forbidden (Carol is not in list)
When moderator updates allowedUsers to include Carol
Then Carol can now download
```

---

### US-7.7: Recording Retention Policy Enforcement
**Priority:** P1 (High)
**Capability:** Recordings are automatically deleted per tenant retention policy; compliance holds can prevent deletion.

**As a** platform operator
**I want to** enforce data retention policies automatically
**So that** storage costs are managed and compliance requirements are met

**Acceptance Scenarios:**

**Scenario 7.7.1: Recording Auto-Deletion by Retention Policy**
```gherkin
Given a tenant configures: recordingRetentionDays = 30
And a server recording is completed and stored in MinIO
When 30 days have elapsed since recording completion
And background retention cleanup job runs (daily)
Then the job identifies RecordingArtifact where (now - createdAt) > retentionDays
And deletes file from MinIO: DELETE /artifacts/{objectPath}
And soft-deletes database records: set deletedAt timestamp
Or hard-deletes if policy requires permanent removal (GDPR)
And audit log records: {timestamp, action: "recording_deleted_by_retention", recordingId, retentionDays}
```

**Scenario 7.7.2: Legal Hold Prevents Deletion**
```gherkin
Given a recording is placed on legal hold: RecordingManifest.legalHold = true
When retention cleanup job runs
Then the job SKIPS this recording (legal hold takes precedence)
And file remains in MinIO indefinitely
And audit log records: {timestamp, action: "retention_skipped_legal_hold", recordingId}
(Note: Legal hold is a Phase 2 feature; Phase 1 no legal hold support)
```

**Scenario 7.7.3: Tenant-Specific Retention Override**
```gherkin
Given default tenant recordingRetentionDays = 30
And specific room has custom retentionDays = 90
When retention job evaluates room's recordings
Then job uses room-specific retention (90 days) instead of tenant default
And recordings deleted after 90 days
```

---

### US-7.8: Recording Session State Machine
**Priority:** P0 (Critical)
**Capability:** RecordingSession has well-defined state transitions; invalid transitions are prevented.

**As a** system operator
**I want to** ensure recording state is always valid
**So that** I can rely on state for business logic and auditing

**Acceptance Scenarios:**

**Scenario 7.8.1: State Transitions**
```gherkin
Recording Session state machine:

Pending → Active (start recording)
Pending → Cancelled (user cancels before starting)

Active → Stopping (stop requested)
Active → Failed (error during recording)

Stopping → Completed (file finalized successfully)
Stopping → Failed (error while finalizing)

Completed (final state - recording is done)
Failed (final state - recording failed)
Cancelled (final state - not recorded)

Examples:
- Local recording: Pending → Active → Completed (or Failed if crash)
- Server recording: Pending → Active → Stopping → Completed
- Cancellation: can only happen in Pending state
```

**Scenario 7.8.2: Invalid State Transitions Rejected**
```gherkin
Given recording is in state = "Completed"
When API call attempts to update state to "Active" (invalid)
Then server rejects with error "invalid_state_transition"
And state remains "Completed"
And audit log records: {timestamp, action: "invalid_state_transition", from: "Completed", to: "Active", recordingId}
```

---

## Functional Requirements

### F7.1: Local Recording Registration
- **F7.1.1:** POST /rooms/{roomId}/recordings/local with {action: "start"} registers local recording start
- **F7.1.2:** Endpoint must accept participantId implicitly from auth token (no client-provided participantId)
- **F7.1.3:** Server creates RecordingSession: recordingType = "local", status = "Active"
- **F7.1.4:** No media is uploaded; server only tracks registration
- **F7.1.5:** POST /rooms/{roomId}/recordings/{recordingSessionId}/stop with {action: "stop"} deregisters recording
- **F7.1.6:** Deregistration updates status = "Completed", sets stoppedAt
- **F7.1.7:** RecordingSession persisted to database: Recording.RecordingSession table
- **F7.1.8:** Active recording counter must be incremented on start, decremented on stop
- **F7.1.9:** Authorization: any participant can register their own recording (no moderator-only restriction)
- **F7.1.10:** Idempotency: if participant calls start twice without stop, second call is rejected with error "recording_already_active"

### F7.2: Server Recording via LiveKit Egress
- **F7.2.1:** POST /rooms/{roomId}/recordings with {action: "start_server_recording"} initiates server recording
- **F7.2.2:** Authorization: only current room moderator can start server recording
- **F7.2.3:** Server calls LiveKit Egress API: POST /egress/room with room name and output config
- **F7.2.4:** LiveKit configuration: preset = "AUDIO_ONLY" (Phase 1), format = "m4a", bitrate = "128k" (configurable)
- **F7.2.5:** Output destination: MinIO with path /recordings/{roomId}/{sessionId}_{timestamp}
- **F7.2.6:** Server creates RecordingSession: recordingType = "server", status = "Active", egressSessionId from LiveKit
- **F7.2.7:** POST /rooms/{roomId}/recordings/{recordingSessionId}/stop with {action: "stop"} stops server recording
- **F7.2.8:** Stop calls LiveKit Egress API: DELETE /egress/{egressSessionId}
- **F7.2.9:** Stop updates RecordingSession.status = "Stopping" (intermediate)
- **F7.2.10:** After LiveKit confirms file ready (via webhook or polling), update status = "Completed"
- **F7.2.11:** Only one server recording can be active per room at a time (enforced at creation)
- **F7.2.12:** If recording already active, start request is rejected with error "recording_already_active"

### F7.3: Active Recorder Counter
- **F7.3.1:** Counter stored in Redis: key = room:{roomId}:active_recorders
- **F7.3.2:** Counter is atomic integer (Redis INCR/DECR operations)
- **F7.3.3:** Increment on local recording start or server recording start
- **F7.3.4:** Decrement on local recording stop or server recording stop
- **F7.3.5:** Counter must never go negative (defensive check on decrement; log error if detected)
- **F7.3.6:** Counter value persisted to RecordingSession table for audit trail
- **F7.3.7:** Counter state transitions (0→1 and 1→0) trigger announcements (F7.4)
- **F7.3.8:** Counter accessible via GET /rooms/{roomId}/recordings/counter for monitoring

### F7.4: Announcement Logic
- **F7.4.1:** Orchestration monitors counter transitions (via event listener or polling)
- **F7.4.2:** On transition 0→1: broadcast announcement.changed event with announcement = "recording_started", recordingTypes = list of active types
- **F7.4.3:** On transition 1→0: broadcast announcement.changed event with announcement = "recording_stopped"
- **F7.4.4:** Broadcast via SignalR room group within 100ms
- **F7.4.5:** No announcement for transitions where counter stays > 1 (still recording)
- **F7.4.6:** recordingTypes field in event shows ["local"], ["server"], or ["local", "server"]

### F7.5: Consent Event Management
- **F7.5.1:** ConsentEvent table in database: Recording.ConsentEvent
- **F7.5.2:** Fields: id, roomId, participantId, consentType ("recording"), consentStatus ("Accepted" | "Declined"), consentedAt, ipAddress, userAgent, userAgent
- **F7.5.3:** Pre-join consent collection: if room.recordingConsent = true, client must show prompt and collect consent before join
- **F7.5.4:** POST /rooms/{roomId}/join must include optional {consentStatus: "Accepted"} or caller is assumed to have declined
- **F7.5.5:** Server creates ConsentEvent upon join with provided consentStatus
- **F7.5.6:** If consentStatus = "Declined", room join is rejected with 403 Forbidden (before join processing)
- **F7.5.7:** ConsentEvent persisted immediately (before participant state update)
- **F7.5.8:** Audit log: all consent events logged with timestamp and details
- **F7.5.9:** Room.recordingConsent field (boolean) configurable per room or inherited from tenant default

### F7.6: Recording Manifest
- **F7.6.1:** RecordingManifest table: Recording.RecordingManifest
- **F7.6.2:** Fields: id, recordingSessionId, roomId, fileName, fileSize, duration, startedAt, endedAt, participants_recorded (JSON array), tracks (JSON object), format, artifactUrl, checksumSha256, downloadScope, allowedUsers (JSON array), createdAt
- **F7.6.3:** Manifest created when server recording transitions from "Stopping" to "Completed"
- **F7.6.4:** participants_recorded: list of all participants who were in room during recording (snapshot at recording start and end)
- **F7.6.5:** tracks: JSON object with codec, bitrate, sampleRate for audio (and video in future)
- **F7.6.6:** artifactUrl: MinIO path to recording file
- **F7.6.7:** checksumSha256: computed by server after file upload for integrity verification
- **F7.6.8:** Manifest accessible via GET /rooms/{roomId}/recordings/{recordingId}/manifest (authorization check)
- **F7.6.9:** Manifest JSON also uploaded to MinIO for archival (immutable record)

### F7.7: Recording Artifact Storage
- **F7.7.1:** RecordingArtifact table: Recording.RecordingArtifact
- **F7.7.2:** Fields: id, recordingId, storageType ("minIO"), bucketName, objectPath, fileSize, checksumSha256, uploadedAt, expiresAt, deletedAt
- **F7.7.3:** File upload to MinIO: /recordings/{roomId}/{fileName}
- **F7.7.4:** Server verifies file exists and checksumSha256 matches manifest
- **F7.7.5:** expiresAt = uploadedAt + tenant.recordingRetentionDays
- **F7.7.6:** RecordingArtifact persisted upon file upload confirmation
- **F7.7.7:** Retention cleanup: daily job soft-deletes RecordingArtifact where (now > expiresAt)
- **F7.7.8:** Hard-deletion from MinIO happens after configurable grace period (e.g., 7 days after soft-delete)

### F7.8: Download Scope Control
- **F7.8.1:** RecordingManifest.downloadScope field: "all" | "moderator_only" | "specific_users"
- **F7.8.2:** RecordingManifest.allowedUsers field: JSON array of participantIds (only if scope = "specific_users")
- **F7.8.3:** POST /rooms/{roomId}/recordings/{recordingId}/download-scope with {scope, allowedUsers} updates scope
- **F7.8.4:** Authorization: only moderator can change download scope
- **F7.8.5:** GET /rooms/{roomId}/recordings/{recordingId}/download checks caller's permission against scope
- **F7.8.6:** If scope = "all": all room participants can download
- **F7.8.7:** If scope = "moderator_only": only current/past moderators can download (check role)
- **F7.8.8:** If scope = "specific_users": only participants in allowedUsers can download
- **F7.8.9:** Unauthorized downloads return 403 Forbidden
- **F7.8.10:** Download endpoint returns pre-signed MinIO URL (valid for 24 hours)

### F7.9: Retention Policy and Cleanup
- **F7.9.1:** Tenant configuration: recordingRetentionDays (default 30 days)
- **F7.9.2:** Room-specific override: recordingRetentionDays (optional, null means use tenant default)
- **F7.9.3:** Daily cleanup job (runs at configurable time, e.g., 2 AM UTC)
- **F7.9.4:** Job identifies RecordingArtifact where (now - uploadedAt) > effective_retentionDays
- **F7.9.5:** Soft-delete: set RecordingArtifact.deletedAt = now
- **F7.9.6:** Hard-delete: schedule file deletion from MinIO for after grace period (7 days)
- **F7.9.7:** ConsentEvent and chat messages also subject to retention (if applicable, policy-dependent)
- **F7.9.8:** Audit log: all deletions recorded with timestamp, recordingId, reason, retentionDays
- **F7.9.9:** No actual user-facing recovery after deletion (deletion is permanent after grace period)
- **F7.9.10:** Future: Legal hold mechanism (Phase 2) to prevent deletion

### F7.10: Recording State Machine
- **F7.10.1:** RecordingSession.status field: "Pending" | "Active" | "Stopping" | "Completed" | "Failed" | "Cancelled"
- **F7.10.2:** Valid transitions:
  - Pending → Active (start)
  - Pending → Cancelled (user cancels before start)
  - Active → Stopping (stop requested, for server recordings)
  - Active → Failed (error)
  - Stopping → Completed (file ready)
  - Stopping → Failed (finalization error)
  - Local recording: Pending → Active → Completed directly (no Stopping state)
- **F7.10.3:** Invalid transitions rejected with error "invalid_state_transition"
- **F7.10.4:** Audit log: all state transitions recorded
- **F7.10.5:** Server validates state before allowing operations (e.g., cannot stop a Cancelled recording)

### F7.11: LiveKit Integration
- **F7.11.1:** Egress API configuration: LiveKit API key/secret in config; SSL/TLS enabled
- **F7.11.2:** Egress request payload: room name, preset (AUDIO_ONLY), output config (filepath to MinIO)
- **F7.11.3:** Response parsing: extract egress sessionId, status, and confirm recording started
- **F7.11.4:** Polling or webhook: monitor egress status for completion (recommend: webhook for real-time)
- **F7.11.5:** Webhook validation: verify LiveKit signature before processing
- **F7.11.6:** On egress completion: file is in MinIO, server downloads manifest from LiveKit (duration, size, tracks)
- **F7.11.7:** Error handling: if egress fails, update RecordingSession.status = "Failed", notify moderator

### F7.12: Broadcast Events
- **F7.12.1:** Event: recording.counter.changed when counter increments/decrements
  - Payload: {count: number, recordingTypes: ["local" | "server"], timestamp}
- **F7.12.2:** Event: announcement.changed when counter 0→1 or 1→0
  - Payload: {announcement: "recording_started" | "recording_stopped", timestamp, recordingTypes}
- **F7.12.3:** Event: file.ready when server recording completes
  - Payload: {recordingId, fileName, downloadUrl, duration, participants_recorded, timestamp}
- **F7.12.4:** Event: recording.failed when recording fails
  - Payload: {recordingId, reason, timestamp}
- **F7.12.5:** Broadcast scope: room group for counter/announcement/file.ready; moderator only for failures

---

## Key Entities

### RecordingSession
```typescript
{
  id: string;                           // UUID
  roomId: string;                       // FK to Rooms.Room
  recordingType: "local" | "server";
  status: "Pending" | "Active" | "Stopping" | "Completed" | "Failed" | "Cancelled";
  startedAt: DateTime;
  stoppedAt?: DateTime;
  startedBy: string;                    // Participant ID who initiated
  stoppedBy?: string;                   // Participant ID who stopped (if applicable)
  egressSessionId?: string;             // LiveKit egress ID (server recordings only)
  durationSeconds?: number;             // (completed - startedAt)
  failureReason?: string;               // If status = Failed
  createdAt: DateTime;
  updatedAt: DateTime;
  deletedAt?: DateTime;                 // Soft-delete
}
```

### RecordingManifest
```typescript
{
  id: string;                           // UUID
  recordingSessionId: string;           // FK to RecordingSession
  roomId: string;                       // FK to Rooms.Room
  fileName: string;                     // e.g., "room123_2026-04-03_10-05-30.m4a"
  fileSize: number;                     // bytes
  duration: number;                     // seconds
  startedAt: DateTime;                  // When room/recording started
  endedAt: DateTime;                    // When room/recording ended
  participants_recorded: string[];      // ["participantId1", "participantId2", ...]
  tracks: {
    audio: {
      codec: string;                    // "aac"
      bitrate: string;                  // "128k"
      sampleRate: number;               // 48000
      channels: number;                 // 1 (mono) or 2 (stereo)
    };
  };
  format: string;                       // "m4a"
  artifactUrl: string;                  // MinIO path
  checksumSha256: string;               // For integrity
  downloadScope: "all" | "moderator_only" | "specific_users";
  allowedUsers?: string[];              // If scope = "specific_users"
  legalHold: boolean;                   // Default false (Phase 2)
  createdAt: DateTime;
  deletedAt?: DateTime;
}
```

### RecordingArtifact
```typescript
{
  id: string;                           // UUID
  recordingId: string;                  // FK to RecordingManifest
  storageType: "minIO";
  bucketName: string;                   // "artifacts"
  objectPath: string;                   // "/recordings/{roomId}/{fileName}"
  fileSize: number;
  checksumSha256: string;
  uploadedAt: DateTime;
  expiresAt: DateTime;                  // uploadedAt + retentionDays
  deletedAt?: DateTime;                 // Soft-delete timestamp
  hardDeletedAt?: DateTime;             // Hard-delete (file removed from MinIO)
}
```

### ConsentEvent
```typescript
{
  id: string;                           // UUID
  roomId: string;                       // FK to Rooms.Room
  participantId: string;                // FK to Identity.Participant
  consentType: "recording";             // Extensible for future consent types
  consentStatus: "Accepted" | "Declined";
  consentedAt: DateTime;
  ipAddress: string;                    // For audit
  userAgent: string;                    // For audit
  createdAt: DateTime;
}
```

---

## Success Criteria

1. **Counter Accuracy:** Active recorder counter matches ground truth (no missed increments/decrements) 100% of the time
2. **Announcement Latency:** 0→1 and 1→0 transitions trigger announcements within 100ms (P95)
3. **Broadcast Delivery:** 100% of room participants receive announcement events within 100ms
4. **Consent Compliance:** 100% of joiners in consent-enabled rooms have ConsentEvent logged before room access
5. **Recording Manifest Generation:** Manifest created and available within 5 seconds of recording completion
6. **Download Link Validity:** Pre-signed URLs valid for exactly 24 hours
7. **Retention Cleanup:** 100% of recordings deleted after expiry (no stragglers)
8. **State Transition Validity:** 100% of invalid state transitions rejected
9. **Idempotency:** Duplicate start/stop requests result in same state (no double-counting)
10. **Audit Trail:** 100% of recording events logged (start, stop, delete, consent, scope change)

---

## Edge Cases

1. **Counter Decrement on Already-Stopped Recording:** Participant calls stop on session already stopped; decrement only once (check status before decrement)
2. **Announcement During Room Ending:** Counter transitions as room is ending; announcement broadcasts best-effort (may not reach all)
3. **Server Recording Interruption:** LiveKit egress crashes mid-recording; status = "Failed", counter decremented, announcement may be delayed
4. **Multiple Start/Stop Rapid Succession:** Participant calls start/stop/start very quickly; counter must remain accurate (atomic operations)
5. **Consent Collection Race:** Participant joins while consent prompt is loading; join completes before consent submission; ConsentEvent may be created after join (eventual consistency acceptable)
6. **Recording Deletion During Download:** Cleanup job deletes recording while participant is downloading; download fails with error (expected)
7. **Moderator Changes Download Scope After Download Started:** Participant begins download with scope "all", moderator changes to "moderator_only" mid-download; download completes successfully (pre-signed URL already valid)
8. **Retention Override on Completed Recording:** Room inherits tenant retention, then room-specific value is set; only new setting applies to future calculations
9. **Local Recording Session Timeout:** Participant's recording session is marked lost after 30 minutes of no heartbeat; counter decremented
10. **Egress File Already Exists in MinIO:** LiveKit attempts to write file to same path that already exists; MinIO overwrites or returns error (LiveKit should use unique paths)

---

## Assumptions

1. **LiveKit Egress Available:** LiveKit server has egress/recording capability; API endpoint is accessible and authenticated
2. **MinIO Configured:** MinIO is operational for recording artifact storage; bucket "artifacts" pre-created
3. **Participant Identity Consistent:** participantId in auth token matches LiveKit identity (verified at join time)
4. **Room State Available:** Room state (active, ended, locked) available from Rooms module
5. **Moderator Authority:** Moderator role verified by auth middleware; Recording service trusts JWT claim
6. **Realtime Orchestration Available:** Orchest ration module provides announcement and broadcast APIs
7. **Redis Available:** Counter stored in Redis; data loss tolerance < 1 second (not critical)
8. **Database Persistence:** SQL Server operational for recording session/manifest/consent storage
9. **Clock Synchronization:** Server clocks synchronized; timestamps comparable across services
10. **Policy Configuration:** Tenant recordingRetentionDays and recordingConsent settings are configurable and available

---

## Open Questions & Decisions

1. **Audio Format:** M4A (AAC) for server recording? Or WAV for lossless? (Recommend: M4A for storage efficiency; WAV in Phase 2)
2. **Bitrate Configuration:** Fixed 128kbps or configurable per tenant? (Recommend: configurable with default 128k)
3. **Local Recording Timeout:** How long before unconfirmed local recording is marked lost? (Recommend: 30 minutes)
4. **Consent Re-prompt:** If participant rejoins same room, re-prompt for consent? (Recommend: No re-prompt within same session)
5. **Consent Granularity:** Recording consent or broader "session recording" consent? (Recommend: "recording" only for Phase 1)
6. **Multi-Room Recording:** Can recording span multiple rooms? (Recommend: No; each room has independent recording)
7. **Recording Start Lag:** Live lag time before LiveKit Egress begins writing? (Recommend: < 2 seconds; document limitation)
8. **Failed Recording Notification:** Who is notified of recording failure - moderator only or all participants? (Recommend: Moderator only; log as event)
9. **Download Scope Change History:** Track all scope changes for audit? (Recommend: Yes; add changedAt, changedBy to manifest)
10. **Retention Legal Hold:** When/how is recording placed on legal hold? (Recommend: Phase 2 feature; admin-only action)

