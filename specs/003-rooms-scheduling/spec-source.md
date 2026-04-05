# Epic 3: Rooms & Scheduling Module

**Version:** 1.0
**Status:** Specification
**Last Updated:** 2026-04-03
**Owner:** Product / Engineering

---

## Overview

This epic implements the core audio room management and scheduling system for Muntada. It provides room template creation, recurring room series, room lifecycle state machine, participant management, and moderator assignment. This module builds on Identity (Epic 1) and Tenancy (Epic 2) to provide multi-tenant, user-aware room experiences.

### Scope

- Room template creation (name, description, settings)
- Recurring room series (daily/weekly/custom patterns with iCal support)
- Room occurrence generation from series
- Room invites (email, direct link, bulk)
- Room membership validation and access control
- Room lifecycle state machine: Draft → Scheduled → Live → Grace → Ended → Archived
- Grace period: starts when last moderator disconnects, configurable timeout (default 5 minutes)
- Moderator assignment (one moderator per room, with handover capability)
- Room settings (max participants, listen-only guests, recording policy, transcription)
- Participant real-time tracking
- Room analytics (participant count, duration, quality metrics)

### Dependencies

- **Identity Module (Epic 1)** for user accounts, sessions, authentication, guest access
- **Tenancy Module (Epic 2)** for tenant context, plan limits, feature toggles, retention policies
- **Shared Kernel (Epic 0)** for base entity types, ID generation, event publishing
- **LiveKit OSS** for WebRTC infrastructure and media streaming
- **SQL Server** for persistent room and scheduling data
- **Redis** for real-time participant tracking and state
- **RabbitMQ** for integration events (RoomCreated, RoomLive, RoomEnded, etc.)

---

## User Stories

### US-3.1: Room Template Creation
**Priority:** P1
**Story Points:** 13
**Owner:** Product / Rooms

As a room organizer, I want to create a room template so that I can reuse settings and quickly create rooms with consistent configurations.

#### Acceptance Criteria

**Given** I am a tenant member
**When** I navigate to "Rooms" and click "Create Template"
**Then** I see a form with fields:
- Template name (required, 3-100 chars)
- Description (optional)
- Max participants (default 100, must be <= plan limit)
- Allow guest access via magic link (toggle, default: true)
- Allow recording (toggle, default: true if plan allows)
- Allow transcription (toggle, default: false)
- Moderator role requirements (optional, e.g., "Owner+ only")

**Given** I fill in the form
**When** I click "Create template"
**Then** validation occurs:
- Name is unique within tenant
- Max participants <= plan limit
- Recording allowed only if plan permits

**Given** validation passes
**When** the template is saved
**Then** a RoomTemplate record is created with:
- `Id` (opaque ID, prefix `tpl_`)
- `TenantId`
- `Name`
- `Description`
- `Settings: { MaxParticipants, AllowGuests, AllowRecording, AllowTranscription }`
- `CreatedBy` (current user)
- `CreatedAt`
- `UpdatedAt`

**Given** the template is created
**When** I navigate to the room creation flow
**Then** I can select this template to quickly create rooms with the same settings

**Given** I want to edit a template
**When** I click "Edit" on an existing template
**Then** I can modify all fields except name (name is immutable for audit)

#### Definition of Done
- Room template creation endpoint: `POST /api/tenants/{tenantId}/room-templates`
- Room template retrieval endpoint: `GET /api/tenants/{tenantId}/room-templates/{templateId}`
- Room template update endpoint: `PATCH /api/tenants/{tenantId}/room-templates/{templateId}`
- Template list endpoint: `GET /api/tenants/{tenantId}/room-templates`
- Validation for plan limits
- Integration event: `RoomTemplateCreated` published
- Unit and integration tests
- Audit logging of template changes

---

### US-3.2: Room Series & Recurring Schedules
**Priority:** P1
**Story Points:** 21
**Owner:** Product / Scheduling

As a room organizer, I want to create recurring room series (daily standups, weekly syncs) so that I don't have to manually create each room.

#### Acceptance Criteria

**Given** I am creating a recurring room series
**When** I select a template and configure recurrence
**Then** I see options:
- Recurrence pattern (Daily, Weekly, Monthly, Custom)
- For Daily: every N days
- For Weekly: which days of week (Mon-Sun)
- For Monthly: which day of month or relative (e.g., "first Monday")
- For Custom: custom iCal recurrence rule (e.g., `FREQ=WEEKLY;BYDAY=MO,WE`)

