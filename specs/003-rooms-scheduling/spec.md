# Feature Specification: Rooms & Scheduling

**Feature Branch**: `003-rooms-scheduling`
**Created**: 2026-04-04
**Status**: Draft
**Input**: User description: "Rooms and Scheduling Module - audio room management and scheduling system for Muntada"

## Clarifications

### Session 2026-04-04

- Q: Single moderator or multi-moderator per room? → A: Single moderator — exactly one active moderator per room; handover transfers control to one other person.
- Q: One-off rooms vs series-only? → A: Support both — organizers can create a one-off room (standalone occurrence with no series parent) or a recurring series.
- Q: How are timezones handled for scheduling? → A: Organizer's timezone is stored with the series/occurrence; times are displayed in each viewer's local timezone; DST transitions are handled correctly for recurring events.
- Q: Which tenant roles can create rooms and templates? → A: Only Admin and Owner roles can create templates, rooms, and series; regular members can only join rooms they are invited to.

## User Scenarios & Testing *(mandatory)*

### User Story 1 - Create and Reuse Room Templates (Priority: P1)

As a room organizer, I want to create room templates with preset configurations (name, max participants, guest access, recording, transcription settings) so that I can quickly create rooms with consistent settings without re-entering the same options each time.

**Why this priority**: Templates are the foundation for all room creation. Without templates, every room must be configured from scratch, making the experience tedious and error-prone.

**Independent Test**: Can be fully tested by creating a template, verifying it appears in the template list, and then using it to pre-fill room creation. Delivers value by reducing room setup time to under 1 minute.

**Acceptance Scenarios**:

1. **Given** I am a tenant Admin or Owner, **When** I create a room template with name, description, and settings, **Then** the template is saved and available for reuse within my tenant.
2. **Given** a template exists, **When** I create a new room, **Then** I can select the template to pre-populate all settings.
3. **Given** a template exists, **When** I edit it, **Then** all fields except the name can be modified (name is immutable for audit).
4. **Given** I set max participants higher than my plan limit, **When** I try to save, **Then** validation rejects the template with a clear error.
5. **Given** my plan does not allow recording, **When** I try to enable recording on a template, **Then** the option is disabled or rejected.

---

### User Story 2 - Create a One-Off Room (Priority: P1)

As a room organizer, I want to create a single, non-recurring room for an ad-hoc meeting so that I can quickly schedule a one-time session without setting up a series.

**Why this priority**: Ad-hoc meetings are the most common use case. Requiring a recurring series for every room adds unnecessary friction.

**Independent Test**: Can be fully tested by creating a standalone room with a template, setting a scheduled time and moderator, and verifying the occurrence is created without a series parent.

**Acceptance Scenarios**:

1. **Given** I am a tenant Admin or Owner, **When** I create a room using a template and set a specific date/time, **Then** a standalone room occurrence is created (not linked to any series).
2. **Given** I create a one-off room, **When** I view it in the calendar, **Then** it appears as a single event with no series indicator.
3. **Given** I create a one-off room, **When** I assign a moderator and send invites, **Then** the room follows the same lifecycle and invite flow as series-generated occurrences.

---

### User Story 3 - Schedule Recurring Room Series (Priority: P1)

As a room organizer, I want to create recurring room series (daily standups, weekly syncs) with flexible recurrence patterns so that rooms are automatically scheduled without manual intervention.

**Why this priority**: Recurring rooms are essential for regular meetings. Without this, organizers must manually create each occurrence, which defeats the purpose of a scheduling system.

**Independent Test**: Can be fully tested by creating a series with a weekly pattern, verifying occurrences are generated for the next 30 days, and confirming each occurrence appears on the calendar with correct dates/times.

**Acceptance Scenarios**:

1. **Given** I select a template and configure weekly recurrence (e.g., Monday and Wednesday), **When** I create the series, **Then** room occurrences are generated for the next 30 days matching that pattern.
2. **Given** a series exists with generated occurrences, **When** I view the calendar, **Then** all upcoming occurrences are displayed with correct times.
3. **Given** I want to cancel a single occurrence, **When** I mark it as cancelled, **Then** only that instance is cancelled; the series and other occurrences are unaffected.
4. **Given** I edit the series recurrence pattern, **When** I save, **Then** future occurrences are regenerated according to the new pattern.
5. **Given** I end a series, **When** I confirm, **Then** no new occurrences are generated going forward.
6. **Given** a series is active, **When** a background process runs, **Then** it generates new occurrences to always maintain 30 days of upcoming rooms.

