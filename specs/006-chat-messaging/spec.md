# Epic 6: Chat & Messaging Module

**Version:** 1.0
**Last Updated:** 2026-04-03
**Status:** Specification
**Module Owner:** Communication & Messaging
**Dependencies:** Epic 3 (Rooms), Epic 4 (Realtime Orchestration)

---

## Overview

The Chat & Messaging Module manages text communication within rooms and between moderators and individual participants. It supports room-wide chat (broadcast to all) and moderator direct messages (1:1 private). Messages are persisted to SQL Server, delivery status is tracked, and real-time updates are broadcast via SignalR. Messages remain available for the duration of the room plus a configurable retention period. Permission enforcement ensures only authorized participants can send or read chat based on their role and room policy.

---

## User Stories

### US-6.1: Participant Sends Room-Wide Chat Message
**Priority:** P0 (Critical)
**Capability:** Any participant with chat permission can send a text message visible to all room participants.

**As a** participant
**I want to** send a chat message to the entire room
**So that** I can ask questions or share thoughts with all panelists and listeners

**Acceptance Scenarios:**

**Scenario 6.1.1: Send Chat Message Successfully**
```gherkin
Given Alice is a participant in active room "Panel-Q&A"
And Alice's role has chat permission enabled (Panelist or Listener)
When Alice sends POST /rooms/{roomId}/chat with {messageType: "room_wide", content: "What about scalability?"}
Then the server creates ChatMessage with:
  - id: unique UUID
  - roomId: room ID
  - participantId: Alice's ID
  - messageType: "room_wide"
  - content: "What about scalability?"
  - sentAt: current timestamp
  - deliveryStatus: "sent"
And persists to database: Chat.ChatMessage table
And broadcasts SignalR event to room group: {event: "chat.message_sent", messageId, participantId: "Alice", content, sentAt}
And returns 201 Created with: {messageId, sentAt, deliveryStatus: "sent"}
And within 100ms, all connected participants in room receive the message via SignalR
```

**Scenario 6.1.2: Send Chat Message - Listener with Chat Disabled**
```gherkin
Given Bob is a listener in the room
And room policy disables chat for listeners
When Bob calls POST /rooms/{roomId}/chat with {messageType: "room_wide", content: "..."}
Then the server returns 403 Forbidden with error "chat_not_permitted"
And no message is created or persisted
And audit log records the rejected attempt
```

**Scenario 6.1.3: Send Chat Message - Guest Listener Cannot Send**
```gherkin
Given Carol is a guest listener (observer-only role)
And guest listeners have no chat permission by default
When Carol attempts POST /rooms/{roomId}/chat
Then the server returns 403 Forbidden with error "guest_cannot_chat"
And Carol can still receive chat messages if room broadcasts to guests (policy-dependent)
```

**Scenario 6.1.4: Message Content Validation**
```gherkin
Given Alice sends a message
When the message content exceeds max length (e.g., 5000 characters)
Then the server returns 400 Bad Request with error "message_too_long"
When message content is empty or whitespace-only
Then the server returns 400 Bad Request with error "message_empty"
When message contains only whitespace
Then the server trims and validates; if empty after trim, rejects
```

**Scenario 6.1.5: Rapid Message Sending - Rate Limit**
```gherkin
Given Alice sends multiple messages in rapid succession
When Alice sends > 10 messages in 10 seconds (rate limit: 1 msg/sec average)
Then the server returns 429 Too Many Requests with error "rate_limit_exceeded"
And Alice's subsequent messages are rejected until rate limit window expires
And server logs the rate limit violation
```

---

### US-6.2: Moderator Sends Direct Message to Participant
**Priority:** P0 (Critical)
**Capability:** Moderator can send private 1:1 messages to individual participants; only recipient and moderator can see the message.

**As a** moderator
**I want to** send a private message to a participant
**So that** I can provide individual guidance, feedback, or moderation notices without distracting the room

**Acceptance Scenarios:**

