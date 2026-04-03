# Epic 5: Moderation Module

**Version:** 1.0
**Last Updated:** 2026-04-03
**Status:** Specification
**Module Owner:** Room Management & Safety
**Dependencies:** Epic 3 (Rooms), Epic 4 (Realtime Orchestration)

---

## Overview

The Moderation Module manages server-authoritative controls over room participant state and behavior. It handles hand raise queuing, speaker approval/revocation, moderator assignment, room locking, participant removal, and roster state tracking. All moderation actions are transactional, persisted, and broadcast to all participants via SignalR in real-time. The module enforces fine-grained authorization: only designated moderators can perform moderation actions.

---

## User Stories

### US-5.1: Participant Raises Hand to Request Speaking
**Priority:** P0 (Critical)
**Capability:** Participant submits hand raise request; request enters queue with timestamp ordering; moderators see ordered queue.

**As a** participant
**I want to** raise my hand to request speaking permission
**So that** the moderator can see I want to speak and I maintain my position in a fair queue

**Acceptance Scenarios:**

**Scenario 5.1.1: Hand Raise Submitted Successfully**
```gherkin
Given Alice is a participant (not moderator) in an active room
And Alice's role is "Listener" (currently cannot publish)
When Alice calls POST /rooms/{roomId}/handRaise with {action: "raise"}
Then the server creates HandRaiseRequest with:
  - id: unique UUID
  - participantId: Alice's ID
  - roomId: room ID
  - status: "Queued"
  - createdAt: current timestamp
  - position: current queue length + 1 (e.g., if 2 in queue, position = 3)
And returns 201 Created with: {handRaiseId, position: 3, estimatedWaitTime: "5 minutes"}
And broadcasts SignalR event to room: {event: "handraise.changed", action: "queued", participantId: "Alice", position: 3, totalInQueue: 3}
And the hand raise is persisted to database: Moderation.HandRaiseRequest table
And moderators see updated queue: [HandRaise1{pos:1}, HandRaise2{pos:2}, HandRaise3{Alice,pos:3}]
```

**Scenario 5.1.2: Hand Raise for Already-Speaking Participant - Rejected**
```gherkin
Given Bob is already an approved speaker with active SpeakerGrant
And Bob's role is "Panelist" with publish permission
When Bob calls POST /rooms/{roomId}/handRaise
Then the server returns 409 Conflict with error "already_speaking" or silently succeeds with position = 0 (no queue entry)
And Bob is not added to queue
And no broadcast occurs
```

**Scenario 5.1.3: Hand Raise Cancelled by Participant**
```gherkin
Given Alice has hand raised and is in queue at position 2
When Alice calls POST /rooms/{roomId}/handRaise/{handRaiseId} with {action: "cancel"}
Then the server updates HandRaiseRequest.status = "Cancelled"
And broadcasts SignalR event: {event: "handraise.changed", action: "cancelled", handRaiseId, participantId: "Alice"}
And remaining queue is re-ordered: [HandRaise1{pos:1}, HandRaise3{pos:2}] (Alice's position disappears)
And moderators see updated queue
```

**Scenario 5.1.4: Hand Raise Expires After Timeout**
```gherkin
Given hand raise request was created 30 minutes ago
And room has handRaiseTimeoutMinutes = 30 (configurable per room)
When orchestration runs hand raise expiry job (every 1 minute)
Then hand raise is marked with status = "Expired"
And broadcasts SignalR event: {event: "handraise.changed", action: "expired", handRaiseId}
And queue is re-ordered (expired entry removed)
And participant is notified via SignalR: "Your hand raise request expired"
```

**Scenario 5.1.5: Moderator Views Hand Raise Queue**
```gherkin
Given moderator "Carol" is in the room with role = "Moderator"
And there are 3 active hand raises in queue
When Carol calls GET /rooms/{roomId}/handRaises with {status: "Queued"}
Or Carol subscribes to SignalR room hub and receives handraise.changed events
Then server returns ordered list: [
  {id: "hr1", participantId: "Alice", participantName: "Alice Smith", position: 1, createdAt, estimatedWaitTime},
  {id: "hr2", participantId: "Bob", position: 2, createdAt, estimatedWaitTime},
  {id: "hr3", participantId: "Dave", position: 3, createdAt, estimatedWaitTime}
]
And ordering is by createdAt ascending (FIFO)
And Carol can see each participant's duration in queue
```