---

### User Story 4 - Room Lifecycle Management (Priority: P1)

As a room organizer, I want rooms to follow a clear lifecycle (Scheduled, Live, Grace, Ended, Archived) so that I always know the current state of any room and the system handles transitions automatically.

**Why this priority**: The lifecycle state machine is critical for room reliability. It ensures rooms start, end, and archive predictably, and participants always see accurate status.

**Independent Test**: Can be fully tested by creating a scheduled room, simulating the first participant joining (transition to Live), simulating moderator disconnect (transition to Grace with countdown), and verifying the room ends after the grace period expires.

**Acceptance Scenarios**:

1. **Given** a room occurrence is created, **When** the scheduled time has not arrived, **Then** its status is "Scheduled" with an invite link and scheduled time visible.
2. **Given** the scheduled time arrives and the first participant connects, **When** the connection is established, **Then** the room transitions to "Live" and the actual start time is recorded.
3. **Given** the room is live and the moderator disconnects, **When** other participants remain, **Then** the room transitions to "Grace" with a configurable countdown (default 5 minutes) visible to remaining participants.
4. **Given** the room is in Grace period, **When** the moderator reconnects, **Then** the room returns to "Live" and the countdown is cancelled.
5. **Given** the room is in Grace period, **When** the countdown expires without moderator reconnection, **Then** the room transitions to "Ended" and recordings are finalized.
6. **Given** a room has ended, **When** the retention period expires, **Then** the room transitions to "Archived" and eventually its data is deleted per policy.
7. **Given** a room is in any state, **When** an invalid transition is attempted, **Then** the system rejects it with a clear error.

---

### User Story 5 - Invite Participants to Rooms (Priority: P1)

As a room organizer, I want to invite participants via email, direct link, or guest magic link so that the right people can join my rooms easily.

**Why this priority**: Rooms are useless without participants. Invitations are the primary mechanism for getting people into rooms.

**Independent Test**: Can be fully tested by sending an email invite, verifying the recipient receives it, clicking the join link, and confirming the participant is added to the room.

**Acceptance Scenarios**:

1. **Given** I have a scheduled room, **When** I enter email addresses and send invites, **Then** each invitee receives an email with room details and a join link.
2. **Given** I copy a direct join link, **When** an authenticated user clicks it, **Then** they can join the room immediately (if Scheduled or Live).
3. **Given** I generate a guest magic link, **When** an unauthenticated user clicks it, **Then** they join with listen-only permissions without logging in.
4. **Given** an invite is pending, **When** I revoke it, **Then** the invite token is invalidated and the invitee can no longer use the link.
5. **Given** I upload a CSV file of email addresses, **When** I send bulk invites, **Then** all listed participants receive invitations.
6. **Given** the room has ended, **When** someone tries to use an invite link, **Then** they are informed the room has ended.

---

### User Story 6 - Moderator Assignment and Handover (Priority: P1)

As a room organizer, I want to designate a moderator who controls the room and allow moderator handover so that there is always someone responsible managing the session.

**Why this priority**: A moderator is required for the room lifecycle (Grace period triggers on moderator disconnect). Without moderator management, rooms cannot function properly.

**Independent Test**: Can be fully tested by assigning a moderator during room creation, verifying moderator controls are active when they join, and testing handover to another pre-authorized user during a Grace period.

**Acceptance Scenarios**:

1. **Given** I am creating a room, **When** I designate a moderator, **Then** the room is ready to be scheduled with that moderator assigned.
2. **Given** the moderator connects to the room, **When** they join, **Then** they have full controls (mute all, end room, record).
3. **Given** the moderator disconnects, **When** the room enters Grace period, **Then** a pre-authorized user can click "Become moderator" to take over.
4. **Given** a handover occurs, **When** the new moderator accepts, **Then** the room returns to Live and the new moderator has controls.
5. **Given** the room is in Scheduled status, **When** I want to change the moderator, **Then** I can update the moderator designation before the room goes live.

---

### User Story 7 - Real-Time Participant Tracking (Priority: P1)

As a room organizer, I want to see real-time participant count, presence, and audio/video status so that I can manage engagement during the session.

**Why this priority**: Real-time awareness of who is in the room and their status is essential for moderators to manage sessions effectively.

**Independent Test**: Can be fully tested by joining a live room, verifying the participant list updates in real-time as others join/leave, and confirming audio/video status indicators change correctly.

**Acceptance Scenarios**:

