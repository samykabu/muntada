# Epic 6: Chat & Messaging Module - Task Breakdown

**Version:** 1.0
**Epic Owner:** Communication & Messaging
**Last Updated:** 2026-04-03

---

## Execution Overview

This epic manages text communication within rooms. Depends on Rooms (Epic 3) and Realtime Orchestration (Epic 4). Tasks organize into 4 phases: infrastructure, message persistence, broadcasting, and integration.

---

## Phase 1: Module Setup & Entities

### T601: Chat Module Structure [P]
**User Story:** US-6.1
**Priority:** P1
**Effort:** 5 pts
**Dependencies:** Epic 0, Epic 3, Epic 4

Create Chat module structure and database schema.

**File Locations:**
- `backend/src/Modules/Chat/Chat.csproj`
- `backend/src/Modules/Chat/Infrastructure/ChatDbContext.cs`
- SQL schema: `[chat]`

---

### T602: Chat Message Entity [P]
**User Story:** US-6.1
**Priority:** P1
**Effort:** 3 pts
**Dependencies:** T601

Implement ChatMessage entity.

**Deliverables:**
- `ChatMessage` (RoomId, SenderId, MessageType, Content, SentAt, RecipientId, DeliveryStatus, DeliveredAt, ReadAt, DeletedAt)
- `ChatMessageType` enum (room_wide, direct_message)
- `DeliveryStatus` enum (sent, delivered, read)
- Max length: 5000 characters

**File Locations:**
- `backend/src/Modules/Chat/Domain/Message/ChatMessage.cs`

---

### T603: Chat Delivery Status Entity [P]
**User Story:** US-6.3
**Priority:** P2
**Effort:** 2 pts
**Dependencies:** T602

Implement per-participant delivery tracking.

**Deliverables:**
- `ChatDeliveryStatus` (MessageId, ParticipantId, DeliveryStatus, DeliveredAt, ReadAt)
- For granular tracking of room-wide message delivery

**File Locations:**
- `backend/src/Modules/Chat/Domain/Message/ChatDeliveryStatus.cs`

---

### T604: Chat Read Models [P]
**User Story:** US-6.5
**Priority:** P2
**Effort:** 5 pts
**Dependencies:** T602

Implement denormalized read models.

**Deliverables:**
- `ChatReadModel_RoomLatest` (RoomId, Messages, LastUpdated)
- `ChatReadModel_DMConversations` (RoomId, Participant1Id, Participant2Id, LastMessageAt, UnreadCount, LastMessagePreview)

**File Locations:**
- `backend/src/Modules/Chat/Infrastructure/ReadModels/ChatReadModelRoomLatest.cs`
- `backend/src/Modules/Chat/Infrastructure/ReadModels/ChatReadModelDMConversations.cs`

---

## Phase 2: Message Operations

### T605: Send Room-Wide Chat Command [P]
**User Story:** US-6.1
**Priority:** P1
**Effort:** 8 pts
**Dependencies:** T602

Implement room-wide message sending.

**Deliverables:**
- `SendRoomChatCommand` & handler
- Authorization check (chat_send permission)
- Content validation (non-empty, max 5000 chars)
- Rate limiting: 10 messages per 10 seconds per participant
- Persist to database
- Publish `ChatMessageSent` event
- Broadcast via SignalR

**File Locations:**
- `backend/src/Modules/Chat/Application/Commands/SendRoomChatCommand.cs`

**Acceptance:**
- Message created and persisted
- Authorization enforced
- Rate limit checked
- Event published
- Broadcast within 100ms

---

### T606: Send Direct Message Command [P]
**User Story:** US-6.2
**Priority:** P1
**Effort:** 8 pts
**Dependencies:** T602

Implement moderator DM sending.

**Deliverables:**
- `SendDirectMessageCommand` & handler
- Moderator authorization (only moderators can send DM)
- Recipient validation (in room)
- Content validation
- Rate limiting (same as room-wide)
- Persist to database
- Broadcast to sender and recipient only
- Publish `DirectMessageSent` event

**File Locations:**
- `backend/src/Modules/Chat/Application/Commands/SendDirectMessageCommand.cs`

**Acceptance:**
- DM created and persisted
- Broadcast only to sender and recipient
- Moderator authorization enforced

---

### T607: Message Delivery Status Tracking [P]
**User Story:** US-6.3
**Priority:** P1
**Effort:** 5 pts
**Dependencies:** T605

Implement delivery status updates.

**Deliverables:**
- `UpdateMessageDeliveryStatusCommand` & handler
- Client sends `chat.message_delivered` on receipt
- Client sends `chat.message_read` on read (optional)
- Update ChatMessage.deliveryStatus
- Broadcast status change to sender

