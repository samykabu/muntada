# Epic 4: Realtime Orchestration Module

**Version:** 1.0
**Last Updated:** 2026-04-03
**Status:** Specification
**Module Owner:** Infrastructure & Realtime Systems
**Dependencies:** Epic 0 (Foundation), Epic 1 (Identity), Epic 2 (Tenancy), Epic 3 (Rooms)

---

## Overview

The Realtime Orchestration Module manages the ephemeral state and real-time coordination of active audio rooms. It orchestrates LiveKit media transport with server-side room state caching, handles token issuance with fine-grained permission claims, processes LiveKit webhooks, maintains roster state, and broadcasts room events to all participants via SignalR hubs. This module bridges stateless HTTP request-response patterns with stateful, low-latency real-time updates.

---

## User Stories

### US-4.1: Participant Joins Room with LiveKit Integration
**Priority:** P0 (Critical)
**Capability:** Server validates room invite, tenant policy, and room readiness, then issues a signed LiveKit join token with permission claims.

**As a** participant
**I want to** join an active audio room after accepting the invite
**So that** my audio stream is authenticated and authorized in the LiveKit media engine

**Acceptance Scenarios:**

**Scenario 4.1.1: Valid Join with Publisher Permissions**
```gherkin
Given a tenant "TechConf2026" with LiveKit server configured
And a room "Panel-Q&A" exists in the tenant with state = Active
And a participant "Alice" has a valid invite token to the room
And Alice's role is "Panelist" (has publish permission by policy)
When Alice calls POST /rooms/{roomId}/join with invite token
Then the server validates the invite signature and expiry
And the server validates the room is in state Active and not locked
And the server validates the tenant's participant count quota is not exceeded
And the server generates a LiveKit join token with claims: [subscribe, publish, publish_data, admin_controls]
And the server stores room state in Redis: {participantId, role, joinedAt, audio_on, speaking_state, quality}
And the server returns {token, livekitUrl, roomId, participantId} with 200 OK
And Alice connects to LiveKit using the token
And within 100ms, Alice's audio stream is available to other participants
```

**Scenario 4.1.2: Listener Join with Subscriber-Only Permissions**
```gherkin
Given a room "Panel-Q&A" exists with 2 active panelists
And a participant "Bob" has valid invite token with role = "Listener"
When Bob calls POST /rooms/{roomId}/join
Then the server generates a LiveKit token with claims: [subscribe, publish_data] only
And the server does NOT include publish or admin_controls claims
And the server stores Bob's state in Redis with role = Listener
And when a panelist speaks, Bob's client receives audio through LiveKit
And when Bob attempts to publish audio, LiveKit rejects it with PermissionDenied
And the server logs the rejection attempt
```

**Scenario 4.1.3: Join Fails - Room Locked by Moderator**
```gherkin
Given a room is locked by moderator
And participant "Carol" has valid invite and available quota
When Carol calls POST /rooms/{roomId}/join
Then the server returns 403 Forbidden with error "room_locked"
And Carol's connection is not created
And server logs: audit entry for blocked join attempt
```

**Scenario 4.1.4: Join Fails - Quota Exceeded**
```gherkin
Given a tenant allows max 100 participants per room
And a room currently has 100 participants
And participant "Dave" has valid invite
When Dave calls POST /rooms/{roomId}/join
Then the server returns 429 Too Many Participants with error "participant_quota_exceeded"
And Dave is not added to room state
And server logs quota violation for monitoring
```

---

### US-4.2: Room State Cache Maintains Roster and Quality Indicators
**Priority:** P0 (Critical)
**Capability:** Redis cache maintains authoritative room state including participant roster, speaking state, and connection quality metrics.

**As a** room participant
**I want to** see current roster with live speaking state and quality indicators
**So that** I know who is speaking and understand connection stability

**Acceptance Scenarios:**

**Scenario 4.2.1: Roster Updated on Participant Join/Leave**
```gherkin
Given a room in Redis cache with current roster: [Alice-Panelist, Bob-Listener]
When participant "Carol-Panelist" joins the room
And LiveKit broadcasts participant.joined webhook
Then the orchestration module updates Redis cache: {roomId: {participants: [..., Carol]}}
And the cache entry includes: {participantId, identity, role, joinedAt, audioEnabled, speakingState: false, qualityScore}
And within 50ms, SignalR broadcasts roster.changed event to all connected clients
And each client receives: {action: "joined", participantId: "Carol", role: "Panelist"}
And the roster includes: [Alice-Panelist, Bob-Listener, Carol-Panelist]
```

