# Epic 12: Public API & Webhooks Module - Task Breakdown

**Document Version:** 1.0
**Last Updated:** 2026-04-03
**Module:** PublicApi
**Dependencies:** Epic 2 (Tenancy), Epic 3 (Rooms), Epic 8 (Files), Epic 9 (Billing)

---

## Phase 1: Setup & Infrastructure

### T001: Create Public API Module Structure
- **Description:** Initialize PublicApi module directory structure and base configuration
- **Files to Create:**
  - `backend/src/Modules/PublicApi/Domain/` (entities, value objects)
  - `backend/src/Modules/PublicApi/Application/` (commands, queries, handlers)
  - `backend/src/Modules/PublicApi/Infrastructure/` (persistence, webhook delivery)
  - `backend/src/Modules/PublicApi/Api/` (controllers, DTOs, versioning)
- **Acceptance:** Module scaffold created with proper folder structure and API versioning setup

### T002: Define Public API Domain Models
- **Description:** Create core entities: PersonalAccessToken, WebhookEndpoint, WebhookEvent, WebhookDelivery, IdempotencyRequest, ApiQuota
- **Files:**
  - `backend/src/Modules/PublicApi/Domain/PersonalAccessToken.cs`
  - `backend/src/Modules/PublicApi/Domain/WebhookEndpoint.cs`
  - `backend/src/Modules/PublicApi/Domain/WebhookEvent.cs`
  - `backend/src/Modules/PublicApi/Domain/WebhookDelivery.cs`
  - `backend/src/Modules/PublicApi/Domain/IdempotencyRequest.cs`
  - `backend/src/Modules/PublicApi/Domain/ApiQuota.cs`
  - `backend/src/Modules/PublicApi/Domain/Enums/` (WebhookDeliveryStatus, PaymentMethod, etc.)
- **Acceptance:** All entities with proper validation and immutability

### T003: Create Database Schema and Migrations
- **Description:** Create SQL Server schema for PublicApi module
- **Files:**
  - `backend/src/Modules/PublicApi/Infrastructure/Persistence/PublicApiDbContext.cs`
  - `backend/src/Modules/PublicApi/Infrastructure/Persistence/Migrations/` (initial schema)
- **Acceptance:** Schema with tables for tokens, webhooks, events, deliveries, quotas, indexes on lookups

### T004: Implement Personal Access Token Service
- **Description:** Create service for PAT generation, hashing, validation
- **Files:**
  - `backend/src/Modules/PublicApi/Infrastructure/PersonalAccessTokenService.cs`
  - `backend/src/Modules/PublicApi/Infrastructure/TokenHasher.cs`
  - `backend/src/Modules/PublicApi/Infrastructure/TokenValidator.cs`
- **Acceptance:** Tokens generated in format `muntada_pat_*`, hashed with SHA256, validation works

### T005: Implement Rate Limiting Service
- **Description:** Create Redis-based rate limiter with sliding window per PAT
- **Files:**
  - `backend/src/Modules/PublicApi/Infrastructure/RateLimitingService.cs`
  - `backend/src/Modules/PublicApi/Infrastructure/QuotaEnforcer.cs`
  - `backend/src/Modules/PublicApi/Infrastructure/RateLimitingMiddleware.cs`
- **Acceptance:** Tracks requests per hour in Redis, returns 429 at limit, headers included in all responses

### T006: Implement Webhook Signature Validator
- **Description:** Create HMAC-SHA256 signature validation for webhook security
- **Files:**
  - `backend/src/Modules/PublicApi/Infrastructure/WebhookSignatureValidator.cs`
  - `backend/src/Modules/PublicApi/Infrastructure/WebhookPayloadSigner.cs`
- **Acceptance:** Signatures computed correctly over event_id + timestamp + body, validation works

---

## Phase 2: Personal Access Token Management