**Scenario 6.2.1: Moderator Sends Direct Message**
```gherkin
Given Carol is the room moderator
And Alice is a participant in the room
When Carol sends POST /rooms/{roomId}/chat with {messageType: "direct_message", targetParticipantId: "Alice", content: "Please mute background noise"}
Then the server validates Carol is moderator (authorization check)
And creates ChatMessage with:
  - messageType: "direct_message"
  - senderId: Carol's ID
  - recipientId: Alice's ID
  - roomId: room ID
  - content: "Please mute background noise"
  - sentAt: current timestamp
  - deliveryStatus: "sent"
And persists to database: Chat.ChatMessage table
And broadcasts SignalR event ONLY to Alice and Carol: {event: "chat.message_sent", messageType: "direct_message", senderId: "Carol", content, sentAt}
And returns 201 Created with: {messageId, deliveryStatus: "sent"}
And other room participants do NOT receive this message (private)
```

**Scenario 6.2.2: Non-Moderator Attempts Direct Message - Rejected**
```gherkin
Given Bob is a panelist (not moderator)
And Alice is another panelist
When Bob attempts POST /rooms/{roomId}/chat with {messageType: "direct_message", targetParticipantId: "Alice", content: "..."}
Then the server returns 403 Forbidden with error "only_moderator_can_send_dm"
And no message is created
```

**Scenario 6.2.3: Direct Message to Non-Existent Participant**
```gherkin
Given Carol attempts to send DM to participantId "unknown"
When the participant is not in the room
Then the server returns 404 Not Found with error "participant_not_found"
And no message is created
```

**Scenario 6.2.4: Direct Message Delivery Status - Recipient Offline**
```gherkin
Given Carol sends DM to Alice
But Alice's connection is dropped before message delivery
When Alice reconnects after 2 minutes
Then the server queues the message for delivery
And upon Alice's reconnection, sends message via SignalR
And updates deliveryStatus: "delivered" (or "read" if Alice opens it)
And Carol can track message status in her chat history
```

---

### US-6.3: Message Delivery Status Tracking
**Priority:** P1 (High)
**Capability:** Each message tracks delivery lifecycle: sent → delivered → read (optional).

**As a** participant
**I want to** know if my message was received and read by the recipient
**So that** I understand if my message reached its destination

**Acceptance Scenarios:**

**Scenario 6.3.1: Message Delivery Status Progression**
```gherkin
Given Alice sends a room-wide message "Question for moderator"
When message is created and persisted
Then deliveryStatus = "sent"
And SignalR broadcasts message to all participants
When participant Bob's client receives the message
Then the client acknowledges receipt via SignalR: {event: "chat.message_delivered", messageId}
And server updates ChatMessage.deliveryStatus = "delivered"
When Bob reads the message in his chat UI (implementation-specific)
And client sends optional: {event: "chat.message_read", messageId}
And server updates ChatMessage.deliveryStatus = "read"
And sender Alice sees all statuses in her chat history UI
```

**Scenario 6.3.2: Delivery Status for Direct Messages**
```gherkin
Given Carol sends DM to Alice
When message is created: deliveryStatus = "sent"
When message is broadcast to Alice: deliveryStatus = "delivered"
When Alice reads DM: deliveryStatus = "read"
And Carol's chat view shows: "Carol: Message sent at 10:05 (read)"
```

**Scenario 6.3.3: Delivery Timeout - Recipient Offline**
```gherkin
Given Alice sends message but Bob is temporarily offline
When message waits for delivery for > 5 minutes without acknowledgment
Then deliveryStatus may remain "sent" or transition to "pending"
And when Bob reconnects, message is re-sent and status updated to "delivered"
```

---

### US-6.4: Chat History and Retrieval
**Priority:** P0 (Critical)
**Capability:** Chat messages are persisted and retrievable with pagination and filtering.

**As a** participant
**I want to** scroll back through chat history to review past messages
**So that** I can find important information discussed earlier

**Acceptance Scenarios:**

**Scenario 6.4.1: Retrieve Room-Wide Chat History**
```gherkin
Given a room has chat messages from the past 2 hours
When Alice calls GET /rooms/{roomId}/chat with {limit: 50, offset: 0, messageType: "room_wide"}
Then the server returns paginated chat history:
  - messageType: "room_wide"
  - messages: [
      {messageId, participantId, participantName, content, sentAt, deliveryStatus},
      ...
    ]
  - pagination: {limit: 50, offset: 0, total: 487, hasMore: true}
And messages are ordered by sentAt descending (most recent first)
And all participants can retrieve room-wide chat (authorization: chat_read)
```

