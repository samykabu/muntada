# Epic 12: Public API & Webhooks Module

## Overview
The Public API & Webhooks Module provides external integrations with Muntada via a RESTful API secured with Personal Access Tokens (PATs). Tenants can programmatically create rooms, retrieve usage metrics, and register webhooks to receive real-time events. All API responses are paginated, support filtering, and include proper error handling. Webhooks are signed with HMAC-SHA256, delivered with automatic retries, and include dead-letter queue handling for failed deliveries.

## User Stories

### US-12.1: Public API Authentication with Personal Access Tokens
**Priority:** P0 (Critical)
**Story Points:** 5

As a tenant, I want to create Personal Access Tokens so that external applications can call the Muntada API on my behalf with scoped permissions.

**Acceptance Scenarios:**

**Scenario 1: Create Personal Access Token**
```gherkin
Given I am a tenant admin
When I navigate to Settings → API Tokens
  And click "Generate New Token"
Then a modal appears asking for:
  - Token name (e.g., "Integration with CRM")
  - Permissions (checkboxes: read_rooms, create_rooms, read_usage, read_webhooks, write_webhooks, etc.)
  - Expiration date (90 days, 6 months, 1 year, or custom)
  And I click "Generate"
Then:
  - A PAT record is created with:
    - token_id (UUID)
    - token_hash (SHA256 hash of token, not plaintext stored)
    - tenant_id
    - permissions (JSON array)
    - expiration_date
    - created_by_user_id
    - created_at
    - is_active = true
  - The token value is displayed ONCE (plaintext) with warning "Save this token now, it won't be shown again"
  - Token is in format: `muntada_pat_<random_32_chars>`
  - An audit event "pat_created" is recorded
```

**Scenario 2: Token Used in API Request**
```gherkin
Given I have a PAT token
When I make a request to GET /public/v1/rooms
  And include header "Authorization: Bearer muntada_pat_abc123..."
Then:
  - The server validates the token:
    - Hash the header token
    - Look up token_hash in database
    - Verify token is not expired
    - Verify token is_active = true
  - If valid: request proceeds with tenant_id from token
  - If invalid: response 401 Unauthorized with message "Invalid or expired token"
```

**Scenario 3: Token Revocation**
```gherkin
Given I have created a PAT token
When I navigate to Settings → API Tokens
  And click "Revoke" on the token
Then:
  - A confirmation modal appears
  - I confirm revocation
  - The token's is_active flag is set to false
  - The token can no longer be used
  - All existing API calls with this token are immediately rejected
  - An audit event "pat_revoked" is recorded
```

**Scenario 4: Token Listing and Metadata**
```gherkin
Given I am viewing Settings → API Tokens
Then I see a list of all my tokens:
  - Token name
  - Permissions summary (e.g., "Read-only: rooms, usage")
  - Created date
  - Expiration date
  - Last used date (if any)
  - Actions: View details, Revoke
And I can filter by permissions or status (active, expired, revoked)
```

### US-12.2: API Rate Limiting and Quotas
**Priority:** P0 (Critical)
**Story Points:** 5

As an API consumer, I want to be aware of rate limits and quotas so that I can design my integration to stay within limits and handle backoff.

**Acceptance Scenarios:**

**Scenario 1: API Rate Limiting Per PAT**
```gherkin
Given I have a PAT with standard quota (1000 requests per hour)
When I make API requests
  And I've made 900 requests this hour
Then:
  - Response includes header "X-RateLimit-Limit: 1000"
  - Response includes header "X-RateLimit-Remaining: 100"
  - Response includes header "X-RateLimit-Reset: 1234567890" (UNIX timestamp)
And when I make the 1001st request in the same hour
  Then the response is 429 Too Many Requests
  And response body includes: {"error": "Rate limit exceeded", "retry_after_seconds": 120}
```

**Scenario 2: Rate Limit Headers on All Responses**
```gherkin
Given I make any API request with valid PAT
When the request succeeds (2xx response)
Then response headers include:
  - X-RateLimit-Limit (total requests/hour)
  - X-RateLimit-Remaining (requests remaining this hour)
  - X-RateLimit-Reset (UNIX timestamp of reset)
  - X-RateLimit-Quota-Remaining (monthly quota, if applicable)
```

