# Epic 9: Billing & Metering Module - Task Breakdown

**Document Version:** 1.0
**Last Updated:** 2026-04-03
**Module:** Billing
**Dependencies:** Epic 2 (Tenancy), Epic 3 (Rooms), Epic 8 (Files)

---

## Phase 1: Setup & Infrastructure

### T001: Create Billing Module Structure
- **Description:** Initialize Billing module directory structure and base configuration
- **Files to Create:**
  - `backend/src/Modules/Billing/Domain/` (entities, value objects)
  - `backend/src/Modules/Billing/Application/` (commands, queries, handlers)
  - `backend/src/Modules/Billing/Infrastructure/` (persistence, payment providers, job scheduler)
  - `backend/src/Modules/Billing/Api/` (controllers, DTOs)
- **Acceptance:** Module scaffold created with proper folder structure

### T002: Define Billing Domain Models
- **Description:** Create core domain entities: Subscription, UsageMeter, BillingMeterSnapshot, Invoice, PaymentTransaction, RefundTransaction
- **Files:**
  - `backend/src/Modules/Billing/Domain/Subscription.cs`
  - `backend/src/Modules/Billing/Domain/UsageMeter.cs`
  - `backend/src/Modules/Billing/Domain/BillingMeterSnapshot.cs`
  - `backend/src/Modules/Billing/Domain/Invoice.cs`
  - `backend/src/Modules/Billing/Domain/PaymentTransaction.cs`
  - `backend/src/Modules/Billing/Domain/RefundTransaction.cs`
  - `backend/src/Modules/Billing/Domain/Plan.cs`
  - `backend/src/Modules/Billing/Domain/Enums/` (SubscriptionStatus, PaymentStatus, PlanTier, etc.)
- **Acceptance:** All entities with validation, business logic, and proper aggregates

### T003: Create Database Schema and Migrations
- **Description:** Create SQL Server schema for Billing module with all tables and indexes
- **Files:**
  - `backend/src/Modules/Billing/Infrastructure/Persistence/BillingDbContext.cs`
  - `backend/src/Modules/Billing/Infrastructure/Persistence/Migrations/` (initial schema)
- **Acceptance:** Schema with tables for subscriptions, meters, invoices, transactions, audit columns

### T004: Implement HyperPay Integration Service
- **Description:** Create REST client for HyperPay payment API (initiate payment, verify, refund)
- **Files:**
  - `backend/src/Modules/Billing/Infrastructure/PaymentProviders/HyperPayClient.cs`
  - `backend/src/Modules/Billing/Infrastructure/PaymentProviders/HyperPayModels.cs`
  - `backend/src/Modules/Billing/Infrastructure/PaymentProviders/HyperPayWebhookValidator.cs`
- **Acceptance:** Client can initiate payments, handle responses, validate webhooks with HMAC-SHA256

### T005: Implement Moyasar Integration Service
- **Description:** Create REST client for Moyasar payment API (alternative/fallback provider)
- **Files:**
  - `backend/src/Modules/Billing/Infrastructure/PaymentProviders/MoyasarClient.cs`
  - `backend/src/Modules/Billing/Infrastructure/PaymentProviders/MoyasarModels.cs`
  - `backend/src/Modules/Billing/Infrastructure/PaymentProviders/MoyasarWebhookValidator.cs`
- **Acceptance:** Client can initiate payments, handle responses, validate webhooks

### T006: Implement Usage Metering Service
- **Description:** Create real-time usage tracking across all dimensions
- **Files:**
  - `backend/src/Modules/Billing/Infrastructure/UsageMeteringService.cs`
  - `backend/src/Modules/Billing/Infrastructure/RedisMeteringCache.cs`
  - `backend/src/Modules/Billing/Domain/Services/MeteringLimitChecker.cs`
- **Acceptance:** Tracks OTP, storage, recording, AI usage, concurrent rooms with Redis cache

### T007: Setup Scheduled Jobs Infrastructure
- **Description:** Configure Quartz/Hangfire for scheduled billing jobs
- **Files:**
  - `backend/src/Modules/Billing/Infrastructure/Jobs/BillingJobSetup.cs`
  - `backend/src/Modules/Billing/Infrastructure/Jobs/JobScheduler.cs`
- **Acceptance:** Job scheduler initialized and ready for billing jobs

---

## Phase 2: Subscription Management

### T008: [P] Implement Subscription Assignment [US-9.1]
- **Description:** Create command to assign initial plan to tenant on creation
- **Files:**
  - `backend/src/Modules/Billing/Application/Commands/AssignSubscriptionCommand.cs`
  - `backend/src/Modules/Billing/Application/Handlers/AssignSubscriptionHandler.cs`
  - `backend/src/Modules/Billing/Api/SubscriptionController.cs`