**Scenario 4.2.2: Speaking State Tracked from LiveKit Activity**
```gherkin
Given Alice is connected with audio enabled
When LiveKit detects Alice's voice activity (VAD triggers)
And LiveKit webhook broadcasts: track.subscribed with audioLevel >= threshold
Then the orchestration module updates Redis: participants.Alice.speakingState = true
And SignalR broadcasts: {event: "speaking_started", participantId: "Alice", timestamp}
And all other participants' clients update UI to show Alice is speaking
When voice activity ends and audioLevel < threshold
Then speaking state is updated to false after 500ms debounce
And SignalR broadcasts: {event: "speaking_ended", participantId: "Alice"}
```

**Scenario 4.2.3: Quality Indicators Updated from LiveKit Stats**
```gherkin
Given a participant is connected and streaming
When LiveKit collects connection stats: {bandwidth: 2.5Mbps, latency: 45ms, packetLoss: 0.1%}
And the orchestration module receives these via webhook or polling
Then Redis cache is updated: participants.{id}.quality = {bandwidth, latency, packetLoss, score}
And quality score is calculated: score = (bandwidth > 2Mbps ? 1 : 0.5) * (latency < 100ms ? 1 : 0.5) * (packetLoss < 5% ? 1 : 0.5)
And if score < 0.5 (poor quality), SignalR broadcasts: {event: "quality_alert", participantId, quality_score}
And participants can see quality indicators on roster UI
```

**Scenario 4.2.4: Cache Consistency on Moderator Removes Participant**
```gherkin
Given Alice, Bob, Carol in active room cached in Redis
When moderator manually removes Bob from room
Then the orchestration module updates Redis immediately: remove Bob from participants
And SignalR broadcasts: {event: "participant_removed", participantId: "Bob", reason: "moderator_action"}
And LiveKit connection for Bob is not directly terminated by room module (LiveKit may continue until client disconnect)
And cache remains consistent: [Alice, Carol]
```

---

### US-4.3: LiveKit Webhook Processing Maintains State Consistency
**Priority:** P0 (Critical)
**Capability:** Server processes LiveKit webhooks for room and track state changes, applying backpressure and maintaining idempotency.

**As a** platform operator
**I want to** receive and process LiveKit events reliably without duplicates
**So that** room state remains consistent across LiveKit and our cache

**Acceptance Scenarios:**

**Scenario 4.3.1: Participant Joined Webhook - Synchronize State**
```gherkin
Given a participant successfully connects to LiveKit
When LiveKit sends webhook: POST /webhooks/livekit with event type = "participant_joined"
With payload: {event: {type: "participant_joined", room: {name: "room123"}, participant: {identity, sid, state: "ACTIVE", metadata}}}
Then the orchestration webhook handler receives and validates webhook signature (HMAC-SHA256)
And extracts: participantId from metadata.participantId (our internal ID)
And updates Redis cache: rooms.room123.participants.{participantId} = {sid: "from LiveKit", state: "active"}
And if cache doesn't have this participant record yet, creates new entry with defaults
And within 100ms broadcasts SignalR event: {event: "participant_joined", participantId, timestamp}
And no duplicate processing occurs even if webhook is delivered twice (idempotency via participantId + event_id deduplication)
```

**Scenario 4.3.2: Track Published Webhook - Update Audio Stream State**
```gherkin
Given participant Alice has joined room
When LiveKit sends webhook: {event: {type: "track_published", room, participant, track: {type: "AUDIO", sid, source: "MICROPHONE"}}}
Then orchestration module updates Redis: participants.Alice.audioTrackSid = track.sid
And updates participants.Alice.audioEnabled = true
And if this is first audio track in room (counter = 0), increments active_speakers counter
And broadcasts SignalR: {event: "audio_published", participantId: "Alice", trackSid}
And room participants' clients can now receive Alice's audio from LiveKit
```