1. **Given** a room is live, **When** I view the participant list, **Then** I see all active participants with names, avatars, and audio/video status.
2. **Given** a participant joins, **When** the join event occurs, **Then** they appear in the participant list within 2 seconds.
3. **Given** a participant leaves, **When** the leave event occurs, **Then** they are removed from the list within 2 seconds.
4. **Given** the room has ended, **When** I view analytics, **Then** I see total participants, peak concurrent count, and per-participant dwell time.

---

### User Story 8 - Recording and Transcription (Priority: P2)

As a room organizer, I want to record room sessions and optionally transcribe them so that content is captured for later review and sharing.

**Why this priority**: Recording and transcription add significant value but are not required for the core room experience. Rooms function without them.

**Independent Test**: Can be fully tested by enabling recording on a room, starting a session, verifying the recording is captured after the room ends, and confirming the recording is downloadable with correct visibility settings.

**Acceptance Scenarios**:

1. **Given** I enable recording on a room, **When** the room goes live, **Then** the moderator can start recording and all participants see a "recording in progress" indicator.
2. **Given** recording is auto-start enabled, **When** the room goes live, **Then** recording begins automatically without moderator action.
3. **Given** the room ends with an active recording, **When** the recording is finalized, **Then** I can download or stream the recording based on visibility settings (private, shared, public).
4. **Given** transcription is enabled, **When** the recording completes, **Then** a transcription job produces a searchable text transcript and VTT file.
5. **Given** the retention policy expires, **When** the cleanup process runs, **Then** recordings and transcripts are deleted.

---

### Edge Cases

- What happens when the moderator disconnects? Grace period starts; if the moderator reconnects within the grace period, the room returns to Live.
- What happens if a participant joins during the Grace period? The grace timer continues; the room returns to Live only when a moderator reconnects.
- What happens if recording is ongoing when Grace period starts? Recording continues through Grace and stops only when the room ends.
- What happens when someone tries to join an ended or archived room? They are shown a clear message that the room has ended.
- What happens if the recurrence rule is invalid? Series creation fails with a clear validation error and example patterns.
- What happens when a single occurrence is cancelled? Only that instance is affected; the series and other occurrences remain unchanged.
- What happens if the recording storage service is unavailable? Recording is retried with backoff; after max retries, the organizer is notified of the failure.
- What happens if transcription takes too long? After a timeout threshold, transcription is marked as failed and the user can retry.
- What happens if a participant joins during Grace period? Grace continues (only moderator reconnection cancels it), but participant count updates in real-time.
- What happens when invite is sent to a room that just ended? The invite is rejected; only Scheduled and Live rooms accept new invites.

## Requirements *(mandatory)*

### Functional Requirements

- **FR-001**: System MUST allow tenant Admins and Owners to create room templates with name, description, max participants, guest access, recording, and transcription settings. Regular members MUST NOT have access to create or edit templates, rooms, or series.
- **FR-002**: System MUST enforce that template names are unique within a tenant and immutable after creation.
- **FR-003**: System MUST validate room settings against the tenant's plan limits (max participants, recording permissions).
- **FR-004**: System MUST allow Admins and Owners to create standalone one-off room occurrences (not linked to any series) with a template, scheduled time, and moderator.
- **FR-005**: System MUST support creating recurring room series with daily, weekly, monthly, and custom recurrence patterns.
- **FR-006**: System MUST store the organizer's timezone with each series and standalone occurrence. Scheduled times MUST be displayed in each viewer's local timezone. Recurring series MUST handle DST transitions correctly (e.g., a 9 AM weekly meeting stays at 9 AM local time across DST changes).
- **FR-007**: System MUST generate room occurrences at least 30 days in advance from active series via a background process.
- **FR-008**: System MUST support single-occurrence overrides (cancel, reschedule, modify settings) without affecting the series.
- **FR-009**: System MUST enforce a strict room lifecycle state machine: Draft, Scheduled, Live, Grace, Ended, Archived. Invalid transitions MUST be rejected.
- **FR-010**: System MUST transition a room to "Live" when the first participant connects and record the actual start time.
- **FR-011**: System MUST transition a room to "Grace" when the moderator disconnects, with a configurable countdown (default 5 minutes).
- **FR-012**: System MUST return a room from "Grace" to "Live" when a moderator reconnects, cancelling the countdown.
- **FR-013**: System MUST transition a room to "Ended" when the grace period expires, all participants leave, or the moderator explicitly ends the room.
- **FR-014**: System MUST support inviting participants by email (single and bulk via CSV), direct join link, and guest magic link.
- **FR-015**: System MUST validate invite tokens and enforce room state (only Scheduled or Live rooms accept joins).
- **FR-016**: System MUST allow guest access via magic link with listen-only permissions (no authentication required).
- **FR-017**: System MUST allow invite revocation, invalidating the invite token immediately.
- **FR-018**: System MUST require exactly one designated moderator per room before it can be scheduled. Only one moderator is active at a time; handover transfers control to a single replacement.
- **FR-019**: System MUST support moderator handover to pre-authorized users during Grace period.
- **FR-020**: System MUST track participant presence in real-time, including join/leave events and audio/video state.
- **FR-021**: System MUST broadcast participant list changes to all connected clients within 2 seconds.
- **FR-022**: System MUST support recording with configurable visibility (private to organizer, shared with participants, public).
- **FR-023**: System MUST support optional transcription that produces searchable text and subtitle files.
- **FR-024**: System MUST enforce retention policies, automatically archiving and deleting room data (recordings, transcripts) when the policy expires.
- **FR-025**: System MUST publish integration events for all significant room lifecycle changes (room live, ended, participant joined/left, recording completed, etc.).
- **FR-026**: System MUST provide post-room analytics: total participants, peak concurrent count, per-participant duration, and audio/video participation rates.
- **FR-027**: Participant permissions MUST be enforced based on role: Members can speak and listen, Guests can only listen, Moderators have full controls.