---

### US-5.2: Moderator Approves Speaker and Grants Publish Permission
**Priority:** P0 (Critical)
**Capability:** Moderator approves hand raise; SpeakerGrant is created; participant receives new LiveKit token with publish permission; queue updates.

**As a** moderator
**I want to** approve a participant's hand raise and grant them speaking permission
**So that** they can publish audio and other participants hear them

**Acceptance Scenarios:**

**Scenario 5.2.1: Approve Hand Raise and Grant Speaker Permission**
```gherkin
Given Alice has hand raised and is at position 1 in queue
And Carol is the moderator
When Carol calls POST /rooms/{roomId}/handRaises/{handRaiseId}/approve with {action: "approve"}
Then the server validates Carol is moderator (authorization check)
And creates SpeakerGrant with:
  - id: unique UUID
  - handRaiseId: link to hand raise
  - participantId: Alice
  - roomId: room ID
  - status: "Active"
  - grantedAt: current timestamp
  - grantedBy: Carol's participant ID
  - expiresAt: current time + grant duration (or null if no timeout)
And updates HandRaiseRequest.status = "Approved"
And persists SpeakerGrant to database: Moderation.SpeakerGrant table
And calls Realtime Orchestration to issue new LiveKit token with [subscribe, publish, publish_data] claims
And sends token to Alice via SignalR: {event: "token.refreshed", token, reason: "speaker_grant_approved"}
And broadcasts to room: {event: "handraise.changed", action: "approved", participantId: "Alice", position: "approved"}
And queue is re-ordered: position 1 is now empty, remaining items shift up: [HandRaise2{pos:1}, HandRaise3{pos:2}]
And Carol sees updated queue
And Alice can now publish audio
```

**Scenario 5.2.2: Reject Hand Raise**
```gherkin
Given Bob has hand raised at position 2
When Carol calls POST /rooms/{roomId}/handRaises/{handRaiseId}/reject with {action: "reject", reason: "off_topic"}
Then the server updates HandRaiseRequest.status = "Rejected"
And no SpeakerGrant is created
And broadcasts SignalR: {event: "handraise.changed", action: "rejected", handRaiseId, participantId: "Bob", reason}
And Bob is notified: "Your hand raise was rejected"
And queue re-orders: [HandRaise1{pos:1}, HandRaise3{pos:2}] (Bob removed)
And Bob's publish permission remains unchanged (still cannot publish)
```

**Scenario 5.2.3: Approve Multiple Speakers in Sequence**
```gherkin
Given queue has: [HR1-Alice{pos:1}, HR2-Bob{pos:2}, HR3-Carol{pos:3}]
When Carol (moderator) approves HR1-Alice
Then Alice is approved, queue updates: [HR2-Bob{pos:1}, HR3-Carol{pos:2}]
And Alice receives new token with publish permission
When Carol approves HR2-Bob
Then Bob is approved, Alice's token remains valid
And queue updates: [HR3-Carol{pos:1}]
And now Alice and Bob can both publish audio simultaneously
```

**Scenario 5.2.4: Approve Hand Raise - Room Moderator Himself**
```gherkin
Given Carol is the room moderator
And Carol submits a hand raise (e.g., to promote herself from observer to panelist)
When Carol calls approve on her own hand raise
Then the server allows the self-approval (or denies based on policy)
If allowed: SpeakerGrant is created, Carol receives new token with publish permission
If denied: returns 403 Forbidden with error "cannot_self_approve"
(Recommend: allow self-approval for moderators to maintain flexibility)
```

---

### US-5.3: Moderator Revokes Speaker Permission
**Priority:** P0 (Critical)
**Capability:** Moderator revokes active speaker grant; participant's audio track is unpublished; new token issued without publish permission.

**As a** moderator
**I want to** revoke a speaker's permission to silence them or restore order
**So that** they stop publishing audio immediately

**Acceptance Scenarios:**