**Scenario 4.3.3: Track Unpublished Webhook - Clean Up State**
```gherkin
Given Alice has active audio track in Redis cache
When LiveKit sends webhook: {event: {type: "track_unpublished", participant: Alice, track: {type: "AUDIO"}}}
Then orchestration module updates Redis: participants.Alice.audioTrackSid = null
And sets participants.Alice.audioEnabled = false
And decrements active_speakers counter
And broadcasts SignalR: {event: "audio_unpublished", participantId: "Alice"}
And if active_speakers counter becomes 0, may trigger announcement: "Recording paused - no speakers"
```

**Scenario 4.3.4: Participant Left Webhook - Clean Up Session**
```gherkin
Given Alice is in active room
When LiveKit sends webhook: {event: {type: "participant_left", room, participant: Alice}}
Then orchestration module removes from Redis: rooms.room123.participants.Alice
And broadcasts SignalR: {event: "participant_left", participantId: "Alice"}
And if Alice was a speaker with active grant, may trigger speaker grant cleanup
And if room.participants.length == 0, marks room.state = "empty" in Redis
And if room.retention_auto_end == true and state == "empty", schedules room end
```

**Scenario 4.3.5: Webhook Retry and Backpressure Handling**
```gherkin
Given orchestration service is temporarily under high load
When LiveKit sends webhook for participant_joined event
And the service takes 2 seconds to process
Then webhook handler accepts webhook within 5 seconds (per LiveKit SLA)
And applies exponential backoff: 1s, 2s, 4s, 8s maximum
And deduplicates by (room.name + event.type + participant.sid) to prevent double processing
And persists unprocessed webhook to message queue (RabbitMQ) for async retry
And logs: {timestamp, webhook_id, participant_id, retry_count, status}
And once room state is updated, idempotent reprocessing of same webhook has no side effects
```

---

### US-4.4: SignalR Hubs Broadcast Room, Tenant, and Admin Events
**Priority:** P0 (Critical)
**Capability:** SignalR hubs deliver real-time events to participants, tenants, and admins with connection resilience and group management.

**As a** participant
**I want to** receive instant notifications of roster changes, hand raises, announcements, and recording status
**So that** my UI stays synchronized with room state without polling

**Acceptance Scenarios:**

**Scenario 4.4.1: Room Hub - roster.changed Event**
```gherkin
Given Alice, Bob, Carol connected to room "Panel-Q&A"
And all three are subscribed to SignalR hub: "room-{roomId}-hub"
When a new participant "Dave" joins
And orchestration broadcasts roster.changed via SignalR
Then the hub sends event to all group members:
  {event: "roster.changed", action: "joined", participant: {id: "Dave", role: "Panelist", status: "active"}}
And within 100ms, Alice, Bob, Carol all receive the event
And participants' clients can update the roster UI in real-time
And the event includes timestamp so clients can order updates causally
```

**Scenario 4.4.2: Room Hub - handraise.changed Event**
```gherkin
Given a hand raise is queued or approved
When orchestration broadcasts handraise.changed
Then SignalR sends event to room group: {event: "handraise.changed", action: "queued" | "approved" | "rejected", handRaiseId, participantId, position_in_queue}
And moderators receive queue order: [HandRaise1{pos:1}, HandRaise2{pos:2}, ...]
And the requester receives confirmation: {status: "queued", position: 1, estimatedWaitTime: "2m"}
```

**Scenario 4.4.3: Room Hub - announcement.changed Event**
```gherkin
Given active recorder counter transitions from 0 to 1 (recording starts)
When orchestration triggers announcement state change
Then SignalR broadcasts: {event: "announcement.changed", announcement: "recording_started", timestamp, recordingType: "server"}
And all participants receive announcement
And clients can play audio or display UI notification: "Recording in progress"
When counter transitions from 1 to 0 (last recording stopped)
Then broadcasts: {event: "announcement.changed", announcement: "recording_stopped", timestamp}
And all participants are notified recording has ended
```

**Scenario 4.4.4: Room Hub - recording.counter.changed Event**
```gherkin
Given a participant enables local recording
When orchestration increments active_recorder_counter
Then broadcasts: {event: "recording.counter.changed", count: 2, recordingTypes: ["local", "server"]}
And participants can see UI indicator showing "2 recordings active"
When a local recorder stops
Then broadcasts: {event: "recording.counter.changed", count: 1, recordingTypes: ["server"]}
```