**Scenario 3: Quota Reset on New Hour**
```gherkin
Given I've made 1000 requests (at limit)
  And the hour boundary passes (e.g., 14:00 UTC → 15:00 UTC)
When I make a request at 15:00 UTC
Then:
  - X-RateLimit-Remaining resets to 1000
  - The request is processed normally
  - Rate limit counter is per-hour sliding window
```

**Scenario 4: Premium Quota Tiers**
```gherkin
Given I am on a "Growth" subscription tier
Then my API quotas are:
  - Standard endpoints: 5000 requests/hour
  - Webhook delivery quota: 100 webhooks/hour
When I upgrade to "Enterprise" tier
Then quotas increase to:
  - Standard endpoints: 50000 requests/hour
  - Webhook delivery quota: 1000 webhooks/hour
```

### US-12.3: Public API Endpoints - Rooms
**Priority:** P0 (Critical)
**Story Points:** 8

As an external system, I want to programmatically create, list, and manage rooms via the Public API.

**Acceptance Scenarios:**

**Scenario 1: GET /public/v1/rooms - List Rooms**
```gherkin
Given I have a valid PAT with "read_rooms" permission
When I call GET /public/v1/rooms
  And include optional query params:
    - limit=10 (default, max 100)
    - offset=0 (pagination offset)
    - status=active (filter by status)
    - visibility=private (filter by visibility)
Then the response is:
{
  "data": [
    {
      "room_id": "uuid-1",
      "room_name": "Product Review Meeting",
      "status": "active",
      "visibility": "private",
      "participant_count": 5,
      "created_at": "2026-04-03T10:00:00Z",
      "recording_enabled": true,
      "recording_active": false,
      "max_participants": null,
      "expiry_hours": null
    }
  ],
  "pagination": {
    "offset": 0,
    "limit": 10,
    "total": 42,
    "has_more": true
  }
}
```

**Scenario 2: POST /public/v1/rooms - Create Room**
```gherkin
Given I have a valid PAT with "create_rooms" permission
When I call POST /public/v1/rooms with body:
{
  "room_name": "Q2 Planning",
  "visibility": "private",
  "room_config": {
    "recording_enabled": true,
    "max_participants": 50,
    "enable_chat": true,
    "participant_timeout_minutes": 60
  },
  "metadata": {
    "department": "Engineering",
    "project_id": "proj-123"
  }
}
Then the response is 201 Created:
{
  "room_id": "uuid-123",
  "room_name": "Q2 Planning",
  "status": "created",
  "visibility": "private",
  "join_token": "eyJhbGc...", (JWT token for joining)
  "livek_room_name": "muntada_uuid-123",
  "created_at": "2026-04-03T10:00:00Z"
}
And an audit event "room.created_via_api" is recorded with PAT token ID
```

**Scenario 3: GET /public/v1/rooms/{room_id} - Get Room Details**
```gherkin
Given I have a room ID
When I call GET /public/v1/rooms/{room_id}
Then the response includes:
  - room_id, room_name, status, visibility
  - participant list (user IDs, join times)
  - recording metadata (if active: start time, duration so far)
  - created_at, expected_end_time (if configured)
```

**Scenario 4: DELETE /public/v1/rooms/{room_id} - Terminate Room**
```gherkin
Given I have a valid PAT with "terminate_rooms" permission
When I call DELETE /public/v1/rooms/{room_id}
  And include body: {"reason": "Scheduled maintenance"}
Then:
  - The room status transitions to "Terminated"
  - All participants are disconnected
  - Response is 200 OK with room details
  - An audit event "room.terminated_via_api" is recorded
```

### US-12.4: Public API Endpoints - Usage
**Priority:** P1 (High)
**Story Points:** 3

As a tenant, I want to query my usage metrics via API for billing and capacity planning.

**Acceptance Scenarios:**

