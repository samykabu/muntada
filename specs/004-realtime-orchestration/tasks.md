# Epic 4: Realtime Orchestration Module - Task Breakdown

**Version:** 1.0
**Epic Owner:** Infrastructure & Realtime Systems
**Last Updated:** 2026-04-03

---

## Execution Overview

This epic manages ephemeral state and real-time coordination for active audio rooms. Depends on Identity (Epic 1), Tenancy (Epic 2), Rooms (Epic 3), and Shared Kernel (Epic 0). Tasks organize into 5 phases: infrastructure, token issuance, state caching, webhook handling, and broadcasting.

---

## Phase 1: Module Setup & LiveKit Integration

### T401: Realtime Module Structure [P]
**User Story:** US-4.1
**Priority:** P1
**Effort:** 5 pts
**Dependencies:** Epic 0, Epic 3

Create Realtime Orchestration module structure.

**File Locations:**
- `backend/src/Modules/Realtime/Realtime.csproj`
- LiveKit C# SDK integration
- SignalR configuration

**Acceptance:**
- Module compiles
- LiveKit SDK available
- SignalR configured

---

### T402: LiveKit API Integration [P]
**User Story:** US-4.1
**Priority:** P1
**Effort:** 5 pts
**Dependencies:** T401

Implement LiveKit API client wrapper.

**Deliverables:**
- `ILiveKitClient` interface
- `LiveKitClient` wrapper for API calls
- Room creation, participant queries
- Token validation

**File Locations:**
- `backend/src/Modules/Realtime/Infrastructure/LiveKitClient.cs`

---

## Phase 2: Token Issuance

### T403: LiveKit Token Generation [P]
**User Story:** US-4.1
**Priority:** P1
**Effort:** 8 pts
**Dependencies:** T402

Implement JWT token generation for LiveKit.

**Deliverables:**
- `GenerateLiveKitTokenCommand` & handler
- Token signing (with LiveKit API key)
- Claims: subscribe, publish, publish_data, admin_controls, recorder
- Token TTL: 6 hours (configurable)
- Metadata: tenantId, roomId, role

**File Locations:**
- `backend/src/Modules/Realtime/Application/Commands/GenerateLiveKitTokenCommand.cs`
- `backend/src/Modules/Realtime/Application/Services/TokenService.cs`

**Acceptance:**
- Token generated and signed correctly
- Claims included based on role
- Token valid in LiveKit

---

### T404: Join Endpoint with Permission Validation [P]
**User Story:** US-4.1
**Priority:** P1
**Effort:** 10 pts
**Dependencies:** T403

Create room join endpoint.

**Deliverables:**
- `POST /api/rooms/{roomId}/join` endpoint
- Invite token validation
- Tenant quota check
- Room lock check
- Moderator permission determination
- Token issuance and response
- Audit logging

**File Locations:**
- `backend/src/Modules/Realtime/Api/Controllers/RoomJoinController.cs`

**Acceptance:**
- Valid invites accepted
- Invalid/expired rejected
- Quota enforced
- Locked rooms rejected
- Response includes token and metadata
- Latency < 200ms p95

---

## Phase 3: Room State Caching

### T405: Room State Cache Implementation [P]
**User Story:** US-4.2
**Priority:** P1
**Effort:** 8 pts
**Dependencies:** T401

Implement Redis-backed room state cache.

**Deliverables:**
- `IRoomStateCache` interface
- `RedisRoomStateCache` implementation
- Room state structure: {roomId, tenantId, state, activeParticipants, participantCount, quality}
- TTL: room duration + 1 hour
- Atomic operations for counters

**File Locations:**
- `backend/src/Modules/Realtime/Infrastructure/Cache/RoomStateCache.cs`

**Acceptance:**
- Cache populated on first join
- Updates persisted
- TTL managed correctly

---

### T406: Participant State Tracking [P]
**User Story:** US-4.2
**Priority:** P1
**Effort:** 8 pts
**Dependencies:** T405

Implement per-participant state in Redis.

**Deliverables:**
- Participant cache structure: {participantId, identity, role, joinedAt, audioEnabled, speakingState, qualityScore, trackSid}
- Update on LiveKit events
- TTL: room duration + 1 hour
- Atomic updates

**File Locations:**
- `backend/src/Modules/Realtime/Infrastructure/Cache/ParticipantStateCache.cs`