### Key Entities

- **Room Template**: A reusable configuration defining room settings (max participants, guest access, recording, transcription). Tenant-scoped with immutable name.
- **Room Series**: A recurring schedule that generates room occurrences based on recurrence patterns. Stores the organizer's timezone for DST-correct scheduling. Linked to a template for default settings. Can be Active or Ended.
- **Room Occurrence**: A specific instance of a room at a scheduled time. Can be standalone or part of a series. Follows the lifecycle state machine (Draft through Archived).
- **Room Invite**: An invitation to a specific room occurrence, sent via email or link. Has a unique token and status (Pending, Accepted, Revoked, Expired).
- **Moderator Assignment**: The designated moderator for a room occurrence. Required for scheduling. Supports handover to pre-authorized users.
- **Participant State**: Real-time tracking of a participant in a room, including join/leave times, audio/video status, and display name.
- **Recording**: A captured audio/video file from a room session, with configurable visibility and optional linked transcripts.
- **Transcript**: A text representation of a recording in a specified language, available as searchable text and subtitle format.

## Success Criteria *(mandatory)*

### Measurable Outcomes

- **SC-001**: Room organizers can create a template and use it to schedule a room in under 1 minute.
- **SC-002**: Recurring series correctly generate occurrences for all supported patterns (daily, weekly, monthly, custom) with zero missed or duplicate occurrences over 30 days.
- **SC-003**: Room state transitions are atomic and consistent - no room is ever in an invalid state, verified by zero invalid-transition errors in production.
- **SC-004**: Participants see real-time updates (joins, leaves, status changes) within 2 seconds of the event occurring.
- **SC-005**: Moderator handover completes seamlessly with no room interruption - participants experience zero downtime during handover.
- **SC-006**: Recordings are available for download/streaming within 5 minutes of room ending.
- **SC-007**: Transcriptions complete within 10 minutes of recording finalization for sessions up to 2 hours.
- **SC-008**: Room analytics accurately reflect participant count, peak concurrent count, and duration - verified against raw event data.
- **SC-009**: Retention policies execute automatically, deleting expired room data with zero data loss for non-expired rooms.
- **SC-010**: 95% of invite recipients successfully join the room using their invite link on the first attempt.
- **SC-011**: The system supports at least 100 concurrent rooms with up to 100 participants each without degradation in participant tracking or state updates.

## Assumptions

- An existing Identity module provides user authentication, sessions, and guest access capabilities.
- An existing Tenancy module provides tenant context, plan limits, feature toggles, and retention policies.
- A WebRTC infrastructure (LiveKit or equivalent) is provisioned and accessible for media streaming and webhook events.
- S3-compatible object storage is configured for recording file storage.
- A speech-to-text service is available for transcription (configured externally).
- Email delivery infrastructure is available for sending invite emails (delivery within 30 seconds).
- A mature recurrence rule library is available for parsing standard recurrence patterns.
- Real-time communication infrastructure (WebSocket/SignalR) is available for broadcasting participant updates.
- Background job scheduling is available for occurrence generation, retention cleanup, and transcription queuing.
- Compliance requirements (audit log retention for 7 years) are enforced by the platform.