- **Acceptance:** Subscription created with plan, status "Active", audit event recorded

### T009: [P] Implement Plan Upgrade [US-9.1]
- **Description:** Create workflow for tenant plan upgrade with prorated billing
- **Files:**
  - `backend/src/Modules/Billing/Application/Commands/UpgradeSubscriptionCommand.cs`
  - `backend/src/Modules/Billing/Application/Handlers/UpgradeSubscriptionHandler.cs`
  - `backend/src/Modules/Billing/Domain/Services/ProratingCalculator.cs`
- **Acceptance:** Previous subscription marked "Superseded", new subscription created, prorated refund calculated

### T010: [P] Implement Plan Downgrade [US-9.1]
- **Description:** Create workflow for tenant plan downgrade with refund
- **Files:**
  - `backend/src/Modules/Billing/Application/Commands/DowngradeSubscriptionCommand.cs`
  - `backend/src/Modules/Billing/Application/Handlers/DowngradeSubscriptionHandler.cs`
- **Acceptance:** Downgrade allowed if usage <= new plan limits, refund calculated and initiated

### T011: [P] Implement Subscription Suspension [US-9.1]
- **Description:** Create command to suspend subscription (due to payment failure)
- **Files:**
  - `backend/src/Modules/Billing/Application/Commands/SuspendSubscriptionCommand.cs`
  - `backend/src/Modules/Billing/Application/Handlers/SuspendSubscriptionHandler.cs`
- **Acceptance:** Subscription status "Suspended", room creation blocked, existing rooms continue

### T012: [P] Implement Subscription Reactivation [US-9.1]
- **Description:** Create command to reactivate suspended subscription
- **Files:**
  - `backend/src/Modules/Billing/Application/Commands/ReactivateSubscriptionCommand.cs`
  - `backend/src/Modules/Billing/Application/Handlers/ReactivateSubscriptionHandler.cs`
- **Acceptance:** Subscription status "Active", room creation re-enabled, audit event recorded

---

## Phase 3: Usage Metering & Limits

### T013: [P] Implement Concurrent Room Metering [US-9.2]
- **Description:** Track concurrent active rooms per tenant and enforce limit
- **Files:**
  - `backend/src/Modules/Billing/Application/Commands/IncrementConcurrentRoomCommand.cs`
  - `backend/src/Modules/Billing/Application/Commands/DecrementConcurrentRoomCommand.cs`
  - `backend/src/Modules/Billing/Application/Handlers/IncrementConcurrentRoomHandler.cs`
  - `backend/src/Modules/Billing/Application/Handlers/DecrementConcurrentRoomHandler.cs`
  - `backend/src/Modules/Billing/Domain/Services/ConcurrentRoomLimitChecker.cs`
- **Acceptance:** Metrics incremented/decremented atomically in Redis, limits enforced, soft limit warnings

### T014: [P] Implement OTP Sends Metering [US-9.3]
- **Description:** Track OTP sends per month and enforce per-plan limits
- **Files:**
  - `backend/src/Modules/Billing/Application/Commands/RecordOtpSendCommand.cs`
  - `backend/src/Modules/Billing/Application/Handlers/RecordOtpSendHandler.cs`
  - `backend/src/Modules/Billing/Domain/Services/OtpLimitChecker.cs`
- **Acceptance:** Soft limit warning at 80%, hard limit blocks at 100%, meter resets monthly

### T015: [P] Implement Storage Consumption Metering [US-9.3]
- **Description:** Track aggregate file storage and enforce per-plan limits
- **Files:**
  - `backend/src/Modules/Billing/Application/Commands/UpdateStorageConsumptionCommand.cs`
  - `backend/src/Modules/Billing/Application/Handlers/UpdateStorageConsumptionHandler.cs`
  - `backend/src/Modules/Billing/Infrastructure/Jobs/StorageMeteringJob.cs`
  - `backend/src/Modules/Billing/Domain/Services/StorageLimitChecker.cs`
- **Acceptance:** Daily job aggregates FileArtifact sizes, calculates consumption, enforces limits

### T016: [P] Implement Recording Minutes Metering [US-9.3]
- **Description:** Track recording minutes and enforce per-plan limits
- **Files:**
  - `backend/src/Modules/Billing/Application/Commands/RecordRecordingMinutesCommand.cs`
  - `backend/src/Modules/Billing/Application/Handlers/RecordRecordingMinutesHandler.cs`
  - `backend/src/Modules/Billing/Domain/Services/RecordingLimitChecker.cs`