**Scenario 4.4.5: Room Hub - file.ready Event**
```gherkin
Given server recording completes and artifact is ready in MinIO
When orchestration publishes file.ready event
Then SignalR broadcasts: {event: "file.ready", recordingId, fileName, downloadUrl, duration, participants_recorded: ["Alice", "Bob"]}
And participants/moderators can see file is ready for download
And the URL is a pre-signed MinIO URL with expiry (24 hours)
```

**Scenario 4.4.6: Tenant Hub - report.snapshot Event**
```gherkin
Given a tenant dashboard user is connected to tenant hub: "tenant-{tenantId}-hub"
When room metrics are aggregated (e.g., every 30s)
And orchestration publishes aggregated snapshot
Then SignalR sends: {event: "report.snapshot", rooms_active: 3, participants_total: 47, avg_quality_score: 0.92, recordings_active: 2}
And dashboard updates in real-time with live metrics
```

**Scenario 4.4.7: Admin Hub - platform.alert Event**
```gherkin
Given a platform admin is connected to admin hub: "admin-alerts"
When a critical event occurs (e.g., LiveKit webhook delivery failed for 5 minutes)
Then orchestration publishes: {event: "platform.alert", severity: "warning", message: "LiveKit webhook delivery delayed", room_affected: ["room123"], timestamp}
And admin receives alert in real-time dashboard
And can take corrective action (e.g., manual state sync)
```

---

### US-4.5: Token Refresh After Moderation Changes
**Priority:** P1 (High)
**Capability:** When participant permissions change (e.g., speaker grant approved), server issues new token with updated claims without requiring re-join.

**As a** participant
**I want to** have my permissions updated immediately when moderator approves me to speak
**So that** I can publish audio without dropping from the room and re-joining

**Acceptance Scenarios:**

**Scenario 4.5.1: Speaker Grant Approval Issues New Token**
```gherkin
Given Alice has hand raised and is in queue
And Alice's current LiveKit token has claims: [subscribe, publish_data]
When moderator approves Alice's hand raise
And orchestration creates SpeakerGrant with status = Active
And broadcasts speaker.approved via SignalR
Then orchestration generates new LiveKit token with claims: [subscribe, publish, publish_data, admin_controls]
And sends new token to Alice via SignalR: {event: "token.refreshed", token: "eyJhbGc...", reason: "speaker_grant_approved"}
And Alice's client discards old token and uses new token
And Alice can now publish audio immediately
And no network disconnection occurs
```

**Scenario 4.5.2: Speaker Revocation Updates Token**
```gherkin
Given Alice is an approved speaker with SpeakerGrant.status = Active
And Alice's token has publish permission
When moderator revokes Alice's speaker grant
And orchestration updates SpeakerGrant.status = Revoked
And broadcasts speaker.revoked via SignalR
Then orchestration generates new token with claims: [subscribe, publish_data] (removing publish)
And sends updated token via SignalR: {event: "token.refreshed", token, reason: "speaker_grant_revoked"}
And Alice's client uses new token
And when Alice tries to publish with old token, LiveKit rejects with PermissionDenied
And when Alice uses new token, publish is blocked
And server logs this revocation for audit
```

**Scenario 4.5.3: Token Expiry Handling**
```gherkin
Given Alice's token will expire in 30 seconds
When orchestration checks token expiry (periodic task every 10s)
Then for tokens expiring in < 60s, generates new token proactively
And sends via SignalR: {event: "token.refreshed", token, reason: "token_expiry_refresh"}
And Alice's client uses new token before old expires
And no disruption occurs
```

---

### US-4.6: Graceful Degradation When LiveKit Webhook Delivery is Delayed
**Priority:** P2 (Medium)
**Capability:** If LiveKit webhooks are delayed, room state eventually reconciles without blocking user experience.

**As a** platform operator
**I want to** handle LiveKit webhook delivery delays gracefully
**So that** transient network issues don't break room state consistency

**Acceptance Scenarios:**

**Scenario 4.6.1: Webhook Delivery Delayed - Eventual Consistency**
```gherkin
Given a participant joins LiveKit successfully
But orchestration's webhook endpoint is temporarily unreachable (e.g., network flap)
When participant can connect and stream audio via LiveKit
And webhook delivery is retried by LiveKit (standard exponential backoff)
Then room state cache may be out of sync for a window (e.g., 30 seconds)
And meanwhile, participants can still join/leave based on their LiveKit connection status
And once webhook is eventually delivered (within 5 minutes per LiveKit SLA)
And orchestration updates cache and broadcasts roster changes
And all state is reconciled
And no participant data is lost
```