### T007: [P] Implement PAT Creation API [US-12.1]
- **Description:** Create endpoint for tenant admins to generate new PATs
- **Files:**
  - `backend/src/Modules/PublicApi/Api/TokenManagementController.cs`
  - `backend/src/Modules/PublicApi/Application/Commands/CreatePersonalAccessTokenCommand.cs`
  - `backend/src/Modules/PublicApi/Application/Handlers/CreatePersonalAccessTokenHandler.cs`
- **Acceptance:** Token created with name, permissions, expiration, plaintext returned once, hash stored, audit logged

### T008: [P] Implement PAT Listing and Metadata API [US-12.1]
- **Description:** Create endpoint to list tokens with metadata (created date, expiry, last used, status)
- **Files:**
  - `backend/src/Modules/PublicApi/Application/Queries/GetPersonalAccessTokensQuery.cs`
  - `backend/src/Modules/PublicApi/Application/Handlers/GetPersonalAccessTokensQueryHandler.cs`
- **Acceptance:** Lists all tokens with metadata, filters by status/permissions, no plaintext tokens shown

### T009: [P] Implement PAT Revocation API [US-12.1]
- **Description:** Create endpoint to revoke tokens
- **Files:**
  - `backend/src/Modules/PublicApi/Application/Commands/RevokePersonalAccessTokenCommand.cs`
  - `backend/src/Modules/PublicApi/Application/Handlers/RevokePersonalAccessTokenHandler.cs`
- **Acceptance:** Token marked inactive, existing calls rejected, audit logged

### T010: [P] Implement PAT Authorization Middleware [US-12.1]
- **Description:** Create middleware to validate PAT on every API request
- **Files:**
  - `backend/src/Modules/PublicApi/Infrastructure/PatAuthorizationMiddleware.cs`
  - `backend/src/Modules/PublicApi/Infrastructure/PatPermissionValidator.cs`
- **Acceptance:** Authorization header parsed, token hashed, validated, expired/inactive rejected with 401

---

## Phase 3: Rate Limiting and Quotas

### T011: [P] Implement Rate Limiting Enforcement [US-12.2]
- **Description:** Enforce per-PAT rate limits with proper headers
- **Files:**
  - `backend/src/Modules/PublicApi/Infrastructure/RateLimitingMiddleware.cs` (updated)
  - `backend/src/Modules/PublicApi/Application/Queries/GetRateLimitStatusQuery.cs`
- **Acceptance:** Requests tracked per hour, 429 returned at limit, X-RateLimit-* headers on all responses

### T012: [P] Implement Quota Tier Configuration [US-12.2]
- **Description:** Create configuration mapping subscription tiers to API quotas
- **Files:**
  - `backend/src/Modules/PublicApi/Domain/QuotaTiers.cs`
  - `backend/src/Modules/PublicApi/Infrastructure/QuotaTierMapper.cs`
- **Acceptance:** Standard: 1000/hr, Growth: 5000/hr, Enterprise: 50000/hr quotas set

### T013: [P] Implement Rate Limit Reset Job [US-12.2]
- **Description:** Create background job to reset hourly quotas
- **Files:**
  - `backend/src/Modules/PublicApi/Infrastructure/Jobs/RateLimitResetJob.cs`
- **Acceptance:** Job runs hourly, resets request counters in Redis

---

## Phase 4: Public API Endpoints - Rooms

### T014: [P] Implement GET /public/v1/rooms (List Rooms) [US-12.3]
- **Description:** Create endpoint to list rooms with pagination and filtering
- **Files:**
  - `backend/src/Modules/PublicApi/Api/RoomsController.cs`
  - `backend/src/Modules/PublicApi/Application/Queries/GetRoomsQuery.cs`
  - `backend/src/Modules/PublicApi/Application/Handlers/GetRoomsQueryHandler.cs`
  - `backend/src/Modules/PublicApi/Api/Dtos/RoomDto.cs`
  - `backend/src/Modules/PublicApi/Api/Dtos/PaginationDto.cs`
- **Acceptance:** Endpoint supports limit (max 100), offset, status/visibility filters, returns paginated response