**Scenario 6.4.2: Retrieve Direct Message History with Specific Participant**
```gherkin
Given Carol sent multiple DMs to Alice
When Carol calls GET /rooms/{roomId}/chat with {messageType: "direct_message", targetParticipantId: "Alice"}
Then the server returns only DMs between Carol and Alice (bidirectional)
And ordering: sentAt descending
And pagination support
And only Carol and Alice can view this conversation (authorization: sender or recipient)
```

**Scenario 6.4.3: Chat History After Room Ends**
```gherkin
Given a room ended 2 hours ago
And room has configurable retention: chatRetentionHours = 72
When Alice calls GET /rooms/{roomId}/chat
And current time is room.endedAt + 2 hours
Then chat history is still available (within retention window)
When current time is room.endedAt + 73 hours
Then the server returns 410 Gone or 404 Not Found (chat history expired)
And chat records are deleted from database as part of retention policy cleanup
```

**Scenario 6.4.4: Search Chat Messages**
```gherkin
Given a room has 500+ messages
When Alice calls GET /rooms/{roomId}/chat with {search: "scalability"}
Then the server returns messages containing keyword "scalability"
And results are paginated and ordered by relevance or date
(Note: Phase 1 may not include full-text search; implement in Phase 2 if needed)
```

---

### US-6.5: Chat Read Models for Efficient Retrieval
**Priority:** P1 (High)
**Capability:** Chat read models optimize common queries (latest messages, DM lists) without full table scans.

**As a** system operator
**I want to** ensure chat queries are fast even with large message volumes
**So that** chat performance doesn't degrade as rooms accumulate history

**Acceptance Scenarios:**

**Scenario 6.5.1: Latest Room Messages Cache**
```gherkin
Given a room has 10,000+ chat messages
When Alice joins the room and requests latest 50 messages
Then the server fetches from a read model or cache
And response time is < 100ms (P95)
Not fetching from full message table
```

**Scenario 6.5.2: DM Conversation List Read Model**
```gherkin
Given Carol has sent DMs to 20+ participants
When Carol calls GET /rooms/{roomId}/messages/conversations
Then the server returns list of unique participants she's exchanged DMs with
And each conversation shows: participantId, participantName, lastMessageAt, unreadCount, lastMessagePreview
And response time is < 200ms
```

---

### US-6.6: Real-Time Message Broadcasting
**Priority:** P0 (Critical)
**Capability:** New messages are broadcast to all eligible recipients in real-time via SignalR.

**As a** participant
**I want to** receive new chat messages instantly as they are sent
**So that** conversations feel live and responsive

**Acceptance Scenarios:**

**Scenario 6.6.1: Room-Wide Message Broadcast**
```gherkin
Given Alice sends a room-wide message
When message is persisted to database
Then orchestration broadcasts via SignalR room group: {event: "chat.message_sent", messageId, participantId: "Alice", content, sentAt}
And within 100ms, all connected room participants receive the message
And their chat UI updates to display the new message
```

**Scenario 6.6.2: Direct Message Broadcast**
```gherkin
Given Carol sends DM to Alice
When message is persisted
Then orchestration broadcasts via SignalR to only Alice and Carol:
  {event: "chat.message_sent", messageType: "direct_message", senderId: "Carol", recipientId: "Alice", content}
And only Alice and Carol's clients receive the message
And other participants see no indication of the DM
```

**Scenario 6.6.3: Message Delivery Acknowledgment Broadcast**
```gherkin
Given Alice sends message to room
And Bob's client receives the broadcast
When Bob's client sends acknowledgment: {event: "chat.message_delivered", messageId}
Then the server updates ChatMessage.deliveryStatus = "delivered"
And broadcasts updated status back to Alice: {event: "chat.message_status_changed", messageId, deliveryStatus: "delivered"}
And Alice sees message marked as "delivered" in her chat UI
```

---