**Scenario 4.6.2: Webhook Timeout - Health Check Fallback**
```gherkin
Given webhooks are not received for > 5 minutes
When orchestration's health monitor detects this gap
Then orchestration initiates full state reconciliation via LiveKit API: GET /rooms/{name}
And fetches current participant list from LiveKit API
And compares with Redis cache
And detects discrepancies (e.g., participant in LiveKit but not in cache)
And updates cache to match LiveKit source of truth
And broadcasts missed events retroactively (participant_joined for those in LiveKit but not cached)
And logs reconciliation event for debugging
```

**Scenario 4.6.3: Graceful UI Experience During Webhook Delays**
```gherkin
Given Alice is viewing room roster
When orchestration detects webhook delay
And roster is stale (e.g., Carol joined but webhook not yet processed)
Then orchestration does NOT hide stale information from UI
And instead marks roster entry with "pending sync" indicator
And once webhook arrives and state is confirmed
And updates UI to confirmed state
And this avoids flickering and jarring user experience
```

---

## Functional Requirements

### F4.1: LiveKit Token Issuance
- **F4.1.1:** Server must validate invite token signature (HMAC-SHA256, key from config) before issuing LiveKit token
- **F4.1.2:** Server must verify invite expiry and room state before token issuance
- **F4.1.3:** Join token claims must be determined by participant role via policy engine (E.g., Panelist → [subscribe, publish, publish_data, admin_controls], Listener → [subscribe, publish_data])
- **F4.1.4:** LiveKit token must be signed with LiveKit API key and include: room name, identity (participantId), metadata (role, tenantId), ttl (default 6 hours), can_subscribe, can_publish, can_publish_data, room_admin
- **F4.1.5:** Token must include recorder claim if participant has recorder role
- **F4.1.6:** POST /rooms/{roomId}/join must accept invite_token and optional metadata in request body
- **F4.1.7:** Response must include: token (LiveKit JWT), roomId, participantId, livekitUrl, expiresAt
- **F4.1.8:** Tenant quota validation must reject join if participant_count >= tenant.max_participants_per_room
- **F4.1.9:** Room lock state must be checked; locked rooms reject join requests with 403 error
- **F4.1.10:** Server must log all join attempts (success/failure) for audit trail

### F4.2: Room State Caching (Redis)
- **F4.2.1:** Each active room must have Redis key: `room:{roomId}` with TTL = room duration + 1 hour buffer
- **F4.2.2:** Room state structure: `{roomId, tenantId, state, activeParticipants: [], participantCount, createdAt, updatedAt, quality: {}}`
- **F4.2.3:** Each participant must have Redis entry: `room:{roomId}:participant:{participantId}` with fields: `{id, identity, role, joinedAt, audioEnabled, speakingState, qualityScore, audioTrackSid, videoTrackSid, lastActivityAt}`
- **F4.2.4:** Speaking state must be updated based on LiveKit track activity (VAD or audioLevel threshold)
- **F4.2.5:** Speaking state changes must be debounced (500ms) to avoid jitter
- **F4.2.6:** Quality metrics must be cached: `{bandwidth, latency_ms, packet_loss_pct, jitter_ms, score: 0-1}`
- **F4.2.7:** Quality score calculation: `(bandwidth >= 2Mbps ? 1 : 0.5) * (latency < 100ms ? 1 : 0.5) * (packet_loss < 5% ? 1 : 0.5)`
- **F4.2.8:** Room state cache must be populated on first participant join and maintained until room ends
- **F4.2.9:** Cache must support atomic increments for active recorder counter and speaker counter
- **F4.2.10:** Cache consistency must be validated on each webhook (e.g., participant.sid must match cached value)
- **F4.2.11:** Participant removal from cache must be atomic and broadcast immediately via SignalR