**Acceptance:**
- Participant state created on join
- Updates on webhook events
- Speaking state tracked
- Quality metrics updated

---

### T407: Speaking State & Quality Metrics [P]
**User Story:** US-4.2
**Priority:** P1
**Effort:** 8 pts
**Dependencies:** T406

Implement speaking state detection and quality calculation.

**Deliverables:**
- Speaking state from LiveKit track activity
- Voice activity detection (VAD) or audioLevel threshold
- Debouncing (500ms)
- Quality score calculation: (bandwidth >= 2Mbps ? 1 : 0.5) * (latency < 100ms ? 1 : 0.5) * (loss < 5% ? 1 : 0.5)
- Quality alerts at threshold

**File Locations:**
- `backend/src/Modules/Realtime/Application/Services/SpeakingStateService.cs`
- `backend/src/Modules/Realtime/Application/Services/QualityMetricsService.cs`

**Acceptance:**
- Speaking state updates in real-time
- Quality calculated correctly
- Alerts triggered at threshold

---

## Phase 4: LiveKit Webhook Processing

### T408: Webhook Handler & Signature Validation [P]
**User Story:** US-4.3
**Priority:** P1
**Effort:** 8 pts
**Dependencies:** T405, T406

Implement LiveKit webhook handler.

**Deliverables:**
- `POST /webhooks/livekit` endpoint
- HMAC-SHA256 signature validation
- Supported events: participant_joined, participant_left, track_published, track_unpublished, room_started, room_finished
- Error handling and logging

**File Locations:**
- `backend/src/Modules/Realtime/Api/Controllers/WebhookController.cs`

**Acceptance:**
- Webhook endpoint accessible
- Signature validation correct
- Invalid signatures rejected
- Supported events handled

---

### T409: Webhook Event Processing [P]
**User Story:** US-4.3
**Priority:** P1
**Effort:** 10 pts
**Dependencies:** T408

Implement webhook event handlers.

**Deliverables:**
- Handler for participant_joined: create cache entry
- Handler for track_published: update audioEnabled, increment speakers
- Handler for track_unpublished: clear track, decrement speakers
- Handler for participant_left: remove from cache
- Idempotency via deduplication key (room + event type + participant + event_id)
- Redis dedup store with 24-hour TTL

**File Locations:**
- `backend/src/Modules/Realtime/Application/Services/WebhookProcessingService.cs`

**Acceptance:**
- All events processed correctly
- Duplicate events handled (idempotent)
- Cache updated immediately
- Latency < 500ms

---

### T410: Webhook Backpressure & Retry [P]
**User Story:** US-4.3
**Priority:** P2
**Effort:** 8 pts
**Dependencies:** T409

Implement webhook backpressure and retry logic.

**Deliverables:**
- Queue depth monitoring
- Return 503 if queue > 1000 events
- Failed webhook persistence to RabbitMQ
- Exponential backoff retry (1s, 2s, 4s, 8s)
- Dead letter queue for final failures
- Logging of all retries

**File Locations:**
- `backend/src/Modules/Realtime/Application/Services/WebhookRetryService.cs`

**Acceptance:**
- Backpressure applied at threshold
- Failed webhooks retried
- DLQ captures final failures
- Retries logged

---

### T411: Webhook Health Check & Reconciliation [P]
**User Story:** US-4.6
**Priority:** P2
**Effort:** 10 pts
**Dependencies:** T409

Implement health monitoring and state reconciliation.

**Deliverables:**
- Health check detects webhook delivery gaps (> 5 minutes)
- LiveKit API query: GET /rooms/{roomId}
- Compare LiveKit participants with cache
- Identify discrepancies and update cache
- Broadcast missed events
- Logging for debugging

**File Locations:**
- `backend/src/Modules/Realtime/Application/BackgroundJobs/WebhookHealthCheckJob.cs`

**Acceptance:**
- Health check runs periodically
- Reconciliation on detection
- Cache restored to consistency
- Missed events broadcast

---

## Phase 5: SignalR Broadcasting

### T412: SignalR Hub Setup [P]
**User Story:** US-4.4
**Priority:** P1
**Effort:** 8 pts
**Dependencies:** Epic 0

Configure SignalR with Redis backplane.

**Deliverables:**
- SignalR hub factory
- Redis backplane for scaling
- Authentication via JWT
- Hub groups: room-{roomId}, tenant-{tenantId}, admin-alerts
- Connection management and lifecycle

