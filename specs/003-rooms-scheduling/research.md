# Research: Rooms & Scheduling

**Branch**: `003-rooms-scheduling` | **Date**: 2026-04-04

## R1: State Machine Library

**Decision**: Use [Stateless](https://github.com/dotnet-state-machine/stateless) for room lifecycle state machine.

**Rationale**: Stateless is the most widely adopted .NET state machine library (9k+ GitHub stars), supports parameterized triggers, guard clauses, async transitions, and DOT graph export for documentation. It integrates cleanly with DDD aggregate roots — the state machine configuration lives inside the `RoomOccurrence` aggregate, enforcing valid transitions at the domain level.

**Alternatives considered**:
- **Manual switch/case**: Simpler but error-prone, no graph export, no guard clause support. Rejected due to 6 states and 10+ transitions.
- **NStateMachine**: Less active maintenance, smaller community. Rejected.
- **MassTransit Saga State Machine**: Too heavy for in-process state management; better suited for distributed sagas. Rejected.

---

## R2: iCal Recurrence Rule Library

**Decision**: Use [Ical.Net](https://github.com/rianjs/ical.net) (NuGet: `Ical.Net`) for RRULE parsing and occurrence generation.

**Rationale**: Ical.Net is the most mature .NET iCal library, supporting RFC 5545 RRULE parsing, timezone-aware occurrence generation (via NodaTime internally), and DST-correct recurrence expansion. It handles all required patterns: FREQ=DAILY, FREQ=WEEKLY with BYDAY, FREQ=MONTHLY with BYMONTHDAY/BYDAY, and custom rules.

**Alternatives considered**:
- **Custom RRULE parser**: Too complex and error-prone for RFC 5545 compliance. Rejected.
- **NodaTime only**: NodaTime handles timezones well but doesn't parse iCal RRULE syntax. Would need custom parser on top. Rejected.

---

## R3: Timezone Handling Strategy

**Decision**: Store organizer's IANA timezone identifier (e.g., `Asia/Riyadh`) with each `RoomSeries` and standalone `RoomOccurrence`. Use NodaTime (via Ical.Net dependency) for all timezone conversions. Store `ScheduledAt` as UTC in the database; store `OrganizerTimeZoneId` alongside. Display conversion happens at the API/frontend layer using the viewer's browser timezone.

**Rationale**: IANA timezone database is the industry standard. Storing UTC + timezone ID allows correct DST-aware recurrence expansion (a 9 AM weekly meeting in `America/New_York` stays at 9 AM local even across DST changes). The frontend uses `Intl.DateTimeFormat` for display conversion.

**Alternatives considered**:
- **Store local time only**: Ambiguous across DST transitions. Rejected.
- **Store DateTimeOffset**: Loses the timezone identity (offset changes with DST). Rejected.

---

## R4: LiveKit Integration Pattern

**Decision**: Use LiveKit Server SDK for .NET (`Livekit.Server.Sdk.Dotnet`) for server-side API calls (create room, generate join token). Use LiveKit webhooks (HTTP POST with HMAC-SHA256 verification) for participant join/leave/media events.

**Rationale**: LiveKit's .NET SDK provides typed access to the room API and token generation. Webhooks are the recommended pattern for server-side event handling — they are retry-safe and can be made idempotent using event IDs. The webhook handler updates `RoomParticipantState` in the database and pushes real-time updates to Redis + SignalR.

**Alternatives considered**:
- **LiveKit gRPC streaming**: More complex, requires persistent connection. Webhooks are simpler for our event-driven architecture. Rejected.
- **Polling LiveKit API**: Introduces latency and unnecessary load. Rejected.

---

## R5: Real-Time Participant Tracking

**Decision**: Dual-write pattern — LiveKit webhook handler writes participant state to both Redis (ephemeral, for real-time reads) and SQL Server (persistent, for analytics). SignalR hub broadcasts changes to room groups.

**Rationale**: Redis provides sub-millisecond reads for the participant list API during live rooms. SQL Server provides durable storage for post-room analytics. SignalR groups (keyed by `room-{occurrenceId}`) ensure only room participants receive broadcasts. This matches the existing SignalR + Redis backplane pattern in the Aspire AppHost.

**Key design**:
- Redis key: `room:{occurrenceId}:participants` (Hash, field = participantId, value = JSON state)
- Redis TTL: room duration + 1 hour buffer
- SignalR group: `room-{occurrenceId}`
- Broadcast events: `ParticipantJoined`, `ParticipantLeft`, `ParticipantMediaChanged`, `RoomStatusChanged`

**Alternatives considered**:
- **Redis only (no SQL)**: Loses analytics data after TTL. Rejected.
- **SQL only (no Redis)**: Too slow for real-time participant list reads under load. Rejected.

---

## R6: Recording Storage Strategy

**Decision**: Use LiveKit's Egress API for recording (composite recording of all tracks). Store recording files in MinIO via S3-compatible SDK. Store metadata (path, size, duration, status, visibility) in SQL Server `[rooms]` schema.

**Rationale**: LiveKit Egress handles the media capture natively — no need for a separate ffmpeg sidecar. MinIO is already provisioned in Aspire AppHost. Pre-signed S3 URLs handle secure download/streaming without proxying through the API server.

**Alternatives considered**:
- **External ffmpeg recorder**: More infrastructure to manage. Rejected since LiveKit Egress handles this natively.
- **Direct file system storage**: Not scalable, not S3-compatible. Rejected.

---

## R7: Transcription Service

**Decision**: Use AssemblyAI as the transcription provider, with async polling via a MassTransit consumer (RabbitMQ). Fallback: abstract behind `ITranscriptionService` interface for easy provider swapping.

**Rationale**: AssemblyAI offers competitive accuracy, supports Arabic and English (key for GCC market), and provides a simple REST API with async webhook callbacks. The interface abstraction allows swapping to Google Cloud Speech-to-Text or Whisper API without code changes.

**Alternatives considered**:
- **Google Cloud Speech-to-Text**: Good but more complex setup with GCP credentials. Viable alternative.
- **OpenAI Whisper**: Self-hosted option, but adds infrastructure burden. Rejected for v1.
- **In-process Whisper.net**: CPU-intensive, blocks worker threads. Rejected.

---

## R8: Grace Period Timer Implementation

**Decision**: Use a delayed MassTransit message (`ScheduleSend`) to trigger grace period expiry. When grace starts, schedule a message for `GracePeriodSeconds` in the future. If moderator reconnects, cancel the scheduled message.

**Rationale**: MassTransit's `ScheduleSend` with RabbitMQ delayed message plugin is already available in the infrastructure. It's more reliable than in-memory timers (survives server restarts) and simpler than a separate job scheduler for per-room timers.

**Alternatives considered**:
- **In-memory CancellationTokenSource**: Lost on restart. Rejected.
- **Hangfire delayed job**: Works but adds Hangfire dependency. MassTransit already available. Rejected.
- **Redis key expiry notification**: Unreliable (delivery not guaranteed). Rejected.

---

## R9: Authorization Model for Room Management

**Decision**: Enforce at the Application layer via MediatR pipeline behavior. Check `TenantRole` (from `TenantMembership` in Tenancy module) is `Admin` or `Owner` for all room creation/management commands. Regular `Member` role can only execute `JoinRoomCommand` and read queries.

**Rationale**: Consistent with the existing `TenantContextMiddleware` pattern that resolves tenant and membership for each request. Authorization checks in the command handler pipeline keep business rules in the Application layer, not scattered across controllers.

**Alternatives considered**:
- **ASP.NET Core policy-based auth**: Works for controller-level, but doesn't cover command-level granularity. Used in combination, not instead.
- **Domain-level authorization**: Too deep — authorization is an application concern. Rejected.