### T015: [P] Implement POST /public/v1/rooms (Create Room) [US-12.3]
- **Description:** Create endpoint to create rooms via API
- **Files:**
  - `backend/src/Modules/PublicApi/Application/Commands/CreateRoomViaApiCommand.cs`
  - `backend/src/Modules/PublicApi/Application/Handlers/CreateRoomViaApiHandler.cs`
  - `backend/src/Modules/PublicApi/Api/Dtos/CreateRoomRequest.cs`
  - `backend/src/Modules/PublicApi/Api/Dtos/CreateRoomResponse.cs`
- **Acceptance:** Returns room ID, join_token JWT, status 201 Created, audit logged with PAT ID

### T016: [P] Implement GET /public/v1/rooms/{roomId} (Room Details) [US-12.3]
- **Description:** Create endpoint to get detailed room information
- **Files:**
  - `backend/src/Modules/PublicApi/Application/Queries/GetRoomDetailsQuery.cs`
  - `backend/src/Modules/PublicApi/Application/Handlers/GetRoomDetailsQueryHandler.cs`
- **Acceptance:** Returns full room details including participants, recording status, metadata

### T017: [P] Implement DELETE /public/v1/rooms/{roomId} (Terminate Room) [US-12.3]
- **Description:** Create endpoint to terminate room via API
- **Files:**
  - `backend/src/Modules/PublicApi/Application/Commands/TerminateRoomViaApiCommand.cs`
  - `backend/src/Modules/PublicApi/Application/Handlers/TerminateRoomViaApiHandler.cs`
- **Acceptance:** Room terminated, participants disconnected, audit logged, reason recorded

---

## Phase 5: Public API Endpoints - Usage

### T018: [P] Implement GET /public/v1/usage (Current Usage) [US-12.4]
- **Description:** Create endpoint for current month usage metrics
- **Files:**
  - `backend/src/Modules/PublicApi/Application/Queries/GetCurrentUsageQuery.cs`
  - `backend/src/Modules/PublicApi/Application/Handlers/GetCurrentUsageQueryHandler.cs`
  - `backend/src/Modules/PublicApi/Api/Dtos/UsageDto.cs`
- **Acceptance:** Returns usage for all dimensions with limits and percentages

### T019: [P] Implement GET /public/v1/usage/history (Usage History) [US-12.4]
- **Description:** Create endpoint for historical usage data
- **Files:**
  - `backend/src/Modules/PublicApi/Application/Queries/GetUsageHistoryQuery.cs`
  - `backend/src/Modules/PublicApi/Application/Handlers/GetUsageHistoryQueryHandler.cs`
- **Acceptance:** Returns monthly snapshots for last 6-12 months

---

## Phase 6: Webhook Management

### T020: [P] Implement POST /public/v1/webhook-endpoints (Register Webhook) [US-12.5]
- **Description:** Create endpoint to register webhook endpoints
- **Files:**
  - `backend/src/Modules/PublicApi/Api/WebhookEndpointController.cs`
  - `backend/src/Modules/PublicApi/Application/Commands/RegisterWebhookEndpointCommand.cs`
  - `backend/src/Modules/PublicApi/Application/Handlers/RegisterWebhookEndpointHandler.cs`
  - `backend/src/Modules/PublicApi/Api/Dtos/RegisterWebhookRequest.cs`
  - `backend/src/Modules/PublicApi/Api/Dtos/WebhookEndpointDto.cs`
- **Acceptance:** Endpoint registered with event types, signing_secret generated (unique), status 201 Created, audit logged

### T021: [P] Implement GET /public/v1/webhook-endpoints (List Webhooks) [US-12.5]
- **Description:** Create endpoint to list registered webhook endpoints
- **Files:**
  - `backend/src/Modules/PublicApi/Application/Queries/GetWebhookEndpointsQuery.cs`
  - `backend/src/Modules/PublicApi/Application/Handlers/GetWebhookEndpointsQueryHandler.cs`
- **Acceptance:** Lists all endpoints with status, event types, delivery history