### US-6.7: Chat Permission Enforcement
**Priority:** P0 (Critical)
**Capability:** Chat access is controlled by room policy and participant role.

**As a** room administrator
**I want to** control who can send and receive chat
**So that** I can maintain room focus and prevent disruption

**Acceptance Scenarios:**

**Scenario 6.7.1: Guest Listeners - No Chat Access**
```gherkin
Given a room allows guest listeners
And guests have policy: can_send_chat = false, can_read_chat = false
When guest Carol joins and subscribes to room chat
Then Carol receives NO chat messages in the broadcast
And if Carol attempts to send chat, receives 403 Forbidden
And Carol's chat UI is disabled or hidden
```

**Scenario 6.7.2: Listeners - Receive Chat Only**
```gherkin
Given a room has policy: listeners.can_send_chat = false, listeners.can_read_chat = true
When listener Bob joins
Then Bob can read all room-wide chat messages
And if Bob attempts to send room-wide chat, receives 403 Forbidden
And Bob cannot send DMs to other participants (only moderators can send DM)
```

**Scenario 6.7.3: Panelists - Full Chat Access**
```gherkin
Given a room has policy: panelists.can_send_chat = true, panelists.can_read_chat = true
When panelist Alice joins
Then Alice can send and receive room-wide chat messages
And Alice can send DMs (if moderator, otherwise restricted to moderators sending DMs to her)
```

**Scenario 6.7.4: Moderator - Unrestricted Chat**
```gherkin
Given Carol is the room moderator
Then Carol can:
  - Send and receive room-wide messages
  - Send DMs to any participant
  - View all chat history
  - (Future) Moderate messages, delete messages, mute participants
```

---

### US-6.8: Chat Export and Archival
**Priority:** P2 (Medium)
**Capability:** Chat messages can be exported as part of room artifacts for record-keeping or compliance.

**As a** room administrator
**I want to** export chat messages after room ends
**So that** I have a record for archival, compliance, or sharing

**Acceptance Scenarios:**

**Scenario 6.8.1: Export Chat to Text File**
```gherkin
Given a room has ended
When moderator calls POST /rooms/{roomId}/export with {artifactType: "chat"}
Then the server aggregates all room-wide chat messages (excluding DMs for privacy)
And generates a text file with format:
  HH:MM - ParticipantName: "Message content"
  10:05 - Alice: "What about scalability?"
  10:06 - Bob: "Great question. Let me explain..."
And uploads to MinIO with path: /artifacts/{roomId}/chat_export.txt
And returns {exportId, downloadUrl, generatedAt}
```

**Scenario 6.8.2: Chat Retention Policy**
```gherkin
Given a tenant has chatRetentionHours = 72
When room ended 4 days ago
And retention period has passed
Then orchestration's retention cleanup job deletes all chat records for that room
And audit log records: {timestamp, action: "chat_deleted_by_retention", roomId, messageCount}
And if room is locked for legal hold (future), retention is suspended
```

---

## Functional Requirements

### F6.1: Room-Wide Chat
- **F6.1.1:** POST /rooms/{roomId}/chat with {messageType: "room_wide", content} creates ChatMessage
- **F6.1.2:** Message creation must validate sender has chat_send permission (role-based from Identity)
- **F6.1.3:** Message content must be text only in Phase 1 (no media, markdown in Phase 2)
- **F6.1.4:** Maximum message length: 5000 characters (configurable per tenant)
- **F6.1.5:** Messages must be persisted to database: Chat.ChatMessage table with fields: id, roomId, senderId, messageType, content, sentAt, deliveryStatus, readAt
- **F6.1.6:** Upon creation, message deliveryStatus = "sent"
- **F6.1.7:** Room-wide messages must be broadcast via SignalR to all participants in room group
- **F6.1.8:** Broadcast payload: {event: "chat.message_sent", messageId, senderId, senderName, content, sentAt, messageType}
- **F6.1.9:** Broadcast must reach all connected participants within 100ms
- **F6.1.10:** Rate limiting: max 10 messages per 10 seconds per participant (configurable)