**Scenario 5.3.1: Revoke Active Speaker Grant**
```gherkin
Given Alice is currently speaking with active SpeakerGrant.status = "Active"
And Alice's audio track is published in LiveKit
When Carol (moderator) calls POST /rooms/{roomId}/speakers/{speakerGrantId}/revoke with {action: "revoke", reason: "off_topic"}
Then the server updates SpeakerGrant.status = "Revoked"
And sets SpeakerGrant.revokedAt = current timestamp
And sets SpeakerGrant.revokedBy = Carol's ID
And issues new LiveKit token to Alice with claims: [subscribe, publish_data] (no publish)
And sends token via SignalR: {event: "token.refreshed", token, reason: "speaker_grant_revoked"}
And broadcasts to room: {event: "speaker.revoked", participantId: "Alice", revokedBy: "Carol", reason}
And when Alice tries to publish with old token, LiveKit rejects with PermissionDenied
And when Alice uses new token, publish is blocked
And Alice's audio track transitions to unpublished (LiveKit stops relaying her audio)
And server logs revocation for audit: {timestamp, moderatorId: Carol, participantId: Alice, reason}
```

**Scenario 5.3.2: Revoke Non-Active Speaker - No-Op**
```gherkin
Given Bob has SpeakerGrant.status = "Revoked" (already revoked)
When Carol attempts to revoke the same grant again
Then the server returns 409 Conflict with error "already_revoked"
And no state change occurs
And audit log reflects the rejected attempt
```

**Scenario 5.3.3: Revoke Causes Queue Position Updates**
```gherkin
Given Alice at position 1 (approved/active), Bob at position 1 (waiting in queue - from original HR2)
When Alice is revoked
Then Alice's SpeakerGrant.status = "Revoked" but she is NOT automatically returned to queue
And Bob remains at position 1 in queue (or becomes first in queue if positions are re-normalized)
And Bob can now be approved for the next speaking slot
(Note: revoked speaker does not automatically re-queue; they must raise hand again if desired)
```

---

### US-5.4: Roster State Management
**Priority:** P0 (Critical)
**Capability:** Server maintains authoritative roster with participant presence, role, speaking status, recording status, and quality indicators.

**As a** room participant
**I want to** see current roster with each participant's role, speaking status, and recording status
**So that** I know who is in the room and who is actively speaking or recording

**Acceptance Scenarios:**

**Scenario 5.4.1: Roster Shows Participant Role and Speaking Status**
```gherkin
Given a room has participants: Alice (Panelist, speaking), Bob (Moderator, listening), Carol (Listener, listening)
When any participant calls GET /rooms/{roomId}/roster
Or subscribes to SignalR and receives roster.changed events
Then server returns roster: [
  {participantId: "Alice", role: "Panelist", speakingStatus: "speaking", recordingStatus: "none", qualityScore: 0.95},
  {participantId: "Bob", role: "Moderator", speakingStatus: "listening", recordingStatus: "local", qualityScore: 0.92},
  {participantId: "Carol", role: "Listener", speakingStatus: "listening", recordingStatus: "none", qualityScore: 0.88}
]
And speaking status is updated in real-time via SignalR when any participant changes status
```

**Scenario 5.4.2: Roster Reflects Permission Changes**
```gherkin
Given Alice has role = "Listener" with speakerGrant = null
And Alice's SpeakerGrant is approved
Then roster entry for Alice is updated: {speakerGrant: {id, grantedAt, expiresAt}, speaking_eligible: true}
And SignalR broadcasts roster.changed event
When Alice is revoked
Then roster entry is updated: {speakerGrant: null, speaking_eligible: false}
And SignalR broadcasts roster.changed event
```

**Scenario 5.4.3: Roster Includes Recording Status**
```gherkin
Given Alice registers local recording, Bob starts server recording
When roster is queried or broadcast
Then roster includes: [
  {participantId: "Alice", recordingStatus: "local", recordingStartedAt},
  {participantId: "Bob", recordingStatus: "server", recordingStartedAt},
  {participantId: "Carol", recordingStatus: "none"}
]
When Alice stops local recording
Then recordingStatus reverts to "none" and broadcast occurs
```

**Scenario 5.4.4: Roster Display Respects Privacy (Guest Listeners Cannot See Details)**
```gherkin
Given a room allows guest listeners to join
And guests have restricted permissions
When a guest calls GET /rooms/{roomId}/roster
Then the server returns roster WITHOUT sensitive details:
  - No internal IDs (only display names)
  - No quality scores
  - No recording status (for privacy)
  - Simple presence: {displayName, role: "Panelist" | "Listener", isSpeaking: true | false}
Or the server returns 403 Forbidden if guests are not allowed to see roster
```

---

### US-5.5: Moderator Transfers Role
**Priority:** P1 (High)
**Capability:** Current moderator transfers moderator role to another participant; new moderator gains moderation permissions.