**Given** I select "Weekly on Monday and Wednesday"
**When** I click "Create series"
**Then** a RoomSeries record is created with:
- `Id` (opaque ID, prefix `ser_`)
- `TenantId`
- `TemplateId` (or inherit settings from template)
- `RecurrenceRule` (iCal RRULE format)
- `StartsAt` (series start date)
- `EndsAt` (optional, series end date)
- `Status: Active`
- `CreatedBy`
- `CreatedAt`

**Given** the series is created
**When** a background job runs (every hour or on-demand)
**Then** it generates RoomOccurrence records for upcoming 30 days:
- `Id` (opaque ID, prefix `occ_`)
- `RoomSeriesId`
- `TenantId`
- `ScheduledAt` (the occurrence date/time)
- `Status: Scheduled`
- `Settings` (inherited from series template)

**Given** occurrences are generated
**When** I view the calendar
**Then** I see all upcoming room occurrences with times

**Given** I want to modify a single occurrence
**When** I click "Edit" on a specific occurrence
**Then** I can change:
- Title (override for this instance)
- Max participants
- Cancelled (mark as cancelled for this instance)

**Given** I want to edit the series
**When** I click "Edit series"
**Then** I can change recurrence pattern and affect future occurrences

**Given** I want to end a series
**When** I click "End series"
**Then** the series status transitions to Ended and no future occurrences are generated

#### Definition of Done
- Room series creation endpoint: `POST /api/tenants/{tenantId}/room-series`
- Room series update endpoint: `PATCH /api/tenants/{tenantId}/room-series/{seriesId}`
- Room occurrence generation job (background task)
- Recurrence rule parsing (use iCal.NET or similar)
- Calendar view integration
- Single occurrence override support
- Integration event: `RoomSeriesCreated`, `RoomOccurrenceGenerated` published
- Unit and integration tests (recurrence patterns, edge cases)

---

### US-3.3: Room Lifecycle & State Machine
**Priority:** P1
**Story Points:** 21
**Owner:** Product / Rooms

As a room organizer, I want to understand and manage the room lifecycle so that I can see which rooms are upcoming, live, and completed.

#### Acceptance Criteria

**Given** a room occurrence is created
**When** I view its details
**Then** its status is "Scheduled" and shows:
- `ScheduledAt` (when the room is scheduled)
- `Status: Scheduled` (not yet started)
- Invite link
- Participant list (empty until room goes live)

**Given** the scheduled time arrives
**When** the first participant connects to LiveKit
**Then** the room status transitions to `Live` and:
- Room connects to LiveKit instance
- `LiveStartedAt` is recorded
- Integration event `RoomLive` is published
- Participants are tracked in real-time (Redis)

**Given** the room is live
**When** participants join and leave
**Then** the participant count is updated in real-time (WebSocket broadcast)

**Given** the moderator disconnects while the room is live
**When** the disconnect event is received
**Then** the room transitions to `Grace` status:
- Grace period starts (default 5 minutes, configurable per room)
- A countdown timer is shown to remaining participants: "Host will end room in 5:00"
- If moderator reconnects, status returns to `Live`
- If grace period expires, room transitions to `Ended`

**Given** the room is in Grace period
**When** the moderator reconnects
**Then** the room status returns to `Live` and grace timer is cancelled

**Given** all participants disconnect
**When** the last participant leaves
**Then** the room may transition to `Grace` (if moderator left) or `Ended` (if last participant left while in Live)

**Given** the room is ended
**When** the status transitions to Ended
**Then** recordings are finalized, `LiveEndedAt` is recorded, and integration event `RoomEnded` is published

**Given** a room is ended
**When** it's retained per policy (e.g., 90 days)
**Then** it transitions to `Archived` after retention expires

**Given** an archived room
**When** retention expires
**Then** the room and related data (recordings, chat, files) are deleted

#### State Machine Diagram

```
    [Draft] (on creation, if manual override needed)
       ↓
   [Scheduled] (once scheduled time is set)
       ↓
   [Live] (first participant joins)
       ↓ (moderator disconnects)
   [Grace] (5 minute timeout or moderator reconnects)
       ↓ (timeout expires or last participant leaves)
   [Ended] (manually or by timeout)
       ↓ (per retention policy)
   [Archived] (after retention period, before deletion)
       ↓ (deletion job runs)
   [Deleted]
```

