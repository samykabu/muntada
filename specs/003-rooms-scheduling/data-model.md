# Data Model: Rooms & Scheduling

**Branch**: `003-rooms-scheduling` | **Date**: 2026-04-04
**Schema**: `[rooms]`

## Entity Relationship Overview

```
RoomTemplate 1──* RoomSeries 1──* RoomOccurrence 1──* RoomInvite
                                       │
                                       ├──1 ModeratorAssignment (owned)
                                       ├──* RoomParticipantState
                                       └──* Recording 1──* Transcript (owned)
```

---

## Entities

### RoomTemplate (Aggregate Root)

| Field | Type | Constraints | Notes |
|-------|------|-------------|-------|
| Id | RoomTemplateId | PK, opaque (`tpl_` prefix) | OpaqueIdGenerator |
| TenantId | TenantId | FK (logical, no cross-schema join), required, indexed | Module boundary |
| Name | string | Required, 3-100 chars, unique per tenant, **immutable** | Cannot change after creation (audit) |
| Description | string? | Optional, max 500 chars | |
| Settings | RoomSettings | Required, owned value object | See RoomSettings below |
| CreatedBy | UserId | Required | Logical FK to Identity module |
| CreatedAt | DateTime | Required, UTC | |
| UpdatedAt | DateTime | Required, UTC | |
| Version | int | Optimistic concurrency token | From AggregateRoot base |

**Unique index**: `(TenantId, Name)`

### RoomSettings (Value Object — Owned by RoomTemplate, RoomOccurrence)

| Field | Type | Constraints | Notes |
|-------|------|-------------|-------|
| MaxParticipants | int | Required, 1-10000, <= plan limit | Validated against PlanLimits |
| AllowGuestAccess | bool | Default: true | Guest magic link support |
| AllowRecording | bool | Default: true if plan allows | Validated against plan |
| AllowTranscription | bool | Default: false | Requires recording enabled |
| DefaultTranscriptionLanguage | string? | ISO 639-1 code (e.g., "en", "ar") | Null if transcription disabled |
| AutoStartRecording | bool | Default: false | Auto-record on room Live |

### RoomSeries (Aggregate Root)

| Field | Type | Constraints | Notes |
|-------|------|-------------|-------|
| Id | RoomSeriesId | PK, opaque (`ser_` prefix) | |
| TenantId | TenantId | Required, indexed | |
| TemplateId | RoomTemplateId | FK, required | Settings inherited from template |
| Title | string | Required, 3-200 chars | |
| RecurrenceRule | string | Required, valid iCal RRULE | Parsed by Ical.Net |
| OrganizerTimeZoneId | string | Required, IANA timezone (e.g., `Asia/Riyadh`) | For DST-correct occurrence generation |
| StartsAt | DateTime | Required, UTC | Series start (first occurrence >= this) |
| EndsAt | DateTime? | Optional, UTC | Null = no end date |
| Status | RoomSeriesStatus | Required | `Active`, `Ended` |
| CreatedBy | UserId | Required | |
| CreatedAt | DateTime | Required, UTC | |
| UpdatedAt | DateTime | Required, UTC | |
| Version | int | Optimistic concurrency | |

**Index**: `(TenantId, Status)` — for listing active series per tenant

### RoomOccurrence (Aggregate Root)

| Field | Type | Constraints | Notes |
|-------|------|-------------|-------|
| Id | RoomOccurrenceId | PK, opaque (`occ_` prefix) | |
| TenantId | TenantId | Required, indexed | |
| RoomSeriesId | RoomSeriesId? | FK, nullable | Null for standalone one-off rooms |
| Title | string | Required, 3-200 chars | Can override series title |
| ScheduledAt | DateTime | Required, UTC | Displayed in viewer's local TZ |
| OrganizerTimeZoneId | string | Required, IANA timezone | Inherited from series or set directly |
| LiveStartedAt | DateTime? | UTC | Set on transition to Live |
| LiveEndedAt | DateTime? | UTC | Set on transition to Ended |
| Status | RoomOccurrenceStatus | Required | State machine enforced |
| ModeratorAssignment | ModeratorAssignment | Required (before Scheduled), owned | Single moderator |
| Settings | RoomSettings | Required, owned value object | Can override template settings |
| GracePeriodSeconds | int | Default: 300 (5 min) | Configurable per room |
| GraceStartedAt | DateTime? | UTC | Set on transition to Grace |
| IsCancelled | bool | Default: false | For single-occurrence cancellation |
| CreatedBy | UserId | Required | |
| CreatedAt | DateTime | Required, UTC | |
| UpdatedAt | DateTime | Required, UTC | |
| Version | int | Optimistic concurrency | Critical for concurrent state transitions |