**Scenario 1: GET /public/v1/usage - Current Month Usage**
```gherkin
Given I have a valid PAT with "read_usage" permission
When I call GET /public/v1/usage
  And include optional query: month=2026-04 (defaults to current month)
Then the response is:
{
  "billing_month": "2026-04",
  "plan": "Growth",
  "subscription_status": "active",
  "usage": {
    "concurrent_rooms_peak": 5,
    "concurrent_rooms_avg": 2.3,
    "concurrent_rooms_limit": 10,
    "otp_sends_used": 450,
    "otp_sends_limit": 1000,
    "storage_used_gb": 45.2,
    "storage_limit_gb": 100,
    "recording_minutes_used": 2340,
    "recording_minutes_limit": 5000,
    "ai_requests_used": 250,
    "ai_requests_limit": 1000
  },
  "usage_percentage": {
    "concurrent_rooms": 50,
    "otp_sends": 45,
    "storage": 45.2,
    "recording_minutes": 46.8,
    "ai_requests": 25
  }
}
```

**Scenario 2: GET /public/v1/usage/history - Usage History**
```gherkin
Given I have read_usage permission
When I call GET /public/v1/usage/history
  And include query: months_back=12 (optional, defaults to 6)
Then the response is an array of monthly usage snapshots:
[
  { "billing_month": "2026-03", "usage": {...} },
  { "billing_month": "2026-02", "usage": {...} },
  ...
]
```

### US-12.5: Webhook Endpoint Registration
**Priority:** P0 (Critical)
**Story Points:** 5

As a tenant, I want to register webhook endpoints so that my external system is notified of Muntada events in real-time.

**Acceptance Scenarios:**

**Scenario 1: Register Webhook Endpoint**
```gherkin
Given I am a tenant admin
When I call POST /public/v1/webhook-endpoints with body:
{
  "url": "https://my-crm.example.com/webhooks/muntada",
  "event_types": ["room.started", "room.ended", "participant.joined", "recording.completed"],
  "active": true,
  "description": "Sync room events to CRM"
}
Then the response is 201 Created:
{
  "webhook_endpoint_id": "uuid-456",
  "url": "https://my-crm.example.com/webhooks/muntada",
  "event_types": ["room.started", "room.ended", "participant.joined", "recording.completed"],
  "active": true,
  "signing_secret": "whsec_abc123..." (HMAC secret for signature validation),
  "created_at": "2026-04-03T10:00:00Z",
  "last_successful_delivery": null,
  "failure_count": 0,
  "next_retry": null
}
And an audit event "webhook_endpoint_created" is recorded
```

**Scenario 2: List Webhook Endpoints**
```gherkin
Given I have registered webhooks
When I call GET /public/v1/webhook-endpoints
Then the response lists all my webhook endpoints with:
  - webhook_endpoint_id, url, event_types, active status
  - last_successful_delivery timestamp (if any)
  - failure_count, next_retry timestamp (if pending)
```

**Scenario 3: Update Webhook Endpoint**
```gherkin
Given I have a webhook endpoint
When I call PATCH /public/v1/webhook-endpoints/{webhook_endpoint_id} with body:
{
  "active": false,
  "event_types": ["room.started", "room.ended"]
}
Then the endpoint is updated
  And an audit event "webhook_endpoint_updated" is recorded
  And future deliveries use updated configuration
```

**Scenario 4: Delete Webhook Endpoint**
```gherkin
Given I have a webhook endpoint
When I call DELETE /public/v1/webhook-endpoints/{webhook_endpoint_id}
Then:
  - The endpoint is deleted
  - No more events are delivered to this URL
  - An audit event "webhook_endpoint_deleted" is recorded
```

### US-12.6: Signed Webhook Delivery
**Priority:** P0 (Critical)
**Story Points:** 8

As a webhook consumer, I want to verify that webhooks are authentic and have not been tampered with.

**Acceptance Scenarios:**

**Scenario 1: Webhook Signed with HMAC-SHA256**
```gherkin
Given a webhook event occurs (e.g., room.started)
When the webhook is delivered to my endpoint
Then the HTTP POST request includes headers:
  - X-Muntada-Event-ID: "evt_uuid_123"
  - X-Muntada-Event-Timestamp: "1712145600" (UNIX timestamp)
  - X-Muntada-Signature: "v1,signature=<HMAC-SHA256>" (see format below)
  - Content-Type: application/json

And the request body is a JSON object:
{
  "id": "evt_uuid_123",
  "timestamp": "2026-04-03T10:00:00Z",
  "event_type": "room.started",
  "data": {
    "room_id": "uuid-room",
    "room_name": "Q2 Planning",
    "participant_count": 1,
    "started_at": "2026-04-03T10:00:00Z"
  }
}

Signature format: v1,signature={HMAC_SIGNATURE}
Where HMAC_SIGNATURE = HMAC-SHA256(
  signing_secret,
  evt_uuid_123 + 1712145600 + request_body_json
)
```