#### Definition of Done
- Room occurrence status transitions (state machine)
- Grace period implementation (timer, countdown)
- Real-time status broadcast (WebSocket)
- Status update endpoint: `PATCH /api/tenants/{tenantId}/room-occurrences/{occurrenceId}/status`
- Integration event: `RoomLive`, `RoomEnded`, `RoomArchived` published
- Unit and integration tests for all state transitions
- Edge case handling (duplicate transitions, invalid transitions)

---

### US-3.4: Room Invites & Participant Access
**Priority:** P1
**Story Points:** 13
**Owner:** Product / Rooms

As a room organizer, I want to invite participants to a room so that they can join when the room is live.

#### Acceptance Criteria

**Given** I have a scheduled room
**When** I click "Invite participants"
**Then** I see options:
- Invite by email (enter one or more email addresses)
- Copy direct join link (shareable link, requires login)
- Generate guest magic link (shareable link, listen-only)
- Bulk invite from CSV

**Given** I enter email addresses
**When** I click "Send invites"
**Then** a RoomInvite record is created per invitee with:
- `Id` (opaque ID, prefix `inv_`)
- `RoomOccurrenceId`
- `InvitedEmail`
- `InvitedBy` (current user)
- `Status: Pending` (until claimed)
- `InviteToken` (unique token for tracking)
- `CreatedAt`

**Given** the invite is created
**When** an email is sent
**Then** the invitee receives an email with:
- Room title and time
- Sender's name
- Join link: `/rooms/{occurrenceId}/join?token=...`

**Given** the invitee clicks the join link
**When** the token is validated
**Then** if the user is authenticated:
  - They are added to the room participant list
  - RoomInvite status transitions to `Accepted`
  - They are connected to the LiveKit room
  - If user not authenticated, they are prompted to log in or use guest link

**Given** I share a direct join link
**When** an authenticated user clicks it
**Then** they can join immediately (if room is Scheduled or Live)

**Given** I share a guest magic link
**When** an unauthenticated user clicks it
**Then** they join with guest permissions (listen-only) without logging in

**Given** I want to revoke an invite
**When** I click "Revoke" on a pending invite
**Then** the invite token is invalidated and the invitee cannot use the link

#### Definition of Done
- Room invite creation endpoint: `POST /api/tenants/{tenantId}/room-occurrences/{occurrenceId}/invites`
- Room invite list endpoint: `GET /api/tenants/{tenantId}/room-occurrences/{occurrenceId}/invites`
- Room join endpoint: `GET /api/tenants/{tenantId}/room-occurrences/{occurrenceId}/join`
- Invite token validation
- Email delivery integration
- Bulk invite from CSV
- Invite revocation
- Integration event: `RoomInviteSent`, `ParticipantJoined` published
- Unit and integration tests

---

### US-3.5: Moderator Assignment & Handover
**Priority:** P1
**Story Points:** 13
**Owner:** Product / Rooms

As a room organizer, I want to designate a moderator who controls the room so that I can ensure someone responsible is managing the session.

#### Acceptance Criteria

**Given** I am creating a room
**When** I select the moderator
**Then** the room requires a designated moderator before it can be scheduled

**Given** a room has a designated moderator
**When** I view room details
**Then** I see:
- Moderator name and avatar
- Badge indicating "Moderator" role in participant list
- Moderator controls: (mute all, end room, record, etc.)

**Given** the moderator connects to the room
**When** they join LiveKit
**Then** they are marked as the active moderator and controls are enabled

**Given** the moderator disconnects
**When** they leave the room
**Then** the room transitions to Grace period (as per US-3.3)

**Given** the room is in Grace period
**When** another user wants to take over as moderator
**Then** they can click "Become moderator" if they are in the moderator list (pre-authorized)

**Given** the moderator is handed over
**When** the new moderator accepts
**Then** the room returns to Live status and the new moderator has controls

**Given** no one is available to take over
**When** grace period expires
**Then** the room automatically ends

**Given** I want to change the moderator before the room goes live
**When** the room is in Scheduled status
**Then** I can update the moderator designation