### T022: [P] Implement PATCH /public/v1/webhook-endpoints/{id} (Update Webhook) [US-12.5]
- **Description:** Create endpoint to update webhook configuration
- **Files:**
  - `backend/src/Modules/PublicApi/Application/Commands/UpdateWebhookEndpointCommand.cs`
  - `backend/src/Modules/PublicApi/Application/Handlers/UpdateWebhookEndpointHandler.cs`
- **Acceptance:** Can update active status, event types, audit logged

### T023: [P] Implement DELETE /public/v1/webhook-endpoints/{id} (Delete Webhook) [US-12.5]
- **Description:** Create endpoint to delete webhook endpoints
- **Files:**
  - `backend/src/Modules/PublicApi/Application/Commands/DeleteWebhookEndpointCommand.cs`
  - `backend/src/Modules/PublicApi/Application/Handlers/DeleteWebhookEndpointHandler.cs`
- **Acceptance:** Endpoint deleted, no more deliveries, audit logged

---

## Phase 7: Webhook Delivery

### T024: [P] Implement Webhook Event Publishing [US-12.6, US-12.7]
- **Description:** Create infrastructure to publish events as webhooks
- **Files:**
  - `backend/src/Modules/PublicApi/Infrastructure/WebhookEventPublisher.cs`
  - `backend/src/Modules/PublicApi/Application/Events/WebhookEventPublishedEvent.cs`
  - `backend/src/SharedKernel/DomainEvents/IWebhookEvent.cs`
- **Acceptance:** All domain events can be published as webhooks, event payloads include required data

### T025: [P] Implement Webhook Delivery Worker [US-12.6]
- **Description:** Create RabbitMQ consumer to deliver webhooks with retries
- **Files:**
  - `backend/src/Modules/PublicApi/Infrastructure/Workers/WebhookDeliveryWorker.cs`
  - `backend/src/Modules/PublicApi/Infrastructure/WebhookDeliveryService.cs`
  - `backend/src/Modules/PublicApi/Infrastructure/RetryScheduler.cs`
- **Acceptance:** Worker picks up WebhookEvent, finds matching endpoints, delivers with retries (30s, 5m, 1h, 24h)

### T026: [P] Implement Webhook Signature Generation and Delivery [US-12.6]
- **Description:** Add signatures to all webhook requests
- **Files:**
  - `backend/src/Modules/PublicApi/Infrastructure/WebhookSignedDelivery.cs`
  - (WebhookPayloadSigner updated from T006)
- **Acceptance:** All webhooks signed with HMAC-SHA256, signature format correct

### T027: [P] Implement Webhook Delivery Status Tracking [US-12.6]
- **Description:** Track WebhookDelivery status with response handling
- **Files:**
  - `backend/src/Modules/PublicApi/Application/Commands/UpdateWebhookDeliveryStatusCommand.cs`
  - `backend/src/Modules/PublicApi/Application/Handlers/UpdateWebhookDeliveryStatusHandler.cs`
- **Acceptance:** Status transitions (Pending → Delivered/Failed/DeadLettered), response body stored, retry scheduled on failure

### T028: [P] Implement Dead Letter Queue for Failed Webhooks [US-12.6]
- **Description:** Move failed webhooks to dead letter queue after max retries
- **Files:**
  - `backend/src/Modules/PublicApi/Infrastructure/WebhookDeadLetterHandler.cs`
  - `backend/src/Modules/PublicApi/Infrastructure/Jobs/WebhookDeadLetterAlerterJob.cs`
- **Acceptance:** After 4 failures, webhook moved to dead letter, tenant notified via email

### T029: [P] Implement Webhook Event Type Coverage [US-12.7]
- **Description:** Implement handlers to publish all event types as webhooks
- **Files:**
  - `backend/src/Modules/PublicApi/Application/Handlers/RoomEventWebhookPublishers.cs`
  - `backend/src/Modules/PublicApi/Application/Handlers/ParticipantEventWebhookPublishers.cs`
  - `backend/src/Modules/PublicApi/Application/Handlers/FileEventWebhookPublishers.cs`
  - `backend/src/Modules/PublicApi/Application/Handlers/PaymentEventWebhookPublishers.cs`
  - `backend/src/Modules/PublicApi/Application/Handlers/RecordingEventWebhookPublishers.cs`