**As a** moderator
**I want to** transfer moderator role to another participant
**So that** if I need to leave, someone else can manage the room

**Acceptance Scenarios:**

**Scenario 5.5.1: Moderator Hands Over to Another Participant**
```gherkin
Given Carol is the current room moderator
And Alice is a panelist in the same room
When Carol calls POST /rooms/{roomId}/moderator/handover with {targetParticipantId: "Alice"}
Then the server validates Carol is current moderator
And creates new ModeratorAssignment: {participantId: "Alice", assignedAt, assignedBy: "Carol"}
And updates old assignment for Carol: {endedAt: current timestamp}
And broadcasts SignalR: {event: "moderator.changed", newModerator: "Alice", previousModerator: "Carol"}
And issues new LiveKit token to Alice with admin_controls claim
And sends updated token via SignalR
And Alice now has permission to approve/reject hand raises, revoke speakers, lock room, etc.
And Carol's admin_controls claim is removed on next token refresh
And audit log records: {timestamp, action: "moderator_transfer", from: "Carol", to: "Alice"}
```

**Scenario 5.5.2: Moderator Leaves Without Explicit Handover**
```gherkin
Given Carol is the room moderator
And Alice is another panelist
When Carol's connection drops or calls POST /rooms/{roomId}/leave
And Carol's participant state is updated to "left"
And Carol's moderator role is still assigned but participant is offline
Then either:
  - Option A: Role auto-transfers to the longest-connected panelist (Alice)
  - Option B: Room becomes unmoderated until Carol returns or someone explicitly takes role
(Recommend: Option A for better UX - automatic fallback to next eligible participant)
```

---

### US-5.6: Lock Room to Prevent New Joins
**Priority:** P1 (High)
**Capability:** Moderator can lock room to prevent new participants from joining while allowing current members to stay.

**As a** moderator
**I want to** lock the room to prevent new participants from joining
**So that** I can ensure discussion privacy after a certain point

**Acceptance Scenarios:**

**Scenario 5.6.1: Lock Room Successfully**
```gherkin
Given Carol is the moderator in an active room
And the room is currently unlocked
When Carol calls POST /rooms/{roomId}/lock with {locked: true}
Then the server updates Room.locked = true
And persists to database: Rooms.Room table
And broadcasts SignalR: {event: "room.locked", lockedBy: "Carol", timestamp}
And all participants see UI indication "Room is locked"
When a new participant tries to join via POST /rooms/{roomId}/join
Then the server checks Room.locked flag
And returns 403 Forbidden with error "room_locked"
And existing participants remain connected and unaffected
```

**Scenario 5.6.2: Unlock Room**
```gherkin
Given room is locked
When Carol calls POST /rooms/{roomId}/lock with {locked: false}
Then Room.locked = false is persisted
And broadcasts SignalR: {event: "room.unlocked", unlockedBy: "Carol"}
And new participants can join again via normal flow
```

---

### US-5.7: Moderator Removes Participant
**Priority:** P1 (High)
**Capability:** Moderator can force-remove a participant from the room for policy violations or disruption.

**As a** moderator
**I want to** remove a disruptive participant from the room
**So that** I can maintain room integrity and protect other participants

**Acceptance Scenarios:**

**Scenario 5.7.1: Remove Participant Successfully**
```gherkin
Given Carol is the moderator
And Bob is a participant currently speaking
When Carol calls POST /rooms/{roomId}/participants/{participantId}/remove with {reason: "disruptive_behavior"}
Then the server validates Carol is moderator
And updates Bob's participant state: removed = true, removedAt = current timestamp, removedBy = "Carol"
And marks Bob's LiveKit session for termination (signal to client to disconnect)
And broadcasts SignalR: {event: "participant.removed", participantId: "Bob", reason: "disruptive_behavior", removedBy: "Carol"}
And Bob's client receives signal and disconnects from LiveKit
And Bob's entry is removed from roster
And audit log records: {timestamp, action: "participant_removed", moderatorId: "Carol", participantId: "Bob", reason}
And if Bob had active SpeakerGrant, it is revoked automatically
```

**Scenario 5.7.2: Remove Participant Who is Not in Room**
```gherkin
Given Dave was previously removed from room
When Carol attempts to remove Dave again
Then the server returns 404 Not Found or 409 Conflict
And no state change occurs
```