### F4.3: LiveKit Webhook Processing
- **F4.3.1:** Webhook handler must validate request signature (HMAC-SHA256 using LiveKit API key)
- **F4.3.2:** Webhook endpoint: POST /webhooks/livekit (Rooms module)
- **F4.3.3:** Supported webhook event types: participant_joined, participant_left, track_published, track_unpublished, track_subscribed, track_unsubscribed, room_started, room_finished
- **F4.3.4:** Handler must process events idempotently using (room.name + event.type + participant.identity + event_id) as deduplication key
- **F4.3.5:** Deduplication state must be stored in Redis with TTL 24 hours (to handle webhook retries)
- **F4.3.6:** Processing latency target: < 500ms from webhook receipt to cache update and SignalR broadcast
- **F4.3.7:** Handler must parse event payload: `{room: {name, emid}, participant: {identity, state, metadata}, track: {sid, type, source}}`
- **F4.3.8:** Upon participant_joined: create/update cache entry for participant with default state (audioEnabled: false, speaking_state: false)
- **F4.3.9:** Upon track_published with type=AUDIO: set participant.audioEnabled = true, update audioTrackSid
- **F4.3.10:** Upon track_unpublished: set participant.audioEnabled = false, clear trackSid, decrement speaker counter
- **F4.3.11:** Upon participant_left: remove participant from cache, decrement participant counter, check if room is now empty
- **F4.3.12:** Handler must apply backpressure: if queue > 1000 events, reject new webhooks temporarily (return 503)
- **F4.3.13:** Handler must persist failed webhooks to RabbitMQ for async retry with exponential backoff

### F4.4: SignalR Hubs and Groups
- **F4.4.1:** Room Hub (per-room group): group name = `room-{roomId}`, created on first participant join
- **F4.4.2:** Tenant Hub (per-tenant group): group name = `tenant-{tenantId}`, for dashboard/monitoring users
- **F4.4.3:** Admin Hub (global): group name = `admin-alerts`, for platform admins
- **F4.4.4:** Hub connections must be authenticated via SignalR access token (same identity as HTTP bearer token)
- **F4.4.5:** Hub must publish events: `roster.changed, handraise.changed, announcement.changed, recording.counter.changed, file.ready, report.snapshot, platform.alert`
- **F4.4.6:** Room hub must broadcast events to all participants in room group
- **F4.4.7:** Tenant hub must broadcast events only to users subscribed to tenant (via group membership)
- **F4.4.8:** Admin hub must broadcast events only to platform admins (authorization check)
- **F4.4.9:** Hub must maintain group membership: when participant joins room, add to room group; on leave, remove from group
- **F4.4.10:** Hub connection must be resilient to network interruption (reconnect with exponential backoff)
- **F4.4.11:** Hub must support at least 100 concurrent connections per room (scalable via Redis backplane)
- **F4.4.12:** Event payload max size: 64 KB (compress if exceeds)
- **F4.4.13:** Broadcast latency target: < 100ms from event trigger to client receipt

### F4.5: Active Recorder Counter
- **F4.5.1:** Counter must track: `room:{roomId}:active_recorders` (atomic integer in Redis)
- **F4.5.2:** Local recording registration increments counter; deregistration decrements
- **F4.5.3:** Server recording start increments counter; stop decrements
- **F4.5.4:** Counter must never go negative (validation at increment/decrement)
- **F4.5.5:** Counter state transitions trigger announcements:
  - 0 → 1: broadcast announcement.changed event with "recording_started"
  - 1 → 0: broadcast announcement.changed event with "recording_stopped"
  - Any other transition: broadcast recording.counter.changed with current count
- **F4.5.6:** Announcement logic must account for both local and server recordings
- **F4.5.7:** Counter value must be persisted and recoverable if process restarts (Redis persistence)

### F4.6: Token Refresh on Permission Change
- **F4.6.1:** When SpeakerGrant is created/approved, generate new LiveKit token with publish claim
- **F4.6.2:** When SpeakerGrant is revoked/expired, generate new token without publish claim
- **F4.6.3:** New token must be sent to affected participant via SignalR: `{event: "token.refreshed", token, reason: "speaker_grant_approved|revoked|expired"}`
- **F4.6.4:** New token must be generated before old token expires (proactive refresh at expiry - 30s)
- **F4.6.5:** Old token must remain valid during transition period (no immediate revocation)
- **F4.6.6:** Client must be able to update token without re-joining room (LiveKit token update API)