**Scenario 2: Webhook Signature Verification**
```gherkin
Given I receive a webhook
When I want to verify authenticity
Then I:
  1. Extract X-Muntada-Event-ID header (evt_id)
  2. Extract X-Muntada-Event-Timestamp header (timestamp)
  3. Read request body as JSON string (body)
  4. Retrieve my signing_secret from webhook endpoint config
  5. Compute: hmac_sha256(signing_secret, evt_id + timestamp + body)
  6. Extract X-Muntada-Signature header
  7. Compare computed HMAC with signature in header
  8. If match: webhook is authentic
  9. If mismatch: reject webhook as tampering attempt
And I can also verify timestamp is recent (within 5 minutes) to prevent replay attacks
```

**Scenario 3: Webhook Delivery Retry**
```gherkin
Given my webhook endpoint is registered
When an event occurs and my endpoint responds with 500 Internal Server Error
Then:
  - WebhookDelivery status = "Failed"
  - Retry is scheduled with exponential backoff:
    - 1st retry: after 30 seconds
    - 2nd retry: after 5 minutes
    - 3rd retry: after 1 hour
    - 4th retry: after 24 hours
  - After 4th failure, webhook is moved to dead-letter queue
  - An alert is sent to tenant admin: "Webhook delivery failing: <url>"
```

**Scenario 4: Webhook Delivery Success**
```gherkin
Given a webhook is being delivered
When my endpoint responds with 2xx status (200, 201, 204)
Then:
  - WebhookDelivery status = "Delivered"
  - Response body (first 1000 chars) is stored for audit
  - last_successful_delivery on WebhookEndpoint is updated
  - failure_count is reset to 0
```

**Scenario 5: Webhook Delivery Dead Letter**
```gherkin
Given a webhook has failed 4 times
  And all retries are exhausted
When the final retry attempt fails
Then:
  - WebhookDelivery status = "DeadLettered"
  - The webhook is moved to dead-letter queue (RabbitMQ)
  - Tenant admin receives email: "Webhook dead-lettered: <url>"
  - WebhookEndpoint.failure_count increments
  - Tenant can manually retry via API or UI
```

### US-12.7: Webhook Event Types
**Priority:** P1 (High)
**Story Points:** 5

As a tenant, I want to receive notifications for important events so that my external system stays synchronized.

**Acceptance Scenarios:**

**Scenario 1: Available Event Types**
```gherkin
Given I am registering a webhook
When I select event types
Then the available events include:
  - room.started (room status → Active)
  - room.ended (room status → Inactive, last participant left)
  - room.terminated (room status → Terminated by admin or system)
  - participant.joined (participant added to room)
  - participant.left (participant removed or disconnected)
  - participant.removed (removed by moderator or admin)
  - recording.started (room recording begins)
  - recording.stopped (room recording ends)
  - recording.completed (recording file ready for download)
  - file.uploaded (file added to room)
  - file.scanned (file scan completed: Ready or Rejected)
  - file.ready (file scan passed, available for download)
  - file.downloaded (participant downloaded a file)
  - chat.message_sent (chat message posted)
  - payment.received (payment transaction completed)
  - payment.failed (payment transaction failed)
  - subscription.status_changed (subscription status update)
```

**Scenario 2: Event Payload Examples**
```gherkin
room.started event:
{
  "id": "evt_123",
  "timestamp": "2026-04-03T10:00:00Z",
  "event_type": "room.started",
  "data": {
    "room_id": "uuid-room",
    "room_name": "Q2 Planning",
    "visibility": "private",
    "creator_user_id": "uuid-user",
    "started_at": "2026-04-03T10:00:00Z"
  }
}

participant.joined event:
{
  "id": "evt_456",
  "timestamp": "2026-04-03T10:05:00Z",
  "event_type": "participant.joined",
  "data": {
    "room_id": "uuid-room",
    "participant_user_id": "uuid-participant",
    "participant_email": "participant@example.com",
    "joined_at": "2026-04-03T10:05:00Z",
    "role": "participant",
    "is_guest": false
  }
}

file.uploaded event:
{
  "id": "evt_789",
  "timestamp": "2026-04-03T10:10:00Z",
  "event_type": "file.uploaded",
  "data": {
    "file_id": "uuid-file",
    "room_id": "uuid-room",
    "file_name": "Q2_Results.pdf",
    "file_size": 2048000,
    "mime_type": "application/pdf",
    "uploader_user_id": "uuid-user",
    "uploaded_at": "2026-04-03T10:10:00Z"
  }
}
```