#### Definition of Done
- Moderator assignment during room creation
- Moderator designation update endpoint: `PATCH /api/tenants/{tenantId}/room-occurrences/{occurrenceId}/moderator`
- Moderator handover endpoint: `POST /api/tenants/{tenantId}/room-occurrences/{occurrenceId}/moderator/handover`
- Moderator controls enforced in LiveKit integration
- Grace period respects moderator status
- Integration event: `ModeratorAssigned`, `ModeratorHandover` published
- Unit and integration tests

---

### US-3.6: Participant Tracking & Real-Time Status
**Priority:** P1
**Story Points:** 13
**Owner:** Product / Rooms

As a room organizer, I want to see real-time participant count and status so that I know who is in the room and manage engagement.

#### Acceptance Criteria

**Given** a room is live
**When** I view the room details
**Then** I see:
- Participant count (e.g., "12 participants")
- List of active participants with names and avatars
- Participant audio/video status (muted/unmuted)
- Participant join/leave timeline (last 5 events)

**Given** a participant joins the room
**When** the LiveKit webhook is received
**Then** the participant is added to RoomParticipantState:
- `Id` (opaque ID, prefix `rps_`)
- `RoomOccurrenceId`
- `UserId` (or null for guests)
- `DisplayName`
- `JoinedAt`
- `LeftAt` (null if still in room)
- `AudioState` (Muted, Unmuted)
- `VideoState` (Off, On)

**Given** participants are in the room
**When** I open the participant list
**Then** it updates in real-time via WebSocket:
- Participant joins → participant added to list
- Participant leaves → participant removed from list
- Audio/video changes → status updated

**Given** the room is in Grace period
**When** I view participant list
**Then** the countdown timer is shown and participant count is visible

**Given** I want to see participant analytics
**When** I click "Analytics" (post-room)
**Then** I see:
- Total participants joined
- Peak concurrent participants
- Participant dwell time (duration in room per user)
- Audio/video participation rate

#### Definition of Done
- Real-time participant tracking in Redis
- WebSocket broadcast for participant list updates
- LiveKit webhook handler for join/leave/media events
- Participant state store (RoomParticipantState)
- Participant list endpoint: `GET /api/tenants/{tenantId}/room-occurrences/{occurrenceId}/participants`
- Analytics endpoint: `GET /api/tenants/{tenantId}/room-occurrences/{occurrenceId}/analytics`
- Real-time updates via WebSocket or Server-Sent Events
- Integration event: `ParticipantJoined`, `ParticipantLeft` published
- Unit and integration tests

---

### US-3.7: Room Settings & Recording Configuration
**Priority:** P1
**Story Points:** 13
**Owner:** Product / Rooms

As a room organizer, I want to configure recording and transcription settings so that I can capture and archive room content.

#### Acceptance Criteria

**Given** I am creating or editing a room
**When** I configure settings
**Then** I see options:
- Allow recording (toggle, default per plan)
- Recording visibility (Private to organizer, Shared with participants, Public)
- Allow transcription (toggle, default false, plan permitting)
- Transcription language (dropdown, default English)
- Auto-start recording (toggle, default false)

**Given** I enable recording
**When** the room goes live
**Then** recording is available:
- Recording button is visible to moderator
- Recording status is shown (red dot indicator)
- Participants are notified "This room is being recorded"

**Given** the room is being recorded
**When** the recording completes
**Then** the recording file is stored in MinIO with metadata:
- `Id` (opaque ID, prefix `rec_`)
- `RoomOccurrenceId`
- `TenantId`
- `S3Path` (MinIO bucket path)
- `Duration` (seconds)
- `FileSize` (bytes)
- `Status: Processing` (queued for transcription/post-processing)
- `CreatedAt`

**Given** recording is complete
**When** transcription is enabled
**Then** a transcription job is queued:
- Transcription provider (speech-to-text API)
- Transcription language
- Output stored as VTT and transcript text file

**Given** transcription completes
**When** the recording is available
**Then** users can:
- Download the recording file
- View or download transcript
- Search within transcript
- Stream recording (not download, if visibility = Private)

**Given** the retention policy expires
**When** the cleanup job runs
**Then** the recording and transcript are deleted (if applicable)

#### Definition of Done
- Room settings update endpoint: `PATCH /api/tenants/{tenantId}/room-occurrences/{occurrenceId}/settings`
- Recording lifecycle management (start, stop, process)
- MinIO upload integration
- Transcription service integration (Google Speech-to-Text, AssemblyAI, etc.)
- Recording retrieval endpoint: `GET /api/tenants/{tenantId}/room-occurrences/{occurrenceId}/recording`
- Transcript endpoint: `GET /api/tenants/{tenantId}/room-occurrences/{occurrenceId}/transcript`
- Recording visibility enforcement
- Retention policy enforcement
- Integration event: `RecordingStarted`, `RecordingCompleted`, `TranscriptionCompleted` published
- Unit and integration tests