**File Locations:**
- `backend/src/Modules/Chat/Application/Commands/UpdateMessageDeliveryStatusCommand.cs`

**Acceptance:**
- Status transitions correct
- Broadcast to sender
- Timestamps recorded

---

### T608: Get Chat History Query [P]
**User Story:** US-6.4
**Priority:** P1
**Effort:** 8 pts
**Dependencies:** T602, T604

Implement paginated chat retrieval.

**Deliverables:**
- `GetChatHistoryQuery` & handler
- Pagination: limit (1-100), offset, total, hasMore
- Filters: messageType (room_wide or direct_message), targetParticipantId (for DM)
- Ordering: sentAt descending (newest first)
- Authorization: caller can only access permitted messages
- Retention check: return 410 Gone if outside retention window

**File Locations:**
- `backend/src/Modules/Chat/Application/Queries/GetChatHistoryQuery.cs`

**Acceptance:**
- Pagination works correctly
- Authorization enforced
- Retention checked
- Response in < 100ms

---

### T609: Chat Permission Enforcement [P]
**User Story:** US-6.7
**Priority:** P1
**Effort:** 5 pts
**Dependencies:** T605, T606

Implement role-based chat permissions.

**Deliverables:**
- Permission checks: chat_send, chat_read
- Guest listeners: no send, optional no read (policy-driven)
- Panelists: full access
- Moderators: full access + DM capability
- Return 403 if permission denied

**File Locations:**
- `backend/src/Modules/Chat/Application/Services/ChatPermissionService.cs`

**Acceptance:**
- Permissions enforced
- Role-based control working
- Guests restricted correctly

---

## Phase 3: Real-Time Broadcasting

### T610: Chat SignalR Hub [P]
**User Story:** US-6.6
**Priority:** P1
**Effort:** 8 pts
**Dependencies:** T605, T606

Implement real-time message broadcasting.

**Deliverables:**
- SignalR group: `room-{roomId}` for room-wide messages
- Direct message delivery via direct connection or group
- Event: `chat.message_sent` with payload {messageId, senderId, senderName, content, sentAt, messageType}
- Event: `chat.message_delivered` for status update
- Event: `chat.message_read` for status update
- Broadcast latency target: < 100ms

**File Locations:**
- `backend/src/Modules/Chat/Api/Hubs/ChatHub.cs`

**Acceptance:**
- Messages broadcast to correct group
- Direct messages private
- Status updates broadcast
- Latency < 100ms

---

### T611: Read Model Updates [P]
**User Story:** US-6.5
**Priority:** P2
**Effort:** 5 pts
**Dependencies:** T604, T605

Implement read model synchronization.

**Deliverables:**
- Update `ChatReadModel_RoomLatest` on each new message
- Keep latest 100 messages per room
- Update `ChatReadModel_DMConversations` with last message info
- Maintain unread counts per participant

**File Locations:**
- `backend/src/Modules/Chat/Application/Services/ChatReadModelService.cs`

**Acceptance:**
- Read models updated immediately
- Queries use read models (< 100ms)
- Models accurate

---

## Phase 4: Advanced Features & Retention

### T612: Chat Export [P]
**User Story:** US-6.8
**Priority:** P2
**Effort:** 8 pts
**Dependencies:** T605

Implement chat export.

**Deliverables:**
- `ExportChatCommand` & handler
- Query all room-wide messages (exclude DMs for privacy)
- Format: JSON, CSV, or plain text
- Upload to MinIO: `/artifacts/{roomId}/chat_export_{timestamp}.json`
- Return pre-signed URL (24-hour expiry)
- Authorization: moderators or room owner only

**File Locations:**
- `backend/src/Modules/Chat/Application/Commands/ExportChatCommand.cs`
- `backend/src/Modules/Chat/Application/Services/ExportService.cs`

**Acceptance:**
- Export completes in < 5 seconds
- File uploaded to MinIO
- URL accessible for download
- Privacy maintained (DMs excluded)

---

### T613: Chat Retention Policy [P]
**User Story:** US-6.8
**Priority:** P2
**Effort:** 8 pts
**Dependencies:** T602

Implement retention cleanup.

**Deliverables:**
- `ChatRetentionCleanupJob` - runs daily
- Soft-delete messages older than retention period
- Hard-delete after retention + grace period
- Audit logging of deletions
- Retention configurable per tenant (default 72 hours)
- Audit logs minimum 7 years

**File Locations:**
- `backend/src/Modules/Chat/Application/BackgroundJobs/ChatRetentionCleanupJob.cs`

**Acceptance:**
- Job runs daily
- Retention enforced
- Grace period respected
- Audit trail preserved

---

## Phase 5: API Endpoints

### T614: Chat API - Send Messages [P]
**User Story:** US-6.1, US-6.2
**Priority:** P1
**Effort:** 8 pts
**Dependencies:** T605, T606