---

### US-5.8: Speaker Grant Expiry
**Priority:** P2 (Medium)
**Capability:** Speaker grants can have optional timeout; if participant doesn't speak within duration, grant auto-expires.

**As a** moderator
**I want to** grant someone speaking permission for a limited time (e.g., 5 minutes)
**So that** they have a bounded speaking slot and I can move to the next speaker

**Acceptance Scenarios:**

**Scenario 5.8.1: Create Speaker Grant with Expiry**
```gherkin
Given Carol approves Alice's hand raise
When Carol specifies grant duration = 300 seconds (5 minutes)
Then SpeakerGrant is created with:
  - status: "Active"
  - grantedAt: current timestamp
  - expiresAt: current timestamp + 300s
And broadcasts: {event: "speaker.approved", participantId: "Alice", expiresAt}
And Alice sees UI notification: "You have 5 minutes to speak"
When the expiry time approaches (e.g., 30s remaining), sends reminder to Alice
```

**Scenario 5.8.2: Speaker Grant Expires Automatically**
```gherkin
Given Alice's SpeakerGrant.expiresAt = current timestamp + 300s
When orchestration's speaker grant expiry job runs (every 30s)
And current timestamp > expiresAt
Then SpeakerGrant.status = "Expired"
And orchestration issues new token to Alice without publish claim
And broadcasts: {event: "speaker.expired", participantId: "Alice"}
And Alice cannot publish audio anymore
And audit log records the expiry
```

**Scenario 5.8.3: Speaker Manually Stops Speaking Before Expiry**
```gherkin
Given Alice has active SpeakerGrant with expiresAt in 4 minutes
When Alice unpublishes her audio track
Or Alice calls POST /rooms/{roomId}/speakers/{grantId}/release with {action: "stop_speaking"}
Then the server updates SpeakerGrant.status = "Completed" (or "Released")
And broadcasts: {event: "speaker.completed", participantId: "Alice"}
And next person in queue can be approved
And Alice's grant does not expire; it is marked as voluntarily released
```

---

## Functional Requirements

### F5.1: Hand Raise Management
- **F5.1.1:** Hand raise request must include: participantId, roomId, createdAt, status (Queued, Approved, Rejected, Cancelled, Expired)
- **F5.1.2:** Hand raise requests must be stored in database: Moderation.HandRaiseRequest table with fields: id, roomId, participantId, status, createdAt, position, expiresAt, rejectionReason
- **F5.1.3:** Queue ordering must be FIFO: sorted by createdAt ascending, with position calculated as rank within status = Queued
- **F5.1.4:** Position must be recalculated when any hand raise changes status (approve, reject, cancel, expire)
- **F5.1.5:** Hand raises must expire after configurable duration (default 30 minutes, configurable per room)
- **F5.1.6:** Expiry job must run at least every 60 seconds; when hand raise exceeds expiresAt, auto-update status to "Expired"
- **F5.1.7:** Participant can have only one active hand raise at a time (status = Queued); attempt to raise again returns error
- **F5.1.8:** Already-speaking participants (with active SpeakerGrant) cannot raise hand; request is rejected or silently ignored
- **F5.1.9:** Hand raise cancellation must update status immediately and broadcast queue change
- **F5.1.10:** GET /rooms/{roomId}/handRaises endpoint must support filtering by status and return ordered list with position and estimatedWaitTime
- **F5.1.11:** POST /rooms/{roomId}/handRaises with {action: "raise"} must create request and return {handRaiseId, position, estimatedWaitTime}
- **F5.1.12:** POST /rooms/{roomId}/handRaises/{handRaiseId} with {action: "cancel"} must allow participant to cancel own request or moderator to cancel any request