- **Acceptance:** All event types (room.*, participant.*, file.*, payment.*, recording.*, subscription.*) published

---

## Phase 8: Idempotency

### T030: [P] Implement Idempotency Key Handling [US-12.8]
- **Description:** Create idempotency middleware and cache for POST/PATCH/DELETE requests
- **Files:**
  - `backend/src/Modules/PublicApi/Infrastructure/IdempotencyMiddleware.cs`
  - `backend/src/Modules/PublicApi/Infrastructure/IdempotencyCache.cs`
  - `backend/src/Modules/PublicApi/Application/Commands/StoreIdempotencyResultCommand.cs`
- **Acceptance:** Idempotency-Key header parsed, results cached for 24 hours, cached result returned on retry

### T031: [P] Implement Idempotency Key Validation [US-12.8]
- **Description:** Validate idempotency key format and enforce uniqueness per PAT
- **Files:**
  - `backend/src/Modules/PublicApi/Infrastructure/IdempotencyKeyValidator.cs`
- **Acceptance:** Keys validated (not empty, < 1000 chars), enforced unique per PAT + endpoint

---

## Phase 9: Error Handling & Documentation

### T032: [P] Implement Standard Error Response Format
- **Description:** Create consistent error response format across all API endpoints
- **Files:**
  - `backend/src/Modules/PublicApi/Api/ErrorResponses/ErrorResponseDto.cs`
  - `backend/src/Modules/PublicApi/Api/Middleware/ErrorHandlingMiddleware.cs`
  - `backend/src/Modules/PublicApi/Api/ExceptionHandlers/` (custom handlers per error type)
- **Acceptance:** All errors return JSON with error message, error_code, details, proper HTTP status codes

### T033: [P] Implement API Request/Response Logging [US-12.1+]
- **Description:** Add comprehensive logging for all API calls
- **Files:**
  - `backend/src/Modules/PublicApi/Infrastructure/ApiCallLogger.cs`
  - `backend/src/SharedKernel/Middleware/ApiLoggingMiddleware.cs`
- **Acceptance:** All requests logged with method, endpoint, status, latency, PAT ID

---

## Phase 10: Testing & Documentation

### T034: [P] Write Unit Tests for Public API Domain and Application
- **Description:** Comprehensive unit tests for all commands, handlers, queries
- **Files:**
  - `backend/src/Modules/PublicApi/Tests/Domain/PersonalAccessTokenTests.cs`
  - `backend/src/Modules/PublicApi/Tests/Domain/WebhookEndpointTests.cs`
  - `backend/src/Modules/PublicApi/Tests/Application/TokenManagementTests.cs`
  - `backend/src/Modules/PublicApi/Tests/Application/RateLimitingTests.cs`
  - `backend/src/Modules/PublicApi/Tests/Application/WebhookTests.cs`
  - `backend/src/Modules/PublicApi/Tests/Application/IdempotencyTests.cs`
- **Acceptance:** > 85% code coverage, all scenarios tested

### T035: [P] Write Integration Tests for Public API
- **Description:** Integration tests for all API workflows
- **Files:**
  - `backend/src/Modules/PublicApi/Tests/Integration/TokenManagementWorkflowTests.cs`
  - `backend/src/Modules/PublicApi/Tests/Integration/RoomApiWorkflowTests.cs`
  - `backend/src/Modules/PublicApi/Tests/Integration/UsageApiTests.cs`
  - `backend/src/Modules/PublicApi/Tests/Integration/WebhookWorkflowTests.cs`
  - `backend/src/Modules/PublicApi/Tests/Integration/RateLimitingTests.cs`
  - `backend/src/Modules/PublicApi/Tests/Integration/IdempotencyTests.cs`
  - `backend/src/Modules/PublicApi/Tests/Integration/ErrorHandlingTests.cs`