### F4.7: Graceful Degradation
- **F4.7.1:** If webhooks not received for > 5 minutes, trigger health check: GET /rooms/{name} from LiveKit API
- **F4.7.2:** Compare LiveKit participant list with Redis cache
- **F4.7.3:** For discrepancies (participant in LiveKit but not cached), fetch their state from LiveKit and update cache
- **F4.7.4:** Broadcast missed participant_joined events for newly discovered participants
- **F4.7.5:** Mark cache entries with reconciliation timestamp to prevent re-broadcasting same event
- **F4.7.6:** Log reconciliation actions for monitoring and debugging

---

## Key Entities

### JoinToken
```typescript
{
  token: string;           // LiveKit JWT signed with LiveKit API key
  roomId: string;          // Our internal room ID
  participantId: string;   // Our internal participant ID
  identity: string;        // LiveKit identity (matches participantId)
  role: "Panelist" | "Listener" | "Moderator" | "Recorder";
  claims: string[];        // ["subscribe", "publish", "publish_data", "admin_controls", "recorder"]
  issuedAt: DateTime;      // ISO 8601
  expiresAt: DateTime;     // ISO 8601 (default 6 hours from issue)
  metadata: {
    tenantId: string;
    roomId: string;
    role: string;
  };
}
```

### RoomStateCache (Redis)
```typescript
// Key: room:{roomId}
{
  roomId: string;
  tenantId: string;
  state: "active" | "empty" | "ending" | "ended";
  activeParticipants: ParticipantState[];
  participantCount: number;
  activeSpeakers: number;
  activeRecorders: number;
  createdAt: DateTime;
  updatedAt: DateTime;
  quality: {
    avgBandwidth: number;      // Mbps
    avgLatency: number;        // ms
    avgPacketLoss: number;     // %
    overallScore: number;      // 0-1
  };
  metadata: {
    recordingEnabled: boolean;
    recordingType: "local" | "server" | "both";
  };
}

// Key: room:{roomId}:participant:{participantId}
interface ParticipantState {
  participantId: string;
  identity: string;
  livekitSid: string;          // LiveKit participant session ID
  role: string;
  joinedAt: DateTime;
  audioEnabled: boolean;
  videoEnabled: boolean;
  speakingState: boolean;
  speakingStartedAt?: DateTime;
  lastActivityAt: DateTime;
  audioTrackSid?: string;
  videoTrackSid?: string;
  quality: {
    bandwidth: number;         // Mbps
    latency: number;           // ms
    packetLoss: number;        // %
    jitter: number;            // ms
    score: number;             // 0-1
  };
  recordingStatus: "idle" | "recording_local" | "none";
}
```

### LiveKitWebhookEvent
```typescript
{
  event: {
    type: "participant_joined" | "participant_left" | "track_published" | "track_unpublished" | "room_started" | "room_finished";
    createdAt: number;         // Unix timestamp ms
    room: {
      sid: string;
      emid: string;            // External room ID (our roomId)
      name: string;
      numParticipants: number;
      duration: number;        // seconds
    };
    participant?: {
      sid: string;             // LiveKit session ID
      identity: string;        // Our participantId
      state: string;           // "ACTIVE", "DISCONNECTED"
      duration: number;
      ingressId?: string;
      metadata: string;        // JSON stringified metadata
    };
    track?: {
      sid: string;
      type: "AUDIO" | "VIDEO";
      source: "MICROPHONE" | "SCREENSHARE" | "CAMERA";
      codec: string;
      duration: number;
      size: number;            // bytes
    };
  };
  createdAt: number;
  token: string;               // JWT from LiveKit for verification
}
```

### ActiveRecorderCounter
```typescript
// Key: room:{roomId}:active_recorders (Redis atomic counter)
{
  count: number;               // 0, 1, 2, etc.
  lastTransitionAt: DateTime;
  recordingTypes: ("local" | "server")[];
  participants: {
    [participantId]: {
      recordingType: "local" | "server";
      startedAt: DateTime;
    };
  };
}
```

### SignalRConnection
```typescript
{
  connectionId: string;        // SignalR assigned
  participantId: string;
  tenantId: string;
  roomId?: string;
  hubName: "room" | "tenant" | "admin";
  groups: string[];            // e.g., ["room-{roomId}", "tenant-{tenantId}"]
  authenticatedAt: DateTime;
  lastActivityAt: DateTime;
}
```

---

## Success Criteria