**Indexes**:
- `(TenantId, Status)` — for listing rooms by state
- `(TenantId, ScheduledAt)` — for calendar queries
- `(RoomSeriesId)` — for series occurrence listing

**State Machine** (RoomOccurrenceStatus):

```
Draft → Scheduled          (moderator assigned, time set)
Scheduled → Live           (first participant connects)
Live → Grace               (moderator disconnects)
Grace → Live               (moderator reconnects)
Grace → Ended              (timeout expires, or moderator explicitly ends)
Live → Ended               (moderator explicitly ends, or all leave)
Ended → Archived           (retention policy triggers)
```

Invalid transitions are rejected with `DomainException`.

### ModeratorAssignment (Value Object — Owned by RoomOccurrence)

| Field | Type | Constraints | Notes |
|-------|------|-------------|-------|
| UserId | UserId | Required | Must be Admin or Owner in tenant |
| AssignedAt | DateTime | Required, UTC | |
| DisconnectedAt | DateTime? | UTC | Set when moderator leaves Live room |

### RoomInvite (Entity)

| Field | Type | Constraints | Notes |
|-------|------|-------------|-------|
| Id | RoomInviteId | PK, opaque (`inv_` prefix) | |
| RoomOccurrenceId | RoomOccurrenceId | FK, required, indexed | |
| InvitedEmail | string? | Email format, nullable | For email invites |
| InvitedUserId | UserId? | Nullable | For direct user invites |
| InviteToken | string | Required, unique, indexed | Cryptographic random token |
| Status | RoomInviteStatus | Required | `Pending`, `Accepted`, `Revoked`, `Expired` |
| InviteType | RoomInviteType | Required | `Email`, `DirectLink`, `GuestMagicLink` |
| InvitedBy | UserId | Required | |
| CreatedAt | DateTime | Required, UTC | |
| ExpiresAt | DateTime | Required, UTC | Default: 7 days from creation |

**Unique index**: `(InviteToken)`
**Index**: `(RoomOccurrenceId, Status)` — for listing active invites

**Validation**: Either `InvitedEmail` or `InvitedUserId` must be set (or neither for GuestMagicLink).

### RoomParticipantState (Entity)

| Field | Type | Constraints | Notes |
|-------|------|-------------|-------|
| Id | RoomParticipantStateId | PK, opaque (`rps_` prefix) | |
| RoomOccurrenceId | RoomOccurrenceId | FK, required, indexed | |
| UserId | UserId? | Nullable | Null for guest participants |
| DisplayName | string | Required, max 100 chars | |
| Role | ParticipantRole | Required | `Moderator`, `Member`, `Guest` |
| JoinedAt | DateTime | Required, UTC | |
| LeftAt | DateTime? | UTC | Null if still in room |
| AudioState | MediaState | Required | `Muted`, `Unmuted` |
| VideoState | MediaState | Required | `Off`, `On` |
| LiveKitParticipantId | string? | LiveKit internal ID | For webhook correlation |

**Index**: `(RoomOccurrenceId, LeftAt)` — for listing active participants (LeftAt IS NULL)

### Recording (Aggregate Root)