- **Acceptance:** All user stories end-to-end tested, cross-tenant isolation verified

### T036: [P] Write Frontend/Client Library Tests (if applicable)
- **Description:** Tests for client SDK or integration examples
- **Files:**
  - `docs/sdk/typescript-client/__tests__/` (if SDK provided)
- **Acceptance:** SDK works with all API endpoints

### T037: Create Public API Documentation
- **Description:** OpenAPI/Swagger documentation and integration guides
- **Files:**
  - `docs/modules/public-api/README.md`
  - `docs/modules/public-api/openapi.yaml` (or Swagger JSON)
  - `docs/modules/public-api/authentication.md`
  - `docs/modules/public-api/rate-limiting.md`
  - `docs/modules/public-api/endpoints.md` (detailed per endpoint)
  - `docs/modules/public-api/webhooks.md`
  - `docs/modules/public-api/idempotency.md`
  - `docs/modules/public-api/error-codes.md`
  - `docs/sdk/typescript-client/README.md` (SDK examples)
  - `docs/modules/public-api/INTEGRATION_GUIDE.md`
- **Acceptance:** Complete OpenAPI spec, all endpoints documented, examples provided

---

## Checkpoint 1: PAT & Rate Limiting (After T013)
**Criteria:**
- PAT creation/listing/revocation working
- Rate limiting enforced correctly
- Quota tiers configured
- All Phase 2-3 tests passing

## Checkpoint 2: API Endpoints (After T019)
**Criteria:**
- Room endpoints (list/create/get/delete) working
- Usage endpoints working
- Proper pagination and filtering
- All responses include rate limit headers

## Checkpoint 3: Webhooks (After T029)
**Criteria:**
- Webhook endpoints CRUD working
- Events publishing to webhooks
- Delivery worker processing with retries
- Signature validation working
- All event types covered

## Checkpoint 4: Full Feature Complete (After T037)
**Criteria:**
- All 8 user stories implemented (US-12.1 through US-12.8)
- Idempotency keys working
- Error handling consistent
- All tests passing > 85% coverage
- OpenAPI documentation complete
- Webhook dead letter queue working

---

## Dependencies Between Tasks

- **T001-T006** (Setup): Must complete before all other tasks
- **T007-T010**: PAT management, can proceed in parallel
- **T011-T013**: Rate limiting, proceed in parallel
- **T014-T019**: API endpoints, can proceed in parallel
- **T020-T023**: Webhook management, can proceed in parallel
- **T024-T029**: Webhook delivery, some sequential (T024→T025)
- **T030-T031**: Idempotency, can proceed in parallel
- **T032-T033**: Error handling, can proceed in parallel
- **T034-T037**: Testing/docs, depend on all implementations

---

## Success Metrics

- PAT creation returns valid token with correct hash storage
- Rate limiting enforced accurately (429 at quota boundary)
- All list endpoints return paginated responses correctly
- POST /public/v1/rooms creates room within 500ms
- Webhook endpoints accept registration within 200ms
- Webhooks signed with correct HMAC-SHA256 (100% of deliveries)
- Webhook retries occur with correct exponential backoff
- Idempotency keys prevent duplicate operations on retry
- All API errors return proper JSON format
- Cross-tenant data isolation enforced (100% test coverage)

---

**Notes:**
- API base URL: `/public/v1/`
- All timestamps in ISO 8601 format (UTC)
- All IDs are UUIDs
- PAT tokens format: `muntada_pat_{random_32_chars}`
- Signing secret format: `whsec_{random_32_chars}`
- Webhook retries: 30s, 5m, 1h, 24h (max 4 attempts)
- Idempotency key TTL: 24 hours in Redis
- Rate limit window: hourly (per-hour bucket)
- All webhook payloads include: id, timestamp, event_type, data
- SQLServer indexes: (patId, isActive), (webhookEndpointId, active), (eventType)
- RabbitMQ queues: `webhooks.deliver` (main), `webhooks.deadletter` (failed)