- **Acceptance:** Minutes accumulated when recording stops, limits enforced, overages charged

### T017: [P] Implement AI Usage Metering [US-9.3]
- **Description:** Track AI feature usage and enforce reserved capacity limits
- **Files:**
  - `backend/src/Modules/Billing/Application/Commands/RecordAiUsageCommand.cs`
  - `backend/src/Modules/Billing/Application/Handlers/RecordAiUsageHandler.cs`
  - `backend/src/Modules/Billing/Domain/Services/AiLimitChecker.cs`
- **Acceptance:** Requests tracked, reserved capacity enforced, overages identified for charging

### T018: [P] Implement Meter Snapshots and Reset Job [US-9.3]
- **Description:** Create daily/monthly snapshots of usage meters and reset monthly meters
- **Files:**
  - `backend/src/Modules/Billing/Infrastructure/Jobs/BillingMeterSnapshotJob.cs`
  - `backend/src/Modules/Billing/Application/Commands/CreateMeterSnapshotCommand.cs`
  - `backend/src/Modules/Billing/Application/Commands/ResetMonthlyMetersCommand.cs`
  - `backend/src/Modules/Billing/Application/Handlers/CreateMeterSnapshotHandler.cs`
  - `backend/src/Modules/Billing/Application/Handlers/ResetMonthlyMetersHandler.cs`
- **Acceptance:** Snapshots created at month end, meters reset on first of month, audit events recorded

---

## Phase 4: Payment Processing

### T019: [P] Implement Payment Initiation [US-9.4]
- **Description:** Create API to initiate payment with HyperPay/Moyasar
- **Files:**
  - `backend/src/Modules/Billing/Api/PaymentController.cs`
  - `backend/src/Modules/Billing/Application/Commands/InitiatePaymentCommand.cs`
  - `backend/src/Modules/Billing/Application/Handlers/InitiatePaymentHandler.cs`
  - `backend/src/Modules/Billing/Infrastructure/PaymentInitializationService.cs`
- **Acceptance:** Returns payment URL valid for 10 minutes, PaymentTransaction created with "Initiated" status

### T020: [P] Implement Payment Webhook Handler [US-9.4]
- **Description:** Create webhook endpoints for payment completion from HyperPay and Moyasar
- **Files:**
  - `backend/src/Modules/Billing/Api/PaymentWebhookController.cs`
  - `backend/src/Modules/Billing/Application/Commands/ProcessPaymentWebhookCommand.cs`
  - `backend/src/Modules/Billing/Application/Handlers/ProcessPaymentWebhookHandler.cs`
  - `backend/src/Modules/Billing/Infrastructure/WebhookSignatureValidator.cs`
- **Acceptance:** Webhooks validated with HMAC-SHA256, payment status updated, invoice marked paid, subscription reactivated

### T021: [P] Implement Payment Status Polling [US-9.4]
- **Description:** Create background job to poll payment status and handle timeouts
- **Files:**
  - `backend/src/Modules/Billing/Infrastructure/Jobs/PaymentStatusPollingJob.cs`
  - `backend/src/Modules/Billing/Application/Commands/PollPaymentStatusCommand.cs`
- **Acceptance:** Job polls HyperPay/Moyasar every 10 seconds, updates status, records timeout events

### T022: [P] Implement Refund Processing [US-9.4]
- **Description:** Create refund workflow for plan downgrades and overpayments
- **Files:**
  - `backend/src/Modules/Billing/Application/Commands/InitiateRefundCommand.cs`
  - `backend/src/Modules/Billing/Application/Handlers/InitiateRefundHandler.cs`
  - `backend/src/Modules/Billing/Infrastructure/RefundProcessor.cs`
  - `backend/src/Modules/Billing/Domain/Services/RefundCalculator.cs`
- **Acceptance:** Refund amount calculated, API called, RefundTransaction tracked, completion confirmed via webhook

---

## Phase 5: Invoicing

### T023: [P] Implement Invoice Generation Job [US-9.5]
- **Description:** Create monthly batch job for invoice generation
- **Files:**
  - `backend/src/Modules/Billing/Infrastructure/Jobs/InvoiceGenerationJob.cs`
  - `backend/src/Modules/Billing/Application/Commands/GenerateInvoiceCommand.cs`
  - `backend/src/Modules/Billing/Application/Handlers/GenerateInvoiceHandler.cs`
  - `backend/src/Modules/Billing/Infrastructure/InvoiceGenerator.cs`
- **Acceptance:** Job runs on 1st of month, creates Invoice records with line items, calculates totals and tax