### US-12.8: Idempotency Keys on Mutating API Endpoints
**Priority:** P1 (High)
**Story Points:** 3

As an API consumer, I want to safely retry POST/PATCH/DELETE requests without creating duplicate resources or side effects.

**Acceptance Scenarios:**

**Scenario 1: POST with Idempotency Key**
```gherkin
Given I want to create a room
When I call POST /public/v1/rooms with headers:
  - Idempotency-Key: "my-request-id-abc123"
  And body: { "room_name": "Q2 Planning", ... }
Then:
  - A room is created with room_id = "uuid-123"
  - Response includes header "Idempotency-Key: my-request-id-abc123"
  - An IdempotencyRequest record is created: key="my-request-id-abc123", result=room_data, status="Completed"
```

**Scenario 2: Retry with Same Idempotency Key**
```gherkin
Given I previously created a room with Idempotency-Key: "my-request-id-abc123"
When I retry the same request (same Idempotency-Key)
Then:
  - The server looks up IdempotencyRequest by key
  - Finds previous result (room_id = "uuid-123")
  - Returns 201 Created with the same room_id
  - No duplicate room is created
  - The cached result is returned
```

**Scenario 3: Idempotency Key Expiration**
```gherkin
Given I made a request with Idempotency-Key 7 days ago
When I retry with the same key today
Then:
  - The IdempotencyRequest record has expired (24-hour TTL)
  - The request is processed as new
  - A new room is created
  - Developer should use a new Idempotency-Key for each unique request
```

## Functional Requirements