### F6.2: Direct Messages (Moderator Only)
- **F6.2.1:** POST /rooms/{roomId}/chat with {messageType: "direct_message", targetParticipantId, content} creates private message
- **F6.2.2:** Only moderator can send direct messages (enforced by authorization check: caller must be current moderator)
- **F6.2.3:** Target participant must be in the room (FK validation)
- **F6.2.4:** Messages must be persisted with fields: id, roomId, senderId, recipientId, messageType, content, sentAt, deliveryStatus
- **F6.2.5:** Direct messages must NOT be broadcast to room group; only to sender and recipient
- **F6.2.6:** Broadcast via SignalR using direct message group or individual connections
- **F6.2.7:** Both sender and recipient can see the message in their chat history
- **F6.2.8:** Other participants have no visibility of direct message existence

### F6.3: Delivery Status
- **F6.3.1:** Each ChatMessage has deliveryStatus field: "sent" | "delivered" | "read"
- **F6.3.2:** Initial status upon creation: "sent"
- **F6.3.3:** Client must acknowledge receipt via SignalR: {event: "chat.message_delivered", messageId}
- **F6.3.4:** Server updates ChatMessage.deliveryStatus = "delivered" upon acknowledgment
- **F6.3.5:** Optional: client can send read acknowledgment: {event: "chat.message_read", messageId}
- **F6.3.6:** Server updates ChatMessage.deliveryStatus = "read" upon read acknowledgment
- **F6.3.7:** For room-wide messages, deliveryStatus is per-participant (each participant's delivery state tracked separately, or simplified to single status)
  - Recommend: simplified single status (most recent delivery state) for Phase 1
- **F6.3.8:** Direct messages track individual participant delivery status (sender and recipient separate)

### F6.4: Chat History and Retrieval
- **F6.4.1:** GET /rooms/{roomId}/chat endpoint returns paginated chat messages
- **F6.4.2:** Query parameters: limit (1-100, default 50), offset (0 onwards, default 0), messageType ("room_wide" | "direct_message" | null for all), targetParticipantId (for DM filtering)
- **F6.4.3:** Response includes pagination: {messages: [...], pagination: {limit, offset, total, hasMore}}
- **F6.4.4:** Messages ordered by sentAt descending (most recent first)
- **F6.4.5:** Authorization: caller can only retrieve messages they have permission to read
  - Room-wide: anyone with chat_read permission
  - Direct messages: only sender or recipient
- **F6.4.6:** Chat history available for duration of room + configurable retention (default 72 hours)
- **F6.4.7:** After retention expires, messages are soft-deleted or hard-deleted based on policy
- **F6.4.8:** If room is archived/locked, chat remains accessible indefinitely (future: legal hold support)

### F6.5: Chat Read Models
- **F6.5.1:** Chat.ChatReadModel_RoomLatest stores latest 100 messages per room (updated on each new message)
- **F6.5.2:** Chat.ChatReadModel_DMConversations stores unique DM conversation pairs with last message info
- **F6.5.3:** Read models updated immediately upon message creation (synchronous)
- **F6.5.4:** GET /rooms/{roomId}/chat queries read models first before falling back to full table
- **F6.5.5:** Read models cleared when room ends (after retention period)

### F6.6: Permission Enforcement
- **F6.6.1:** chat_send permission determined by role and room policy (Identity module)
- **F6.6.2:** chat_read permission determined by role and room policy
- **F6.6.3:** Guest listeners (if enabled) must have chat_send = false and chat_read = false by default (policy-driven)
- **F6.6.4:** All endpoints must validate permission before processing request
- **F6.6.5:** Direct message sending restricted to moderators only (hardcoded, not policy-driven)
- **F6.6.6:** Non-permitted actions return 403 Forbidden with descriptive error

### F6.7: Real-Time Broadcasting via SignalR
- **F6.7.1:** Chat messages broadcast to SignalR room group: "room-{roomId}"
- **F6.7.2:** Direct messages broadcast to target participant and sender only (private groups or direct connections)
- **F6.7.3:** Broadcasting must occur within 100ms of message persistence
- **F6.7.4:** Broadcast event name: "chat.message_sent"
- **F6.7.5:** Broadcast payload includes: messageId, senderId, senderName, content, sentAt, messageType (room_wide or direct_message)
- **F6.7.6:** Status updates broadcast as separate events: "chat.message_delivered", "chat.message_read"

### F6.8: Content Validation
- **F6.8.1:** Message content must not be empty or whitespace-only (validate after trim)
- **F6.8.2:** Maximum length: 5000 characters (configurable)
- **F6.8.3:** No special character filtering in Phase 1 (accept all Unicode)
- **F6.8.4:** No content moderation/filtering in Phase 1 (reserved for future)
- **F6.8.5:** Invalid content returns 400 Bad Request with specific error

### F6.9: Rate Limiting
- **F6.9.1:** Per-participant rate limit: 10 messages per 10 seconds (sliding window)
- **F6.9.2:** Rate limit applies to room-wide and direct messages combined
- **F6.9.3:** Exceeded limit returns 429 Too Many Requests
- **F6.9.4:** Rate limit window tracked per participant per room (resets if room ends)

### F6.10: Chat Export
- **F6.10.1:** POST /rooms/{roomId}/export with {artifactType: "chat"} triggers export
- **F6.10.2:** Export includes only room-wide messages (DMs excluded for privacy)
- **F6.10.3:** Export format: plain text, CSV, or JSON (recommend: JSON for data integrity)
- **F6.10.4:** Export uploaded to MinIO: /artifacts/{roomId}/chat_export_{timestamp}.json
- **F6.10.5:** Response includes downloadUrl (pre-signed MinIO URL with 24-hour expiry)
- **F6.10.6:** Export authorization: only moderators or room owner

### F6.11: Retention Policy
- **F6.11.1:** Tenant configuration: chatRetentionHours (default 72 hours)
- **F6.11.2:** Room-specific override: chatRetentionHours (optional, defaults to tenant config)
- **F6.11.3:** Cleanup job runs daily, identifies rooms where (now - endedAt) > retentionHours
- **F6.11.4:** Soft-delete all chat records for that room (set deletedAt timestamp)
- **F6.11.5:** Or hard-delete if GDPR/compliance policy requires immediate deletion
- **F6.11.6:** Audit log: record all deletions for compliance

---

## Key Entities

### ChatMessage
```typescript
{
  id: string;                           // UUID
  roomId: string;                       // FK to Rooms.Room
  senderId: string;                     // FK to Identity.Participant
  messageType: "room_wide" | "direct_message";
  content: string;                      // Max 5000 characters
  sentAt: DateTime;                     // ISO 8601
  recipientId?: string;                 // FK to Identity.Participant (for DM only)
  deliveryStatus: "sent" | "delivered" | "read";
  deliveredAt?: DateTime;               // When status changed to delivered
  readAt?: DateTime;                    // When status changed to read
  deletedAt?: DateTime;                 // Soft-delete timestamp (null if active)
}
```

### ChatDeliveryStatus
```typescript
// Per-participant delivery tracking (for room-wide messages)
{
  messageId: string;                    // FK to ChatMessage
  participantId: string;                // FK to Identity.Participant
  deliveryStatus: "sent" | "delivered" | "read";
  deliveredAt?: DateTime;
  readAt?: DateTime;
}
```

### ChatReadModel_RoomLatest
```typescript
// Denormalized view of latest 100 messages per room
{
  roomId: string;                       // Clustered index
  messages: ChatMessage[];              // Array of latest 100
  lastUpdated: DateTime;
}
```

### ChatReadModel_DMConversations
```typescript
// Denormalized view of DM conversations
{
  roomId: string;
  participant1Id: string;
  participant2Id: string;               // (always sorted for uniqueness)
  lastMessageAt: DateTime;
  unreadCount: number;                  // From perspective of each participant
  lastMessagePreview: string;           // First 100 chars of last message
}
```

---

## Success Criteria

1. **Message Persistence Latency:** Create → persist → response within 50ms (P95)
2. **Broadcast Latency:** Message created → delivered to all clients within 100ms (P95)
3. **Chat History Retrieval:** Paginated query for 50 messages in < 100ms even with 10,000+ messages (P95)
4. **Delivery Status Accuracy:** 100% of acknowledged messages show correct status
5. **Permission Enforcement:** All unauthorized chat attempts rejected; 0% false positives
6. **Rate Limiting:** 100% of requests exceeding limit (10 msg/10s) are rejected with 429
7. **Chat Availability:** Chat operational for duration of room + retention period (no unexpected deletions)
8. **Broadcast Reliability:** 100% of eligible participants receive messages (SignalR guarantees)
9. **Export Performance:** Chat export for room with 1000+ messages completes in < 5 seconds
10. **Scalability:** Support 1000+ participants per room, 10,000+ messages per room, 100+ concurrent rooms

---

## Edge Cases

1. **Message Sent During Room End:** Participant sends message as room is ending; message is persisted but may not broadcast to all (eventual consistency acceptable)
2. **Recipient Offline During DM:** Moderator sends DM while recipient is briefly disconnected; message queued and delivered on reconnect
3. **Rate Limit at Boundary:** Participant sends exactly 10 messages in 10-second window; 11th message in same window rejected (correct)
4. **Very Long Message:** Message of exactly 5000 characters accepted; 5001 rejected
5. **Special Characters/Unicode:** Emoji, RTL text, zero-width characters accepted (no filtering in Phase 1)
6. **Chat Query During Retention Cleanup:** GET /rooms/{roomId}/chat called while cleanup job deletes records; returns available messages (soft-delete ensures consistency)
7. **DM to Removed Participant:** Moderator sends DM to participant who is removed mid-message; message persists but may not deliver
8. **Simultaneous Message and Room End:** Multiple messages sent as room ends; all persisted, broadcast attempt is best-effort
9. **SignalR Reconnection:** Participant loses connection, messages sent while offline, reconnects; client requests history to fill gaps
10. **Double-Delivered Message:** SignalR retry causes duplicate broadcast to client; client implements deduplication via messageId

---

## Assumptions

1. **Identity Module Available:** Participant IDs and roles fetched from Identity; no caching of role changes (real-time validation)
2. **Room State Available:** Room state (locked, active, ended) available from Rooms module; Chat respects room lifecycle
3. **SignalR Operational:** SignalR configured with Redis backplane; delivery guaranteed within SLA
4. **SQL Server Available:** Chat.ChatMessage table persisted reliably; if DB down, requests fail with 503
5. **Moderator Authority:** Moderator role verified by auth middleware; Chat trusts JWT claim
6. **Permission Engine Available:** Identity module provides role → permission mapping (chat_send, chat_read)
7. **MinIO for Exports:** MinIO available and configured; exports uploaded and accessible
8. **Clock Synchronization:** Server clocks synchronized; timestamps consistent across services
9. **Text-Only Content Phase 1:** Media/attachments not supported in Phase 1 (reserved for future)
10. **No Content Moderation Phase 1:** No spam/toxicity filtering in Phase 1; moderators review manually (reserved for future)

---

## Open Questions & Decisions

1. **Delivery Status Per-Participant:** For room-wide messages, track delivery per participant or single aggregate? (Recommend: aggregate to "most delivered" for simplicity)
2. **DM Privacy:** Should deleted/removed participants' DMs remain visible to the other party? (Recommend: Yes, for record-keeping; mark sender as "removed")
3. **Read Receipts Opt-Out:** Should participants be able to disable read receipts? (Recommend: Phase 2 feature)
4. **Message Editing:** Can participants edit sent messages? (Recommend: Phase 2 feature; Phase 1 immutable)
5. **Message Deletion:** Can participants/moderators delete messages? (Recommend: moderator delete in Phase 2; Phase 1 immutable)
6. **Typing Indicators:** Show when someone is typing? (Recommend: Phase 2 feature)
7. **Chat Notifications:** Sound/visual notifications for new messages? (Recommend: client-side; server provides event)
8. **Full-Text Search:** Search chat by keyword? (Recommend: Phase 2 with indexed search)
9. **Chat Threading:** Support threaded replies to specific messages? (Recommend: Phase 2; Phase 1 flat)
10. **Forward Messages:** Allow participants to forward messages to other rooms? (Recommend: Phase 2; Phase 1 no forwarding)