### T024: [P] Implement Invoice PDF Generation [US-9.5]
- **Description:** Create PDF template and generation service for invoices
- **Files:**
  - `backend/src/Modules/Billing/Infrastructure/InvoicePdfGenerator.cs`
  - `backend/src/Modules/Billing/Infrastructure/InvoicePdfTemplate.html` (or use QuestPDF library)
- **Acceptance:** PDF generated in < 5 seconds, stored in MinIO, URL persisted in Invoice record

### T025: [P] Implement Invoice Retrieval API [US-9.5]
- **Description:** Create endpoints for retrieving and downloading invoices
- **Files:**
  - `backend/src/Modules/Billing/Api/InvoiceController.cs`
  - `backend/src/Modules/Billing/Application/Queries/GetInvoiceQuery.cs`
  - `backend/src/Modules/Billing/Application/Queries/GetInvoicesQuery.cs`
  - `backend/src/Modules/Billing/Application/Handlers/GetInvoiceQueryHandler.cs`
  - `backend/src/Modules/Billing/Application/Handlers/GetInvoicesQueryHandler.cs`
- **Acceptance:** Can retrieve invoices by ID, list with pagination and filters, download PDFs

### T026: [P] Implement Invoice Email Notification [US-9.5]
- **Description:** Create email notifications when invoices are generated
- **Files:**
  - `backend/src/Modules/Billing/Infrastructure/InvoiceEmailService.cs`
  - `backend/src/Modules/Billing/Application/Events/InvoiceGeneratedEvent.cs`
  - `backend/src/Modules/Billing/Application/Handlers/InvoiceGeneratedEventHandler.cs`
- **Acceptance:** Email sent to tenant admin with invoice PDF attached or download link

---

## Phase 6: Billing Dashboard & Frontend

### T027: [P] Implement Billing Dashboard Backend API [US-9.6]
- **Description:** Create API endpoints for billing dashboard data
- **Files:**
  - `backend/src/Modules/Billing/Api/BillingDashboardController.cs`
  - `backend/src/Modules/Billing/Application/Queries/GetBillingDashboardQuery.cs`
  - `backend/src/Modules/Billing/Application/Handlers/GetBillingDashboardQueryHandler.cs`
  - `backend/src/Modules/Billing/Application/Queries/GetCurrentUsageQuery.cs`
  - `backend/src/Modules/Billing/Application/Handlers/GetCurrentUsageQueryHandler.cs`
- **Acceptance:** Returns subscription status, usage meters, invoice history, payment methods

### T028: [P] Implement Billing Dashboard UI [US-9.6]
- **Description:** Create React components for tenant billing dashboard
- **Files:**
  - `frontend/src/features/billing/BillingDashboard.tsx`
  - `frontend/src/features/billing/SubscriptionCard.tsx`
  - `frontend/src/features/billing/UsageMeterDisplay.tsx`
  - `frontend/src/features/billing/InvoiceHistoryTable.tsx`
  - `frontend/src/features/billing/PaymentMethodsSection.tsx`
  - `frontend/src/features/billing/PlanChangeModal.tsx`
  - `frontend/src/features/billing/useBillingDashboard.ts`
- **Acceptance:** Dashboard loads in < 3 seconds, shows all metrics, modal for plan changes

### T029: [P] Implement Payment Initiation UI [US-9.4]
- **Description:** Create payment flow in React (show invoice, initiate payment, redirect)
- **Files:**
  - `frontend/src/features/billing/PaymentInitiationFlow.tsx`
  - `frontend/src/features/billing/PaymentMethodSelector.tsx`
  - `frontend/src/features/billing/usePaymentInitiation.ts`
  - `frontend/src/features/billing/paymentApi.ts`
- **Acceptance:** User can select payment method, click "Pay Now", redirected to HyperPay/Moyasar hosted page

### T030: [P] Implement Payment Success/Failure Pages [US-9.4]
- **Description:** Create pages for payment completion confirmation
- **Files:**
  - `frontend/src/features/billing/PaymentSuccessPage.tsx`
  - `frontend/src/features/billing/PaymentFailurePage.tsx`
  - `frontend/src/features/billing/PaymentPendingPage.tsx`
- **Acceptance:** Shows payment status, redirects to dashboard on success, allows retry on failure

---

## Phase 7: Testing & Documentation

### T031: [P] Write Unit Tests for Billing Domain and Application
- **Description:** Comprehensive unit tests for all commands, handlers, domain logic
- **Files:**
  - `backend/src/Modules/Billing/Tests/Domain/SubscriptionTests.cs`
  - `backend/src/Modules/Billing/Tests/Domain/UsageMeterTests.cs`
  - `backend/src/Modules/Billing/Tests/Domain/ProratingCalculatorTests.cs`
  - `backend/src/Modules/Billing/Tests/Application/SubscriptionWorkflowTests.cs`
  - `backend/src/Modules/Billing/Tests/Application/UsageLimitTests.cs`
  - `backend/src/Modules/Billing/Tests/Application/PaymentProcessingTests.cs`