**File Locations:**
- `backend/src/Modules/Realtime/Api/Hubs/RoomHub.cs`
- `backend/src/SharedKernel/Infrastructure/SignalR/SignalRConfiguration.cs`

**Acceptance:**
- Hub connects and authenticates
- Groups managed correctly
- Broadcasting works across instances

---

### T413: Room Hub Broadcasting [P]
**User Story:** US-4.4
**Priority:** P1
**Effort:** 8 pts
**Dependencies:** T412

Implement room hub event broadcasting.

**Deliverables:**
- `roster.changed` event: participant join/leave/update
- `handraise.changed` event (from Moderation module)
- `announcement.changed` event: recording status
- `recording.counter.changed` event: active recorder count
- `file.ready` event: recording available
- Broadcast payload and client subscription

**File Locations:**
- `backend/src/Modules/Realtime/Api/Hubs/RoomHub.cs`

**Acceptance:**
- Events broadcast to room group
- Payload < 64 KB
- Latency < 100ms
- All participants receive events

---

### T414: Tenant & Admin Hubs [P]
**User Story:** US-4.4
**Priority:** P2
**Effort:** 8 pts
**Dependencies:** T412

Implement tenant and admin hubs.

**Deliverables:**
- Tenant hub: `report.snapshot` events (active rooms, participants, quality)
- Admin hub: `platform.alert` events (critical issues)
- Authorization checks (tenant users, platform admins)
- Group membership management

**File Locations:**
- `backend/src/Modules/Realtime/Api/Hubs/TenantHub.cs`
- `backend/src/Modules/Realtime/Api/Hubs/AdminHub.cs`

**Acceptance:**
- Tenant hub shows live metrics
- Admin alerts received only by admins
- Authorization enforced

---

## Phase 6: Token Refresh & Advanced Features

### T415: Speaker Grant Token Refresh [P]
**User Story:** US-4.5
**Priority:** P1
**Effort:** 10 pts
**Dependencies:** T403

Implement token refresh on permission changes.

**Deliverables:**
- When SpeakerGrant created: generate new token with publish claim
- When SpeakerGrant revoked: generate token without publish claim
- Send via SignalR: {event: "token.refreshed", token, reason}
- Proactive refresh at expiry - 30s
- No immediate revocation (graceful transition)

**File Locations:**
- `backend/src/Modules/Realtime/Application/Services/TokenRefreshService.cs`

**Acceptance:**
- Token refresh triggered by permission change
- New token delivered within 1 second
- Client can update without re-join

---

### T416: Active Recorder Counter [P]
**User Story:** US-4.5
**Priority:** P1
**Effort:** 5 pts
**Dependencies:** T405

Implement recorder counter for announcements.

**Deliverables:**
- Redis atomic counter: `room:{roomId}:active_recorders`
- Increment on local recording start or server recording start
- Decrement on stop
- Counter transitions trigger announcements: 0→1 (started), 1→0 (stopped)
- Never negative (defensive check)

**File Locations:**
- `backend/src/Modules/Realtime/Application/Services/RecorderCounterService.cs`

**Acceptance:**
- Counter increments/decrements correctly
- Transitions trigger announcements
- Never goes negative

---

## Phase 7: Integration Tests

### T417: Realtime Integration Tests [P]
**User Story:** All
**Priority:** P1
**Effort:** 21 pts
**Dependencies:** All tasks

Write comprehensive tests.

**Deliverables:**
- Tests for token issuance (valid, invalid invites)
- Tests for quota enforcement
- Tests for webhook processing (all event types)
- Tests for idempotency
- Tests for room state cache
- Tests for speaking state tracking
- Tests for quality metrics
- Tests for SignalR broadcasting
- Tests for graceful degradation

**File Locations:**
- `backend/src/Modules/Realtime.Tests/Integration/`

**Acceptance:**
- All scenarios tested
- Coverage > 80%
- Performance tests included

---

## Success Metrics

- Token issuance latency < 200ms p95
- Webhook processing < 500ms p95
- Broadcasting latency < 100ms p95
- Cache hit rate > 99%
- Webhook idempotency: 0% duplicate state updates
- Permission change reflected within 1 second
- Graceful degradation: no user-visible errors during 5-minute webhook outage
- Scalability: 1000+ participants per room, 100+ concurrent rooms
- Availability: 99.95% uptime