### F5.2: Speaker Grant Management
- **F5.2.1:** Speaker grant must include: id, handRaiseId, participantId, roomId, status (Active, Revoked, Expired, Completed), grantedAt, expiresAt, grantedBy, revokedAt, revokedBy
- **F5.2.2:** Speaker grants must be stored in database: Moderation.SpeakerGrant table
- **F5.2.3:** Only moderators can create speaker grants (via hand raise approval)
- **F5.2.4:** Grant creation must trigger LiveKit token reissuance with publish claim to the granted participant
- **F5.2.5:** Token must be delivered via SignalR: {event: "token.refreshed", token, reason: "speaker_grant_approved"}
- **F5.2.6:** Revocation must update status = "Revoked" and issue new token without publish claim within 1 second
- **F5.2.7:** Revoked participant must not be able to publish using old token (LiveKit enforces via claim expiry)
- **F5.2.8:** Grant expiry (if configured) must auto-update status = "Expired" and trigger token refresh
- **F5.2.9:** Only one speaker grant can be active per participant at a time (enforced at creation)
- **F5.2.10:** Participant removal auto-revokes active speaker grant
- **F5.2.11:** GET /rooms/{roomId}/speakers endpoint must return list of active speakers with grant details
- **F5.2.12:** POST /rooms/{roomId}/handRaises/{handRaiseId}/approve with optional {expiresIn: seconds} creates grant with expiry
- **F5.2.13:** POST /rooms/{roomId}/speakers/{speakerGrantId}/revoke with {reason} immediately revokes and broadcasts

### F5.3: Roster State Management
- **F5.3.1:** Roster is cached in Redis and updated in real-time
- **F5.3.2:** Each participant roster entry includes: participantId, identity, role, speakingStatus, recordingStatus, qualityScore, joinedAt, speakerGrantId (if active)
- **F5.3.3:** Role must be one of: "Moderator", "Panelist", "Listener", "Recorder" (from Identity module)
- **F5.3.4:** Speaking status must be updated based on LiveKit track activity (from Realtime Orchestration)
- **F5.3.5:** Recording status tracks: "local", "server", or "none" (from Recording Consent module)
- **F5.3.6:** GET /rooms/{roomId}/roster must return filtered roster based on caller's permissions
  - Moderators: see all details
  - Panelists/Listeners: see roster without internal IDs, possibly without quality scores
  - Guest listeners: no roster access (configurable per tenant policy)
- **F5.3.7:** Roster changes (participant join/leave, speaker grant, revocation) must broadcast via SignalR: roster.changed event
- **F5.3.8:** Broadcast must include only delta (what changed) to minimize payload
- **F5.3.9:** Roster must be available for duration of room plus configurable retention (default 24 hours after room ends)

### F5.4: Moderator Assignment and Handover
- **F5.4.1:** Each room must have exactly one active moderator at any time
- **F5.4.2:** Moderator assignment stored in database: Moderation.ModeratorAssignment with fields: id, roomId, participantId, assignedAt, assignedBy, endedAt
- **F5.4.3:** Moderator is determined by most recent ModeratorAssignment where endedAt is null
- **F5.4.4:** Only current moderator can perform moderation actions: approve/reject hand raises, revoke speakers, lock room, remove participants, transfer role
- **F5.4.5:** Handover must update old assignment (set endedAt) and create new assignment atomically
- **F5.4.6:** New moderator must receive new LiveKit token with admin_controls claim (via Realtime Orchestration)
- **F5.4.7:** Handover must be broadcast via SignalR: {event: "moderator.changed", newModerator, previousModerator}
- **F5.4.8:** POST /rooms/{roomId}/moderator/handover with {targetParticipantId} transfers role to target
- **F5.4.9:** If moderator disconnects, room may auto-assign role to next eligible participant (configurable) or become unmoderated
- **F5.4.10:** Audit log must record all moderator assignments and transfers

### F5.5: Room Lock Control
- **F5.5.1:** Room must have field: locked (boolean, default = false)
- **F5.5.2:** Only moderator can lock/unlock room
- **F5.5.3:** POST /rooms/{roomId}/lock with {locked: true} sets lock and broadcasts event
- **F5.5.4:** Locked room rejects join requests (F4.1.3 in Realtime Orchestration)
- **F5.5.5:** Lock state must be included in room metadata and available via GET /rooms/{roomId}
- **F5.5.6:** Lock state changes must be broadcast via SignalR: room.locked or room.unlocked events
- **F5.5.7:** Existing participants are unaffected by lock; can remain connected and speak

### F5.6: Participant Removal
- **F5.6.1:** Only moderator can remove a participant
- **F5.6.2:** Removal must update participant state: removed = true, removedAt, removedBy (participantId of moderator)
- **F5.6.3:** Removal must trigger revocation of any active SpeakerGrant for that participant
- **F5.6.4:** Removal must signal participant's client to disconnect (via SignalR or API)
- **F5.6.5:** Broadcast removal event via SignalR: {event: "participant.removed", participantId, reason, removedBy}
- **F5.6.6:** Removed participant is excluded from roster
- **F5.6.7:** POST /rooms/{roomId}/participants/{participantId}/remove with {reason} performs removal
- **F5.6.8:** Audit log must record: timestamp, moderatorId, removedParticipantId, reason