- **Acceptance:** > 85% code coverage, all scenarios tested

### T032: [P] Write Integration Tests for Billing APIs
- **Description:** Integration tests for subscriptions, payments, invoices
- **Files:**
  - `backend/src/Modules/Billing/Tests/Integration/SubscriptionUpgradeTests.cs`
  - `backend/src/Modules/Billing/Tests/Integration/PaymentWorkflowTests.cs`
  - `backend/src/Modules/Billing/Tests/Integration/InvoiceGenerationTests.cs`
  - `backend/src/Modules/Billing/Tests/Integration/MeteringTests.cs`
  - `backend/src/Modules/Billing/Tests/Integration/RefundTests.cs`
- **Acceptance:** All workflows end-to-end tested, payment provider calls mocked

### T033: [P] Write Frontend Component Tests
- **Description:** Unit and integration tests for React components
- **Files:**
  - `frontend/src/features/billing/__tests__/BillingDashboard.test.tsx`
  - `frontend/src/features/billing/__tests__/UsageMeterDisplay.test.tsx`
  - `frontend/src/features/billing/__tests__/PaymentInitiationFlow.test.tsx`
  - `frontend/src/features/billing/__tests__/PlanChangeModal.test.tsx`
- **Acceptance:** > 80% coverage, user interactions tested

### T034: Create Billing Module Documentation
- **Description:** API documentation, payment flow diagrams, integration guides
- **Files:**
  - `docs/modules/billing/README.md`
  - `docs/modules/billing/api.md`
  - `docs/modules/billing/payment-flow.md`
  - `docs/modules/billing/metering.md`
  - `docs/modules/billing/hyperpay-integration.md`
  - `docs/modules/billing/invoice-schema.md`
  - `docs/modules/billing/INTEGRATION_GUIDE.md`
- **Acceptance:** Complete documentation with examples

---

## Checkpoint 1: Subscription & Metering (After T018)
**Criteria:**
- Subscriptions can be assigned, upgraded, downgraded
- Suspension/reactivation working
- All usage meters tracking correctly
- Meter snapshots created, monthly reset working
- Limits enforced (soft at 80%, hard at 100%)
- All Phase 2-3 unit tests passing

## Checkpoint 2: Payment Processing (After T022)
**Criteria:**
- Payment initiation returns valid URLs
- Webhooks validated and processed
- Payment status polling working
- Refunds calculated and initiated
- PaymentTransaction state transitions correct

## Checkpoint 3: Invoicing & Dashboard (After T030)
**Criteria:**
- Invoices generated at month end
- PDFs created and stored in MinIO
- Invoice emails sent
- Billing dashboard loads and shows all metrics
- Payment flow UI complete and functional

## Checkpoint 4: Full Feature Complete (After T034)
**Criteria:**
- All 6 user stories implemented (US-9.1 through US-9.6)
- All tests passing with > 85% coverage
- Payment webhook security validated
- Dashboard loads < 3 seconds
- Documentation complete

---

## Dependencies Between Tasks

- **T001-T007** (Setup): Must complete before all other tasks
- **T008-T012**: Subscription management, proceed in parallel
- **T013-T018**: Usage metering, proceed in parallel, needed by T019-T030
- **T019-T022**: Payment processing, depend on T008-T018
- **T023-T026**: Invoicing, depend on T019-T022
- **T027-T030**: Frontend, depend on all backend components
- **T031-T034**: Testing/docs, depend on all implementations

---

## Success Metrics

- Subscription state changes enforced within 1 second
- Usage meters updated within 500ms of event
- Hard limits prevent operations 100% of time
- Payment initiation < 2 seconds
- Webhook signatures validated correctly
- Invoices generated within 1 hour of month end
- Invoice PDFs < 5 seconds generation time
- Dashboard loads < 3 seconds
- Meter accuracy within 1%

---

**Notes:**
- All billing operations audit-logged
- HyperPay integration primary, Moyasar fallback
- GCC (Saudi) compliance: SAR currency, 15% VAT
- Prorating algorithm: daily rate = plan_price / 30
- Retention: billing records retained 7+ years per PDPL
- SQLServer indexes on (tenantId, createdAt) and (subscriptionId, status)
- Redis keys: `billing:{tenantId}:meters:{month}` for real-time tracking