Create REST endpoints for sending messages.

**Deliverables:**
- `POST /api/rooms/{roomId}/chat` - send message
- Request body: {messageType: "room_wide", content}
- Request body: {messageType: "direct_message", targetParticipantId, content}
- Response: {messageId, sentAt, deliveryStatus: "sent"}
- Status: 201 Created, 400 Bad Request, 403 Forbidden, 429 Too Many Requests

**File Locations:**
- `backend/src/Modules/Chat/Api/Controllers/ChatController.cs`

**Acceptance:**
- Endpoints functional
- Status codes correct
- Validation working

---

### T615: Chat API - Retrieve Messages [P]
**User Story:** US-6.4
**Priority:** P1
**Effort:** 8 pts
**Dependencies:** T608

Create REST endpoints for message retrieval.

**Deliverables:**
- `GET /api/rooms/{roomId}/chat` - list messages
- Query params: limit, offset, messageType, targetParticipantId
- Response: {messages: [...], pagination: {limit, offset, total, hasMore}}
- Status: 200 OK, 410 Gone (if outside retention), 403 Forbidden

**File Locations:**
- `backend/src/Modules/Chat/Api/Controllers/ChatController.cs`

**Acceptance:**
- Pagination works
- Authorization enforced
- Retention checked
- Response in < 100ms

---

### T616: Chat API - Delivery Status & Export [P]
**User Story:** US-6.3, US-6.8
**Priority:** P2
**Effort:** 8 pts
**Dependencies:** T607, T612

Create endpoints for status updates and export.

**Deliverables:**
- `POST /api/rooms/{roomId}/chat/messages/{messageId}/delivered` - acknowledge delivery
- `POST /api/rooms/{roomId}/chat/messages/{messageId}/read` - acknowledge read
- `POST /api/rooms/{roomId}/chat/export` - export messages
- Response: {exportId, downloadUrl, generatedAt}

**File Locations:**
- `backend/src/Modules/Chat/Api/Controllers/ChatController.cs`

**Acceptance:**
- Status endpoints functional
- Export works

---

### T617: Frontend: Chat Component [P]
**User Story:** US-6.1, US-6.4
**Priority:** P2
**Effort:** 15 pts
**Dependencies:** T614, T615

Create React chat UI.

**Deliverables:**
- `frontend/src/features/chat/components/ChatPanel.tsx`
- Message list (scrollable, auto-scroll to latest)
- Message input form
- Real-time updates via SignalR
- Delivery status indicators
- Participant name and avatar
- Timestamp formatting

**File Locations:**
- `frontend/src/features/chat/components/ChatPanel.tsx`
- `frontend/src/features/chat/components/MessageList.tsx`
- `frontend/src/features/chat/components/MessageInput.tsx`

**Acceptance:**
- Chat renders correctly
- Real-time updates work
- Scrolling smooth
- Input validation

---

### T618: Frontend: Direct Message Support [P]
**User Story:** US-6.2
**Priority:** P2
**Effort:** 10 pts
**Dependencies:** T617

Create DM UI for moderators.

**Deliverables:**
- `frontend/src/features/chat/components/DirectMessagePanel.tsx`
- Conversation list (DM thread list)
- Private message thread view
- Real-time updates
- Moderator-only visibility

**File Locations:**
- `frontend/src/features/chat/components/DirectMessagePanel.tsx`

**Acceptance:**
- DM panel renders
- Conversations listed
- Private messages not visible to others
- Real-time updates work

---

## Phase 6: Integration Tests

### T619: Chat Integration Tests [P]
**User Story:** All
**Priority:** P1
**Effort:** 21 pts
**Dependencies:** All tasks

Write comprehensive tests.

**Deliverables:**
- Tests for room-wide chat (send, retrieve, authorization)
- Tests for direct messages (send, privacy, authorization)
- Tests for delivery status
- Tests for message validation (content length, empty)
- Tests for rate limiting
- Tests for authorization (guest listeners, panelists, moderators)
- Tests for retention cleanup
- Tests for read models
- Tests for chat export

**File Locations:**
- `backend/src/Modules/Chat.Tests/Integration/`

**Acceptance:**
- All scenarios tested
- Coverage > 80%
- Edge cases handled
- Concurrency tests included

---

## Success Metrics

- Message persistence latency < 50ms p95
- Broadcast latency < 100ms p95
- Chat retrieval (50 messages) < 100ms p95
- Delivery status accuracy 100%
- Authorization: 100% of unauthorized attempts rejected
- Rate limiting: 100% of requests exceeding limit rejected
- Chat available for room duration + retention period
- Export completes in < 5 seconds
- Scalability: 1000+ participants, 10,000+ messages per room
- Retention cleanup: automatic, on schedule, no data loss