---

## Functional Requirements

### Room Management

**FR-3.1:** Room templates define reusable settings for quick room creation. Templates are tenant-scoped and immutable (name cannot change for audit).

**FR-3.2:** Rooms are either one-off occurrences or generated from a recurring series. Room occurrences are scheduled for specific dates/times.

**FR-3.3:** A room occurrence must have a scheduled start time (`ScheduledAt`). The actual start time (`LiveStartedAt`) is recorded when the first participant joins.

**FR-3.4:** A room must have at least one designated moderator. The moderator is required to be present (or available via handover) for the room to remain in Live status.

### Room Lifecycle

**FR-3.5:** Rooms follow a strict state machine: Draft → Scheduled → Live → Grace → Ended → Archived → Deleted. Invalid transitions are rejected.

**FR-3.6:** A room transitions to Live when the first participant connects to LiveKit. It transitions to Grace when the last moderator disconnects (if other participants remain). Grace period has a configurable timeout (default 5 minutes).

**FR-3.7:** A room transitions to Ended when the grace period expires, all participants leave, or moderator explicitly ends the room.

**FR-3.8:** Archived rooms are retained per the tenant's retention policy, then deleted. Audit logs of deletion are kept for 7 years (PDPL compliance).

### Participant Management

**FR-3.9:** Participants are tracked by LiveKit webhook events (join, leave, media state change). A RoomParticipantState record is created for each participant per room occurrence.

**FR-3.10:** Participants must be invited or have a valid join link/magic link. Unauthenticated guests require a valid magic link and join with listen-only permissions.

**FR-3.11:** Participant permissions are enforced based on role (Member, Guest, Moderator). Members can speak/listen. Guests can only listen. Moderators have full controls.

**FR-3.12:** Participant presence is real-time (Redis). Joining/leaving is broadcast to all connected clients via WebSocket.

### Recording & Transcription

**FR-3.13:** Recording is initiated by moderator or auto-started based on settings. Recording is stored in MinIO S3-compatible storage with configurable visibility (private, shared, public).

**FR-3.14:** Transcription is optional and requires explicit enablement. Transcription language is configurable. Transcription service handles audio-to-text conversion asynchronously.

**FR-3.15:** Recordings are retained per the tenant's retention policy. Expired recordings are deleted per grace period (soft-delete then hard-delete).

### Scheduling & Recurrence

**FR-3.16:** Room series use iCal recurrence rules (RRULE) for maximum flexibility. Supported patterns: daily, weekly, monthly, and custom.

**FR-3.17:** Occurrences are generated in advance (default 30 days ahead) by a background job. Job is idempotent and can be re-run safely.

**FR-3.18:** Single occurrence overrides are supported (cancel, reschedule, modify settings). Overrides affect only that occurrence, not the series.

### Integration Events

**FR-3.19:** The following integration events shall be published to RabbitMQ:
- `RoomTemplateCreated`
- `RoomSeriesCreated`
- `RoomOccurrenceGenerated`
- `RoomScheduled`
- `RoomLive`
- `RoomGraceStarted`
- `RoomEnded`
- `RoomArchived`
- `ParticipantJoined`
- `ParticipantLeft`
- `ModeratorAssigned`
- `ModeratorHandover`
- `RecordingStarted`
- `RecordingCompleted`
- `TranscriptionCompleted`

---

## Key Entities

### RoomTemplate

```csharp
public class RoomTemplate : AggregateRoot<RoomTemplateId>
{
    public TenantId TenantId { get; set; }
    public string Name { get; set; }                       // Immutable
    public string? Description { get; set; }
    public RoomSettings Settings { get; set; }
    public UserId CreatedBy { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }
}

public class RoomSettings
{
    public int MaxParticipants { get; set; }               // Per plan limit
    public bool AllowGuestAccess { get; set; }
    public bool AllowRecording { get; set; }
    public bool AllowTranscription { get; set; }
    public string? DefaultTranscriptionLanguage { get; set; }
    public bool AutoStartRecording { get; set; }
}
```

### RoomSeries