1. **Token Issuance Latency:** P95 < 200ms from HTTP request to token response
2. **Webhook Processing Latency:** P95 < 500ms from LiveKit webhook delivery to cache update + SignalR broadcast
3. **Roster Consistency:** Redis cache matches LiveKit participant list within 5 minutes (via reconciliation if needed)
4. **SignalR Broadcasting:** 100% of eligible participants receive roster/announcement events within 100ms
5. **Cache Hit Rate:** > 99% of state lookups satisfied from Redis (no L2 fallback to API)
6. **Webhook Idempotency:** Duplicate webhook delivery results in zero duplicate state updates
7. **Permission Changes:** Speaker grant approval/revocation reflected in new token within 1 second
8. **Graceful Degradation:** No user-visible errors during 5-minute webhook delivery outage; reconciliation restores state
9. **Availability:** Room state cache operational 99.95% uptime (target for self-hosted Kubernetes)
10. **Scalability:** Support 1000+ participants per room, 100+ concurrent rooms per tenant

---

## Edge Cases

1. **Simultaneous Join and Leave:** Participant connects to LiveKit and disconnects before webhook processed; cache must reflect final state (left) without showing intermediate (joined) state
2. **Webhook Out of Order:** participant_left received before participant_joined; handler must tolerate and reconcile
3. **Token Expiry During Active Connection:** Token expires after 6 hours but participant still connected; refresh must be proactive (at 5.5 hours)
4. **LiveKit Webhook Signature Invalid:** Reject webhook and log security event; do not update cache
5. **Participant Metadata Corrupted:** If metadata JSON parsing fails, log error and use defaults (role = Listener, no special permissions)
6. **Redis Connection Lost:** Cache unavailable; fall back to LiveKit API for state queries (with increased latency); buffer writes for replay
7. **Announcement State Race:** If multiple recording starts/stops occur in rapid succession (< 100ms), ensure counter transitions are applied atomically
8. **Room Ends While Participants Still Connected:** Cache TTL expires; webhooks may still arrive; handle gracefully without crashing
9. **SignalR Client Disconnection:** Client loses connection mid-broadcast; must reconnect and request current state (via new hub method)
10. **Recording Counter Goes Negative:** Defensive check in decrement to prevent negative values; log as critical error if detected

---

## Assumptions

1. **LiveKit Server Configured:** LiveKit is deployed and accessible via livekitUrl from tenant config; API key/secret available
2. **Redis Available:** Redis cluster operational for caching; data loss tolerance < 1 second (room state is ephemeral)
3. **SignalR Redis Backplane:** SignalR configured with Redis backplane for scalability across multiple server instances
4. **Webhook Signature Validation:** LiveKit API key is securely stored in config; webhook validation is cryptographically sound
5. **Participant Identity Consistency:** participantId in our system matches LiveKit identity field (stored in invite token metadata)
6. **Room State is Ephemeral:** Room state is not persisted to SQL Server; if server crashes, state is lost and room participants are disconnected (expected)
7. **Tenant Policy Engine Available:** Policy engine (from Identity module) provides role → permission mapping
8. **Invite Token Already Validated:** Join endpoint assumes invite token has been validated by auth middleware (signature, expiry, rate limits)
9. **Network Latency < 100ms:** Assumes typical latency to LiveKit server and Redis is < 100ms for SLA targets
10. **No Broadcast Ordering Guarantees:** SignalR does not guarantee message ordering across room group; clients must handle causality-aware reconciliation

---

## Open Questions & Decisions

1. **Speaking State Detection Method:** Use LiveKit track activity level vs. VAD-based state? (Recommend: track activity for simplicity)
2. **Quality Score Weighting:** Current formula weights bandwidth/latency/loss equally; should they be adjusted? (Recommend: empirical tuning based on early feedback)
3. **Webhook Retry Strategy:** Max retries, backoff curve, dead-letter handling? (Recommend: 5 retries, exponential backoff 2^n, DLQ after final failure)
4. **Redis Cluster Failover:** If primary Redis fails, is data loss acceptable? (Recommend: acceptable; room state is ephemeral)
5. **Token Expiry Duration:** 6 hours default; configurable per room/tenant? (Recommend: fixed 6 hours for Phase 1)
6. **Admin Controls Claim:** What specific actions should room_admin claim enable? (Recommend: scoped to recording control only; other admin actions require explicit checks)