### API Authentication & Authorization
1. **PAT Validation on Every Request**: Every request to /public/v1/* validates the Authorization header. Token is hashed and looked up in database. Expired or inactive tokens are rejected with 401.
2. **Permission Enforcement**: Each endpoint checks if the PAT has required permissions. Missing permissions return 403 Forbidden with message "This action requires <permission> permission".
3. **Tenant Isolation**: All API responses are scoped to the tenant of the PAT. No cross-tenant data exposure.

### Rate Limiting
4. **Per-PAT Rate Limiting**: Each PAT has a quota (requests per hour, based on subscription tier). Quota is tracked in Redis with sliding window. Exceeding quota returns 429 with Retry-After header.
5. **Rate Limit Headers on All Responses**: Every response includes X-RateLimit-Limit, X-RateLimit-Remaining, X-RateLimit-Reset headers for transparency.
6. **Quota Tiers by Subscription**: Standard tier: 1000 req/hr. Growth: 5000 req/hr. Enterprise: 50000 req/hr. Quotas are configurable per plan.

### Public API Endpoints
7. **Base URL and Versioning**: All public endpoints under /public/v1/. Version in URL allows future API versions without breaking consumers.
8. **Pagination on List Endpoints**: All list endpoints support limit (default 10, max 100) and offset query params. Response includes pagination metadata: offset, limit, total, has_more.
9. **Filtering and Sorting**: List endpoints support query param filters (e.g., ?status=active&visibility=private). Sorting via ?sort_by=created_at&sort_order=asc.
10. **Error Response Format**: All errors return JSON with: { "error": "error message", "error_code": "ERROR_CODE", "details": {...} }. HTTP status codes: 400 Bad Request, 401 Unauthorized, 403 Forbidden, 404 Not Found, 429 Too Many Requests, 500 Internal Server Error.

### Webhook Management
11. **Webhook Endpoint CRUD**: Tenants register, list, update, delete webhook endpoints via /public/v1/webhook-endpoints. Endpoints stored in database with tenant_id, url, event_types, active status, signing_secret.
12. **Webhook Delivery Worker**: RabbitMQ consumer processes WebhookEvent messages. For each event, find registered endpoints matching event_type. Post request to endpoint URL with signed payload.
13. **Webhook Signature Scheme**: All webhooks signed with HMAC-SHA256. Format: v1,signature=<hex>. Computed over: {event_id}{timestamp}{request_body_json}.
14. **Webhook Retry Strategy**: Failed deliveries retry with exponential backoff: 30s, 5m, 1h, 24h. After 4th failure, webhook is dead-lettered. Tenant can manually retry.
15. **Webhook Idempotency**: Each webhook has unique event_id. Consumer can deduplicate on event_id if webhook is delivered twice.

### Idempotency
16. **Idempotency Key Support**: POST/PATCH/DELETE endpoints accept Idempotency-Key header. Key + PAT_id forms unique constraint. Results cached for 24 hours.
17. **Idempotency Response**: If request already processed, cached result is returned with same HTTP status as original. Replayed responses are served from IdempotencyRequest cache.

### Key Entities

**PersonalAccessToken (PAT)**
- `token_id` (UUID, PK)
- `tenant_id` (UUID, FK → Tenant)
- `token_hash` (string, SHA256 hex, unique)
- `token_name` (string, 255)
- `permissions` (JSON array of strings)
- `created_by_user_id` (UUID, FK → User)
- `created_at` (datetime)
- `expiration_date` (datetime)
- `last_used_at` (datetime, nullable)
- `is_active` (bool, default: true)
- `revoked_at` (datetime, nullable)
- `revoked_reason` (string, nullable)

**WebhookEndpoint**
- `webhook_endpoint_id` (UUID, PK)
- `tenant_id` (UUID, FK → Tenant)
- `url` (string, 2048)
- `event_types` (JSON array of strings)
- `signing_secret` (string, unique per endpoint)
- `description` (string, 1000, nullable)
- `active` (bool, default: true)
- `created_at` (datetime)
- `updated_at` (datetime)
- `last_successful_delivery` (datetime, nullable)
- `failure_count` (int, default: 0)
- `next_retry_at` (datetime, nullable)

**WebhookDelivery**
- `delivery_id` (UUID, PK)
- `webhook_endpoint_id` (UUID, FK → WebhookEndpoint)
- `webhook_event_id` (UUID, FK → WebhookEvent)
- `tenant_id` (UUID, FK → Tenant)
- `status` (enum: Pending, Delivered, Failed, DeadLettered, Retrying)
- `attempt_number` (int, default: 1)
- `request_body` (text, webhook payload)
- `response_status_code` (int, nullable)
- `response_body` (text, first 1000 chars, nullable)
- `error_message` (string, nullable)
- `next_retry_at` (datetime, nullable)
- `delivered_at` (datetime, nullable)
- `created_at` (datetime)
- `updated_at` (datetime)

**WebhookEvent**
- `event_id` (UUID, PK)
- `tenant_id` (UUID, FK → Tenant)
- `event_type` (string, 100)
- `resource_id` (UUID, nullable)
- `resource_type` (string, nullable)
- `payload` (JSON)
- `created_at` (datetime)

**IdempotencyRequest**
- `idempotency_id` (UUID, PK)
- `pat_id` (UUID, FK → PersonalAccessToken)
- `idempotency_key` (string, 255)
- `http_method` (string: POST, PATCH, DELETE)
- `endpoint_path` (string, 255)
- `response_status` (int)
- `response_body` (text, first 5000 chars)
- `created_at` (datetime)
- `expires_at` (datetime, 24-hour TTL)

**ApiQuota**
- `quota_id` (UUID, PK)
- `pat_id` (UUID, FK → PersonalAccessToken)
- `subscription_tier` (enum: Standard, Growth, Enterprise)
- `requests_per_hour_limit` (int)
- `requests_used_this_hour` (int)
- `hour_reset_at` (datetime)
- `updated_at` (datetime)

## Success Criteria

- [ ] PAT creation returns valid token with correct hash stored (plaintext not stored)
- [ ] Rate limiting is enforced accurately (429 returned at quota boundary)
- [ ] All API list endpoints return paginated responses with correct pagination metadata
- [ ] POST /public/v1/rooms creates room and returns join_token within 500ms
- [ ] Webhook endpoints accept registration and storage within 200ms
- [ ] Webhooks are signed with correct HMAC-SHA256 signature (100% of deliveries)
- [ ] Webhook retries occur with correct exponential backoff timing
- [ ] Idempotency keys prevent duplicate room creation on retry
- [ ] All API errors return proper JSON format with error codes
- [ ] Cross-tenant data isolation is enforced (100% test coverage)
- [ ] Webhook delivery audit trail is complete (all deliveries logged)

## Edge Cases

1. **PAT Revoked During Request**: Request arrives, PAT is validated as active. Midway through request, PAT is revoked. Request completes (no mid-request revocation). Future requests fail with 401.

2. **Rate Limit Reset Boundary**: Request 999 at 14:59:50 UTC. Quota resets at 15:00:00. Request 1000 at 15:00:00 UTC is allowed (new hour bucket).

3. **Webhook URL Returns Error on First Attempt**: First delivery gets 500 error. Retry scheduled. Retries succeed on 2nd attempt (30s later). Status is "Delivered", not retried to 4th time.

4. **Webhook Event Arrives Before Endpoint Registered**: Event occurs. Webhook endpoint not yet registered. Event is created but no WebhookDelivery. When endpoint registered later, no retroactive delivery.

5. **Large Webhook Payload**: Webhook payload is 10MB. Request to endpoint includes full payload. Response is stored (first 1000 chars). Delivery status is "Delivered" regardless of size.

6. **Idempotency Key Not Provided**: POST request without Idempotency-Key header. Request is processed normally. Response does not include Idempotency-Key header. Every identical retry creates new resource.

7. **Invalid Idempotency Key Format**: Idempotency-Key header is empty string or very long (> 1000 chars). Request is rejected with 400 Bad Request.

8. **Concurrent Webhook Registrations**: Two requests register webhooks simultaneously with same URL. Both endpoints are created (separate records). Both receive deliveries for subscribed events.

9. **Webhook Endpoint URL Unreachable**: URL is invalid (DNS fails, network unreachable). First delivery fails with network error. Retries scheduled. After 4 failures, dead-lettered. Tenant receives alert.

10. **PAT with No Permissions**: PAT created with empty permissions array. Any API call returns 403 Forbidden (no permissions granted).

## Assumptions

1. **PAT Secret is One-Time Exposure**: Token value shown only at creation time. Stored as hash only. No recovery mechanism.

2. **Rate Limits are Per-Hour Window**: Quota resets at top of hour (UTC). No rolling 60-minute window in v1.

3. **Webhook Signatures Use HMAC-SHA256**: Assume SHA256 is sufficient. No option for weaker hashing in v1.

4. **Webhook Endpoints Accept JSON**: Assume all webhook consumers accept application/json content type. No form-encoded support.

5. **Idempotency Keys are UUID or String**: Consumer generates unique idempotency key per request. Platform treats keys opaquely (no format validation).

6. **Webhook Delivery is Best-Effort**: Failed deliveries retry but are not guaranteed. Consumers must handle gaps and may receive events out of order.

7. **API Responses Do Not Include Sensitive Data**: Assume responses exclude passwords, tokens, full credit card numbers. Audit trails are excluded from public API responses.

8. **Pagination Offset Not Recommended for Large Offsets**: Offsets > 100,000 are discouraged (database inefficiency). Consumers should use cursor-based pagination if available in future.

9. **Event Payload Structure Stable**: Event payloads follow schema for each event_type. Schema changes are versioned (new event types) not breaking changes.

10. **Tenant Can Have Multiple Webhooks**: No limit on webhook endpoints per tenant in v1. Scaling considerations at 1000+ endpoints per tenant.

## Dependencies

- **Epic 1 (Identity)**: PAT ownership tied to User within Tenant. Permissions checked via Identity module.
- **Epic 2 (Tenancy)**: API quotas tied to subscription tier. Subscription status checked for all API calls.
- **Epic 3 (Rooms)**: Room events (started, ended, participant joined/left) trigger webhooks.
- **Epic 8 (Files & Artifacts)**: File events (uploaded, scanned, ready, downloaded) trigger webhooks.
- **Epic 9 (Billing & Metering)**: Payment and subscription events trigger webhooks.
- **RabbitMQ**: Webhook delivery worker consumes WebhookEvent messages and processes deliveries.
- **Redis**: Optional caching for rate limit counters to avoid database writes on every request.

---

**Document Version:** 1.0
**Last Updated:** 2026-04-03
**Module Owner:** Integration Engineering Team
**Status:** Ready for Implementation