```csharp
public class RoomSeries : AggregateRoot<RoomSeriesId>
{
    public TenantId TenantId { get; set; }
    public RoomTemplateId TemplateId { get; set; }
    public string Title { get; set; }
    public string RecurrenceRule { get; set; }             // iCal RRULE
    public DateTime StartsAt { get; set; }
    public DateTime? EndsAt { get; set; }
    public RoomSeriesStatus Status { get; set; }           // Active, Ended
    public UserId CreatedBy { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }
}

public enum RoomSeriesStatus
{
    Active,
    Ended
}
```

### RoomOccurrence

```csharp
public class RoomOccurrence : AggregateRoot<RoomOccurrenceId>
{
    public TenantId TenantId { get; set; }
    public RoomSeriesId? RoomSeriesId { get; set; }        // Null if one-off
    public string Title { get; set; }
    public DateTime ScheduledAt { get; set; }
    public DateTime? LiveStartedAt { get; set; }
    public DateTime? LiveEndedAt { get; set; }
    public RoomOccurrenceStatus Status { get; set; }       // Draft, Scheduled, Live, Grace, Ended, Archived
    public ModeratorAssignment? ModeratorAssignment { get; set; }
    public RoomSettings Settings { get; set; }
    public int GracePeriodSeconds { get; set; }            // Default 300 (5 min)
    public DateTime? GraceStartedAt { get; set; }
    public UserId CreatedBy { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }
    public ICollection<RoomInvite> Invites { get; set; }
    public ICollection<RoomParticipantState> Participants { get; set; }
}

public enum RoomOccurrenceStatus
{
    Draft,
    Scheduled,
    Live,
    Grace,
    Ended,
    Archived
}

public class ModeratorAssignment
{
    public UserId UserId { get; set; }
    public DateTime AssignedAt { get; set; }
    public DateTime? DisconnectedAt { get; set; }
}
```

### RoomInvite

```csharp
public class RoomInvite : AggregateRoot<RoomInviteId>
{
    public RoomOccurrenceId RoomOccurrenceId { get; set; }
    public string? InvitedEmail { get; set; }              // For email invites
    public UserId? InvitedUserId { get; set; }             // For user invites
    public string InviteToken { get; set; }                // For tracking
    public RoomInviteStatus Status { get; set; }           // Pending, Accepted, Revoked, Expired
    public UserId InvitedBy { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime ExpiresAt { get; set; }                // 7 days default
}

public enum RoomInviteStatus
{
    Pending,
    Accepted,
    Revoked,
    Expired
}
```

### RoomParticipantState

```csharp
public class RoomParticipantState : Entity<RoomParticipantStateId>
{
    public RoomOccurrenceId RoomOccurrenceId { get; set; }
    public UserId? UserId { get; set; }                    // Null for guests
    public string DisplayName { get; set; }
    public DateTime JoinedAt { get; set; }
    public DateTime? LeftAt { get; set; }
    public MediaState AudioState { get; set; }             // Muted, Unmuted
    public MediaState VideoState { get; set; }             // Off, On
    public string? LiveKitParticipantId { get; set; }      // LiveKit track ID
}

public enum MediaState
{
    Muted,
    Unmuted,
    Off,
    On
}
```

### Recording

```csharp
public class Recording : AggregateRoot<RecordingId>
{
    public RoomOccurrenceId RoomOccurrenceId { get; set; }
    public TenantId TenantId { get; set; }
    public string S3Path { get; set; }                     // MinIO bucket path
    public long FileSizeBytes { get; set; }
    public long DurationSeconds { get; set; }
    public RecordingStatus Status { get; set; }            // Processing, Ready, Failed
    public RecordingVisibility Visibility { get; set; }    // Private, Shared, Public
    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }
    public List<Transcript>? Transcripts { get; set; }
}

public enum RecordingStatus
{
    Processing,
    Ready,
    Failed
}

public enum RecordingVisibility
{
    Private,
    Shared,
    Public
}

public class Transcript
{
    public string Language { get; set; }
    public string S3Path { get; set; }                     // MinIO path for VTT/SRT
    public TranscriptStatus Status { get; set; }
    public DateTime CreatedAt { get; set; }
}

public enum TranscriptStatus
{
    Processing,
    Ready,
    Failed
}
```

---

## Success Criteria