### F5.7: Authorization and Server Authoritability
- **F5.7.1:** All moderation actions must validate that caller is the current moderator (via JWT token claim or role check)
- **F5.7.2:** Moderation service must NOT trust client-provided claims; must query ModeratorAssignment to confirm authority
- **F5.7.3:** Non-moderators attempting moderation actions must receive 403 Forbidden
- **F5.7.4:** Participant state changes (hand raise, revocation, removal) must be server-authoritative; client cannot force state changes
- **F5.7.5:** All moderation decisions must be persisted to database before broadcasting to clients
- **F5.7.6:** State changes must be idempotent: repeating the same action twice has same effect as once

### F5.8: Broadcasting and Real-Time Sync
- **F5.8.1:** All state changes must broadcast to room hub via SignalR within 100ms
- **F5.8.2:** Broadcast events must include: event name, action, affected participantId, timestamp, and relevant details
- **F5.8.3:** SignalR group name for room: "room-{roomId}"
- **F5.8.4:** Broadcasts must be resilient to network issues (SignalR handles retry and delivery guarantees)

---

## Key Entities

### HandRaiseRequest
```typescript
{
  id: string;                           // UUID
  roomId: string;                       // FK to Rooms.Room
  participantId: string;                // FK to Identity.Participant (our internal ID)
  status: "Queued" | "Approved" | "Rejected" | "Cancelled" | "Expired";
  createdAt: DateTime;                  // ISO 8601
  position: number;                     // Rank within Queued status (1-based)
  expiresAt: DateTime;                  // createdAt + handRaiseTimeoutMinutes
  rejectionReason?: string;             // e.g., "off_topic", "speaker_limit"
  cancelledAt?: DateTime;
  approvedAt?: DateTime;                // When status changed to Approved
  expiredAt?: DateTime;                 // When status changed to Expired
}
```

### SpeakerGrant
```typescript
{
  id: string;                           // UUID
  handRaiseId?: string;                 // FK to HandRaiseRequest (may be null if grant created directly)
  participantId: string;                // FK to Identity.Participant
  roomId: string;                       // FK to Rooms.Room
  status: "Active" | "Revoked" | "Expired" | "Completed";
  grantedAt: DateTime;
  grantedBy: string;                    // Moderator participantId
  expiresAt?: DateTime;                 // Optional timeout duration
  revokedAt?: DateTime;
  revokedBy?: string;                   // Moderator who revoked
  completedAt?: DateTime;               // When participant voluntarily stopped speaking
  durationSeconds?: number;             // How long grant was active before revocation/expiry
}
```

### RoomParticipantState
```typescript
{
  participantId: string;
  identity: string;
  role: "Moderator" | "Panelist" | "Listener" | "Recorder";
  joinedAt: DateTime;
  speakingStatus: "speaking" | "listening";
  speakingStartedAt?: DateTime;
  recordingStatus: "local" | "server" | "none";
  recordingStartedAt?: DateTime;
  qualityScore: number;                 // 0-1
  speakerGrantId?: string;              // Current active grant, if any
  speakerGrantExpiresAt?: DateTime;
  removed: boolean;
  removedAt?: DateTime;
  removedBy?: string;
}
```

### ModeratorAssignment
```typescript
{
  id: string;                           // UUID
  roomId: string;                       // FK to Rooms.Room
  participantId: string;                // FK to Identity.Participant (current moderator)
  assignedAt: DateTime;
  assignedBy?: string;                  // Moderator who assigned (null if system-assigned)
  endedAt?: DateTime;                   // Null if assignment is still active
}
```

### Room (extends from Epic 3)
```typescript
// Additional fields for moderation:
{
  // ... fields from Epic 3 ...
  locked: boolean;                      // Default false
  handRaiseTimeoutMinutes: number;      // Default 30
  currentModeratorId: string;           // Cached lookup of ModeratorAssignment where endedAt = null
}
```

---

## Success Criteria