| Field | Type | Constraints | Notes |
|-------|------|-------------|-------|
| Id | RecordingId | PK, opaque (`rec_` prefix) | |
| RoomOccurrenceId | RoomOccurrenceId | FK, required, indexed | |
| TenantId | TenantId | Required, indexed | For retention policy queries |
| S3Path | string | Required | MinIO bucket path |
| FileSizeBytes | long | Required | |
| DurationSeconds | long | Required | |
| Status | RecordingStatus | Required | `Processing`, `Ready`, `Failed` |
| Visibility | RecordingVisibility | Required | `Private`, `Shared`, `Public` |
| LiveKitEgressId | string? | LiveKit egress job ID | For status tracking |
| CreatedAt | DateTime | Required, UTC | |
| UpdatedAt | DateTime | Required, UTC | |
| Version | int | Optimistic concurrency | |
| Transcripts | List\<Transcript\> | Owned collection | |

### Transcript (Value Object — Owned by Recording)

| Field | Type | Constraints | Notes |
|-------|------|-------------|-------|
| Language | string | Required, ISO 639-1 | e.g., "en", "ar" |
| S3Path | string | Required | MinIO path for VTT file |
| TextS3Path | string | Required | MinIO path for plain text |
| Status | TranscriptStatus | Required | `Processing`, `Ready`, `Failed` |
| CreatedAt | DateTime | Required, UTC | |

---

## Enumerations

| Enum | Values |
|------|--------|
| RoomSeriesStatus | Active, Ended |
| RoomOccurrenceStatus | Draft, Scheduled, Live, Grace, Ended, Archived |
| RoomInviteStatus | Pending, Accepted, Revoked, Expired |
| RoomInviteType | Email, DirectLink, GuestMagicLink |
| ParticipantRole | Moderator, Member, Guest |
| MediaState | Muted, Unmuted, Off, On |
| RecordingStatus | Processing, Ready, Failed |
| RecordingVisibility | Private, Shared, Public |
| TranscriptStatus | Processing, Ready, Failed |

---

## Integration Events (Published to RabbitMQ via MassTransit)

| Event | Trigger | Key Data |
|-------|---------|----------|
| RoomTemplateCreated | Template saved | TemplateId, TenantId, Name |
| RoomSeriesCreated | Series saved | SeriesId, TenantId, RecurrenceRule |
| RoomOccurrenceGenerated | Background job creates occurrence | OccurrenceId, TenantId, ScheduledAt |
| RoomScheduled | Occurrence transitions to Scheduled | OccurrenceId, ScheduledAt |
| RoomLive | First participant connects | OccurrenceId, LiveStartedAt |
| RoomGraceStarted | Moderator disconnects | OccurrenceId, GraceStartedAt, GracePeriodSeconds |
| RoomEnded | Room ends (any trigger) | OccurrenceId, LiveEndedAt |
| RoomArchived | Retention policy triggers | OccurrenceId |
| ParticipantJoined | Participant connects | OccurrenceId, UserId, DisplayName, Role |
| ParticipantLeft | Participant disconnects | OccurrenceId, UserId, LeftAt |
| ModeratorAssigned | Moderator designated | OccurrenceId, UserId |
| ModeratorHandover | Moderator changed | OccurrenceId, FromUserId, ToUserId |
| RoomInviteSent | Invite created | InviteId, OccurrenceId, InvitedEmail |
| RecordingStarted | Recording begins | RecordingId, OccurrenceId |
| RecordingCompleted | Recording finalized | RecordingId, S3Path, DurationSeconds |
| TranscriptionCompleted | Transcript ready | RecordingId, Language, S3Path |

---

## Redis Cache Schema

### Participant Presence (ephemeral)

**Key**: `room:{occurrenceId}:participants`
**Type**: Hash
**Field**: `{participantStateId}`
**Value**: JSON `{ "userId": "...", "displayName": "...", "role": "...", "joinedAt": "...", "audioState": "...", "videoState": "..." }`
**TTL**: Room duration + 1 hour

### Room Status (ephemeral)

**Key**: `room:{occurrenceId}:status`
**Type**: String
**Value**: Current `RoomOccurrenceStatus` value
**TTL**: Room duration + 1 hour