- Room templates can be created and reused in < 1 minute
- Recurring series generate occurrences correctly (tested with various patterns)
- Room state transitions are atomic and never invalid
- Participants join/leave in real-time (< 2 second latency)
- Moderator handover works seamlessly with no room interruption
- Recordings are captured, transcribed, and retrievable within 5 minutes of end
- Room analytics are accurate (participant count, duration, etc.)
- All edge cases are handled (concurrent moderator disconnects, grace period expiry during invite)
- Retention policies delete old rooms automatically with zero data loss
- Performance: room creation < 200ms, participant join < 500ms, state transitions < 100ms

---

## Edge Cases

1. **Concurrent Moderator Disconnect:** If all moderators disconnect simultaneously, grace period starts. If any moderator reconnects within grace period, room returns to Live.

2. **Single Participant Room:** If room has only one participant and it's not the moderator, room can end if they leave. If participant is moderator, grace period applies.

3. **Recording During Grace Period:** If recording is ongoing when grace period starts, recording continues. If grace period expires and last participant leaves, recording ends.

4. **Moderator Handover During Grace:** If moderator hands over to another user during grace, grace period is cancelled and room returns to Live.

5. **Invites Sent After Room Ends:** Invites sent to ended/archived rooms are rejected. Only Scheduled and Live rooms accept new invites.

6. **Series Occurrence Modification:** If an occurrence is overridden (cancelled), it doesn't affect future occurrences. Cancel affects only that instance.

7. **Recurrence Rule Parsing Error:** If iCal RRULE is invalid, series creation fails with clear error. Admin can provide example RRULEs.

8. **Recording File Not Found:** If MinIO is unavailable when recording is queued, recording is retried with exponential backoff. After max retries, admin is alerted.

9. **Transcription Service Timeout:** If transcription takes > 2 hours, it's marked as failed. User can retry or request support.

10. **Grace Period Edge Case:** If participant joins during grace period, grace period is cancelled (room is back to Live). If they leave, grace restarts.

---

## Assumptions

1. **LiveKit Cluster:** A self-hosted LiveKit cluster is provisioned with API access. LiveKit webhooks are configured and routable to backend.

2. **Webhook Authenticity:** LiveKit webhook requests are verified using shared secret (HMAC-SHA256). Requests without valid signature are rejected.

3. **S3-Compatible Storage:** MinIO is configured with S3-compatible API. All recording/file uploads use S3 SDK.

4. **Transcription Service:** A transcription API (Google Cloud Speech-to-Text, AssemblyAI, etc.) is configured. API key is stored securely in Kubernetes Secrets.

5. **Email Delivery:** Email invites are sent via configured SMTP service. Delivery is assumed within 30 seconds.

6. **iCal Library:** A mature iCal parsing library (iCal.NET, or similar) is used for recurrence rule parsing and validation.

7. **WebSocket Infrastructure:** SignalR or Socket.IO is used for real-time participant updates. All clients maintain a WebSocket connection to receive broadcast updates.

8. **Audit Logging:** All room state changes, participant events, and moderator actions are logged via Serilog with structured context.

9. **Background Jobs:** Recurrence generation, retention cleanup, transcription queuing, and recording finalization are handled by Hangfire or Quartz background job scheduler.

10. **Compliance:** PDPL audit log retention (7 years) is enforced. All user activity in rooms is logged for compliance.

---

## Implementation Notes

- **State Machine:** Use a state machine library (NStateMachine, Stateless) to enforce valid transitions. Document all transitions in code comments.
- **Real-Time Updates:** Use SignalR with connection groups per room (group = `room-{occurrenceId}`). Broadcast all state changes to group.
- **LiveKit Integration:** Use LiveKit C# SDK for API calls. Webhook handler should be idempotent (retry-safe).
- **Recording:** Leverage LiveKit's built-in recording (track-based recording) or external recorder (ffmpeg). Store metadata in SQL Server, files in MinIO.
- **Transcription:** Queue transcription jobs to RabbitMQ or use a service like AssemblyAI with polling. Implement exponential backoff for retries.
- **Concurrency:** Use optimistic locking (Version field) on RoomOccurrence for concurrent state transitions. Failing transactions are retried client-side.
- **Testing:** Unit tests for state machine transitions, integration tests for LiveKit webhook handling, load tests for concurrent participant joins.
- **Documentation:** Provide OpenAPI docs for all endpoints, architecture diagrams for room lifecycle, and runbooks for common operational tasks.