1. **Hand Raise Queue:** Queue ordering accurate (FIFO), position updates immediate, expiry within 1 minute of configured timeout
2. **Speaker Approval Latency:** Moderator approval → token refresh → participant can publish within 1 second (P95)
3. **Revocation Latency:** Revocation → new token delivered → publish blocked within 1 second
4. **Authorization:** Non-moderators rejected from all moderation actions; all auth checks succeed 100% of the time
5. **State Persistence:** All state changes persisted to database before response returned
6. **Broadcasting:** 100% of room participants receive roster/hand raise/speaker events within 100ms
7. **Roster Consistency:** Roster matches ground truth (LiveKit + database) within 5 minutes (via reconciliation if needed)
8. **Audit Trail:** 100% of moderation actions logged with timestamp, actor, action, details
9. **Idempotency:** Duplicate requests for same action produce same result (no double-grants, etc.)
10. **Scalability:** Support 500+ participants per room, 1000+ concurrent hand raises system-wide

---

## Edge Cases

1. **Simultaneous Hand Raises:** Two participants raise hand at same millisecond; ordering must be deterministic (by participantId as tiebreaker)
2. **Approve While Participant Offline:** Moderator approves hand raise while participant's connection dropped; token refresh is queued; when participant reconnects, receives token via reconnection handshake
3. **Moderator Removes Self:** Moderator calls remove on own participantId; system auto-assigns role to next eligible participant (if configured)
4. **Revocation While Speaking:** Participant is mid-sentence when revoked; audio track unpublishes immediately (may cut off mid-word, acceptable)
5. **Queue Re-ordering During Approval:** While Carol is approving position 1, position 2 cancels and position 3 expires; position calculation must remain consistent
6. **Role Change Mid-Grant:** Participant's role changes (e.g., Listener → Panelist) while SpeakerGrant is active; grant remains active with updated role
7. **Hand Raise After Removal:** Participant is removed, then tries to raise hand; system rejects with error "not_in_room"
8. **Moderator Handover to Removed Participant:** Moderator attempts to transfer role to participant who is no longer in room; system rejects with error
9. **Lock + Remove Combination:** Moderator locks room and removes a participant simultaneously; lock prevents new joins, removal is processed atomically
10. **Grant Expiry While Participant Offline:** Participant's grant expires while connection is dropped; when they reconnect, old token is invalid (needs refresh)

---

## Assumptions

1. **Moderator Authority:** Moderator identity is verified by auth middleware; moderation service trusts JWT claim that caller is moderator
2. **Database Availability:** SQL Server is always available; if down, moderation actions fail with 503
3. **SignalR Delivery:** SignalR guarantees message delivery to connected clients (with retry); no custom delivery tracking needed
4. **Participant Exists:** All participantIds referenced exist in Identity module; no validation needed at Moderation level (FK constraint in DB)
5. **Room State Available:** Room state is cached in Redis (from Realtime Orchestration); moderation service reads from cache
6. **LiveKit Integration:** Realtime Orchestration handles token reissuance; moderation service calls into Orchestration API
7. **Policy Engine Available:** Identity module provides role → permission mapping; no custom logic in moderation service
8. **Clock Synchronization:** Server clocks are synchronized (NTP); timestamps are comparable across services
9. **No Manual Role Assignment in Phase 1:** Role is set via Identity module; moderation cannot change participant role (reserved for future)
10. **Hand Raise Timeout Configurable:** Each tenant/room can configure handRaiseTimeoutMinutes; service respects this config

---

## Open Questions & Decisions

1. **Auto-Moderator Fallback:** If moderator disconnects, auto-assign to longest-connected panelist or leave unmoderated? (Recommend: Auto-assign for better UX)
2. **Multiple Simultaneous Speakers:** Can multiple speakers be active at once? (Recommend: Yes - each participant can have own grant, no global limit)
3. **Queue Fairness:** If hand raise expires, can participant immediately re-raise? (Recommend: Yes, but with exponential backoff to prevent spam)
4. **Moderator Self-Approval:** Can moderator raise hand and approve their own? (Recommend: Yes for flexibility)
5. **Speaker Grant Duration:** Is grant timeout mandatory or optional? (Recommend: Optional, default null for infinite)
6. **Removal from Queue:** When participant is removed from room, auto-cancel their hand raise? (Recommend: Yes, clean removal)
7. **Recording Impact on Queue:** Does participant recording status affect hand raise eligibility? (Recommend: No impact; recording is orthogonal)
8. **Roster Visibility:** Should guest listeners see roster? (Recommend: No roster access for guests, policy-driven)

