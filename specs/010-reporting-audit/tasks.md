# Epic 10: Reporting & Audit Module - Task Breakdown

**Document Version:** 1.0
**Last Updated:** 2026-04-03
**Module:** Reporting
**Dependencies:** Epic 1 (Identity), Epic 2 (Tenancy), Epic 3 (Rooms), Epic 8 (Files)

---

## Phase 1: Setup & Infrastructure

### T001: Create Reporting Module Structure
- **Description:** Initialize Reporting module directory structure and base configuration
- **Files to Create:**
  - `backend/src/Modules/Reporting/Domain/` (entities, value objects)
  - `backend/src/Modules/Reporting/Application/` (commands, queries, handlers)
  - `backend/src/Modules/Reporting/Infrastructure/` (persistence, analytics, export)
  - `backend/src/Modules/Reporting/Api/` (controllers, DTOs)
- **Acceptance:** Module scaffold created with proper folder structure

### T002: Define Reporting Domain Models
- **Description:** Create core domain entities: AuditEvent, AuditExport, TenantAnalyticsSnapshot, UsageAggregate
- **Files:**
  - `backend/src/Modules/Reporting/Domain/AuditEvent.cs`
  - `backend/src/Modules/Reporting/Domain/AuditExport.cs`
  - `backend/src/Modules/Reporting/Domain/TenantAnalyticsSnapshot.cs`
  - `backend/src/Modules/Reporting/Domain/UsageAggregate.cs`
  - `backend/src/Modules/Reporting/Domain/Enums/` (AuditExportStatus, EventType, etc.)
- **Acceptance:** All entities with immutability guarantees, proper validation

### T003: Create Database Schema and Migrations
- **Description:** Create SQL Server schema for Reporting module
- **Files:**
  - `backend/src/Modules/Reporting/Infrastructure/Persistence/ReportingDbContext.cs`
  - `backend/src/Modules/Reporting/Infrastructure/Persistence/Migrations/` (initial schema)
- **Acceptance:** Schema with proper indexes on (tenantId, timestamp), (eventType), partitioning strategy for large datasets

### T004: Implement Audit Event Logging Infrastructure
- **Description:** Create base service for recording all audit events across platform
- **Files:**
  - `backend/src/Modules/Reporting/Infrastructure/AuditEventLogger.cs`
  - `backend/src/SharedKernel/Middleware/CorrelationIdMiddleware.cs`
  - `backend/src/SharedKernel/Extensions/AuditEventExtensions.cs`
- **Acceptance:** Middleware captures correlation IDs, AuditEventLogger persists events < 100ms

### T005: Implement Real-Time Analytics Aggregator
- **Description:** Create service to aggregate usage data in real-time and cache in Redis
- **Files:**
  - `backend/src/Modules/Reporting/Infrastructure/RealTimeAnalyticsAggregator.cs`
  - `backend/src/Modules/Reporting/Infrastructure/RedisAnalyticsCache.cs`
- **Acceptance:** Usage aggregates updated in Redis with concurrent room/participant counts

### T006: Implement SignalR Hub for Real-Time Updates
- **Description:** Setup SignalR hub for broadcasting analytics updates to connected dashboards
- **Files:**
  - `backend/src/Modules/Reporting/Infrastructure/AnalyticsHub.cs`
  - `backend/src/Modules/Reporting/Api/AnalyticsSignalRHub.cs`
- **Acceptance:** Hub initialized, methods for broadcasting to tenant-specific channels

---

## Phase 2: Audit Event Logging

### T007: [P] Implement Comprehensive Audit Event Logging [US-10.1]
- **Description:** Create handlers to log all security/compliance/financial events across platform
- **Files:**
  - `backend/src/Modules/Reporting/Application/Handlers/RoomCreatedAuditHandler.cs`
  - `backend/src/Modules/Reporting/Application/Handlers/UserAuthAuditHandler.cs`
  - `backend/src/Modules/Reporting/Application/Handlers/FileDownloadAuditHandler.cs`
  - `backend/src/Modules/Reporting/Application/Handlers/PaymentAuditHandler.cs`
  - `backend/src/Modules/Reporting/Application/Handlers/SubscriptionStateAuditHandler.cs`
  - `backend/src/Modules/Reporting/Application/Handlers/AdminActionAuditHandler.cs`
  - `backend/src/Modules/Reporting/Application/Handlers/AdminImpersonationAuditHandler.cs`
  - `backend/src/Modules/Reporting/Infrastructure/DomainEventAuditBus.cs`
- **Acceptance:** All event types logged with required fields (actor, resource, timestamp, correlation_id, IP, user_agent)

### T008: [P] Implement Audit Event Immutability Enforcement
- **Description:** Ensure audit events cannot be modified or deleted (append-only)
- **Files:**
  - `backend/src/Modules/Reporting/Domain/AuditEventImmutability.cs`
  - `backend/src/Modules/Reporting/Infrastructure/AuditEventShadowingInterceptor.cs`
- **Acceptance:** Attempts to update/delete AuditEvent records fail with exception, audit trail intact

### T009: [P] Implement Correlation ID Propagation [US-10.1]
- **Description:** Ensure all audit events within request share same correlation_id
- **Files:**
  - `backend/src/SharedKernel/Middleware/CorrelationIdMiddleware.cs`
  - `backend/src/SharedKernel/Services/CorrelationIdService.cs`
- **Acceptance:** Correlation ID extracted from header or generated, propagated to all audit events in request

### T010: [P] Implement Retention Policy Enforcement [US-10.1]
- **Description:** Create job to purge audit events older than retention period
- **Files:**
  - `backend/src/Modules/Reporting/Infrastructure/Jobs/AuditRetentionPurgeJob.cs`
  - `backend/src/Modules/Reporting/Application/Commands/PurgeOldAuditEventsCommand.cs`
  - `backend/src/Modules/Reporting/Application/Handlers/PurgeOldAuditEventsHandler.cs`
- **Acceptance:** Job runs monthly, soft-deletes events > retentionDays (default 7 years), audit trail preserved

---

## Phase 3: Audit Export

### T011: [P] Implement Audit Export Request API [US-10.2]
- **Description:** Create endpoint for tenant admins to request audit log exports
- **Files:**
  - `backend/src/Modules/Reporting/Api/AuditExportController.cs`
  - `backend/src/Modules/Reporting/Application/Commands/RequestAuditExportCommand.cs`
  - `backend/src/Modules/Reporting/Application/Handlers/RequestAuditExportHandler.cs`
- **Acceptance:** Endpoint validates date range and event type filters, creates AuditExport record with "Pending" status

### T012: [P] Implement CSV Export Generation [US-10.2]
- **Description:** Create background job to generate CSV files from audit events
- **Files:**
  - `backend/src/Modules/Reporting/Infrastructure/Jobs/AuditExportCsvJob.cs`
  - `backend/src/Modules/Reporting/Infrastructure/AuditExportCsvGenerator.cs`
- **Acceptance:** Job processes events in batches, generates CSV with all required fields, stores in MinIO, status updated

### T013: [P] Implement JSON Export Generation [US-10.2]
- **Description:** Create background job to generate JSON files from audit events
- **Files:**
  - `backend/src/Modules/Reporting/Infrastructure/Jobs/AuditExportJsonJob.cs`
  - `backend/src/Modules/Reporting/Infrastructure/AuditExportJsonGenerator.cs`
- **Acceptance:** Job generates minified JSON array, stores in MinIO, status updated

### T014: [P] Implement Export Status Tracking and Notification [US-10.2]
- **Description:** Track export progress and notify tenant when complete
- **Files:**
  - `backend/src/Modules/Reporting/Application/Commands/UpdateExportStatusCommand.cs`
  - `backend/src/Modules/Reporting/Infrastructure/ExportEmailService.cs`
  - `backend/src/Modules/Reporting/Application/Events/ExportCompletedEvent.cs`
  - `backend/src/Modules/Reporting/Application/Handlers/ExportCompletedEventHandler.cs`
- **Acceptance:** Status transitions (Pending → Processing → Completed), tenant notified via email with download link

### T015: [P] Implement Export Download API [US-10.2]
- **Description:** Create endpoint to download generated export files
- **Files:**
  - `backend/src/Modules/Reporting/Api/AuditExportDownloadController.cs`
  - `backend/src/Modules/Reporting/Application/Queries/GetExportDownloadUrlQuery.cs`
  - `backend/src/Modules/Reporting/Application/Handlers/GetExportDownloadUrlQueryHandler.cs`
- **Acceptance:** Pre-signed URL generated, valid for 7 days, authorization checked

---

## Phase 4: Tenant Analytics Dashboard

### T016: [P] Implement Analytics Data Aggregation Job [US-10.3]
- **Description:** Create daily job to aggregate analytics snapshots from events
- **Files:**
  - `backend/src/Modules/Reporting/Infrastructure/Jobs/AnalyticsAggregationJob.cs`
  - `backend/src/Modules/Reporting/Application/Commands/CreateAnalyticsSnapshotCommand.cs`
  - `backend/src/Modules/Reporting/Application/Handlers/CreateAnalyticsSnapshotHandler.cs`
  - `backend/src/Modules/Reporting/Infrastructure/AnalyticsCalculationService.cs`
- **Acceptance:** Job runs daily, calculates all metrics from audit events, creates TenantAnalyticsSnapshot

### T017: [P] Implement Analytics Query API - Overview [US-10.3]
- **Description:** Create API endpoints for analytics dashboard data
- **Files:**
  - `backend/src/Modules/Reporting/Api/AnalyticsDashboardController.cs`
  - `backend/src/Modules/Reporting/Application/Queries/GetAnalyticsOverviewQuery.cs`
  - `backend/src/Modules/Reporting/Application/Handlers/GetAnalyticsOverviewQueryHandler.cs`
- **Acceptance:** Returns current month and last 12 months data with charts, all metrics included

### T018: [P] Implement Analytics Query API - Room Usage [US-10.3]
- **Description:** Create endpoint for room usage analytics
- **Files:**
  - `backend/src/Modules/Reporting/Application/Queries/GetRoomUsageAnalyticsQuery.cs`
  - `backend/src/Modules/Reporting/Application/Handlers/GetRoomUsageAnalyticsQueryHandler.cs`
- **Acceptance:** Returns daily/weekly/monthly room counts, duration distribution, status breakdown

### T019: [P] Implement Analytics Query API - Participant Analytics [US-10.3]
- **Description:** Create endpoint for participant analytics
- **Files:**
  - `backend/src/Modules/Reporting/Application/Queries/GetParticipantAnalyticsQuery.cs`
  - `backend/src/Modules/Reporting/Application/Handlers/GetParticipantAnalyticsQueryHandler.cs`
- **Acceptance:** Returns unique participants, new per month, repeat participants, session duration

### T020: [P] Implement Analytics Query API - Recording Analytics [US-10.3]
- **Description:** Create endpoint for recording analytics
- **Files:**
  - `backend/src/Modules/Reporting/Application/Queries/GetRecordingAnalyticsQuery.cs`
  - `backend/src/Modules/Reporting/Application/Handlers/GetRecordingAnalyticsQueryHandler.cs`
- **Acceptance:** Returns recording minutes (all-time and by month), storage used, rooms with recording enabled %

### T021: [P] Implement Analytics Query API - File Sharing [US-10.3]
- **Description:** Create endpoint for file sharing analytics
- **Files:**
  - `backend/src/Modules/Reporting/Application/Queries/GetFileAnalyticsQuery.cs`
  - `backend/src/Modules/Reporting/Application/Handlers/GetFileAnalyticsQueryHandler.cs`
- **Acceptance:** Returns files uploaded/downloaded, download rate, top files, storage used, malware rejection %

### T022: [P] Implement Analytics Date Range Filtering [US-10.3]
- **Description:** Add date range parameter to all analytics queries
- **Files:**
  - `backend/src/Modules/Reporting/Domain/Services/DateRangeAnalyticsService.cs`
  - `backend/src/Modules/Reporting/Application/Queries/BaseAnalyticsQuery.cs`
- **Acceptance:** All queries support date_from, date_to parameters, results filtered accurately

---

## Phase 5: Real-Time Aggregates & SignalR

### T023: [P] Implement Real-Time Usage Aggregate Updates [US-10.4]
- **Description:** Publish usage changes to Redis and SignalR for live dashboard
- **Files:**
  - `backend/src/Modules/Reporting/Application/Events/RoomActivatedEvent.cs`
  - `backend/src/Modules/Reporting/Application/Handlers/RoomActivatedEventHandler.cs`
  - `backend/src/Modules/Reporting/Application/Events/ParticipantJoinedEvent.cs`
  - `backend/src/Modules/Reporting/Application/Handlers/ParticipantJoinedEventHandler.cs`
- **Acceptance:** Events trigger RedisAnalyticsCache updates, SignalR broadcasts to connected dashboards

### T024: [P] Implement SignalR Connection Resilience [US-10.4]
- **Description:** Handle SignalR disconnections and reconnections gracefully
- **Files:**
  - `frontend/src/features/reporting/signalRConnection.ts`
  - `frontend/src/features/reporting/useSignalRConnection.ts`
- **Acceptance:** Dashboard reconnects automatically after 3 seconds, cached metrics pulled on reconnect

---

## Phase 6: Platform Admin Operational Dashboard

### T025: [P] Implement Platform Admin Dashboard Backend [US-10.5]
- **Description:** Create API for platform-wide operational metrics
- **Files:**
  - `backend/src/Modules/Reporting/Api/OperationalDashboardController.cs`
  - `backend/src/Modules/Reporting/Application/Queries/GetPlatformMetricsQuery.cs`
  - `backend/src/Modules/Reporting/Application/Handlers/GetPlatformMetricsQueryHandler.cs`
  - `backend/src/Modules/Reporting/Infrastructure/PlatformMetricsAggregator.cs`
- **Acceptance:** Returns all tenants' metrics aggregated (active rooms, participants, payments, errors, API latency)

### T026: [P] Implement Tenant Health Overview API [US-10.5]
- **Description:** Create endpoint showing health status of all tenants
- **Files:**
  - `backend/src/Modules/Reporting/Application/Queries/GetTenantHealthOverviewQuery.cs`
  - `backend/src/Modules/Reporting/Application/Handlers/GetTenantHealthOverviewQueryHandler.cs`
  - `backend/src/Modules/Reporting/Infrastructure/TenantHealthCalculator.cs`
- **Acceptance:** Returns tenant list with status, rooms/limits, storage/limits, warnings/errors

### T027: [P] Implement Infrastructure Health API [US-10.5]
- **Description:** Create endpoint showing infrastructure component health (LiveKit, MinIO, RabbitMQ, Redis, DB)
- **Files:**
  - `backend/src/Modules/Reporting/Application/Queries/GetInfrastructureHealthQuery.cs`
  - `backend/src/Modules/Reporting/Application/Handlers/GetInfrastructureHealthQueryHandler.cs`
  - `backend/src/Modules/Reporting/Infrastructure/HealthChecks/` (LiveKit, MinIO, RabbitMQ, Redis, Database)
- **Acceptance:** Health check results for all components, latency measurements

### T028: [P] Implement Error Rate Monitoring API [US-10.5]
- **Description:** Create endpoint for error tracking and analysis
- **Files:**
  - `backend/src/Modules/Reporting/Application/Queries/GetErrorRateQuery.cs`
  - `backend/src/Modules/Reporting/Application/Handlers/GetErrorRateQueryHandler.cs`
  - `backend/src/Modules/Reporting/Infrastructure/ErrorRateCalculator.cs`
- **Acceptance:** Returns error counts by category, top errors, error rate trend

---

## Phase 7: Frontend - Analytics & Dashboards

### T029: [P] Implement Tenant Analytics Dashboard UI [US-10.3]
- **Description:** Create React components for tenant analytics dashboard
- **Files:**
  - `frontend/src/features/reporting/AnalyticsDashboard.tsx`
  - `frontend/src/features/reporting/AnalyticsOverviewCards.tsx`
  - `frontend/src/features/reporting/RoomUsageChart.tsx`
  - `frontend/src/features/reporting/ParticipantAnalyticsChart.tsx`
  - `frontend/src/features/reporting/RecordingMetricsChart.tsx`
  - `frontend/src/features/reporting/FileAnalyticsChart.tsx`
  - `frontend/src/features/reporting/DateRangeSelector.tsx`
  - `frontend/src/features/reporting/useAnalyticsDashboard.ts`
- **Acceptance:** Dashboard displays all metrics with charts, date range filtering works, loads < 3 seconds

### T030: [P] Implement Real-Time Dashboard Updates [US-10.4]
- **Description:** Add SignalR integration for live metric updates
- **Files:**
  - `frontend/src/features/reporting/LiveMetricsWidget.tsx`
  - `frontend/src/features/reporting/useLiveMetrics.ts`
- **Acceptance:** Concurrent rooms and participants update in real-time, disconnection handled gracefully

### T031: [P] Implement Audit Export UI [US-10.2]
- **Description:** Create UI for requesting and downloading audit exports
- **Files:**
  - `frontend/src/features/reporting/AuditExportModal.tsx`
  - `frontend/src/features/reporting/DateRangeAndFilterSelector.tsx`
  - `frontend/src/features/reporting/ExportFormatSelector.tsx`
  - `frontend/src/features/reporting/ExportProgressTracker.tsx`
  - `frontend/src/features/reporting/useAuditExport.ts`
- **Acceptance:** Modal to select date range and filters, request export, track progress, download when ready

### T032: [P] Implement Platform Admin Operational Dashboard UI [US-10.5]
- **Description:** Create admin console dashboard for platform metrics
- **Files:**
  - `frontend/src/features/admin/OperationalDashboard.tsx`
  - `frontend/src/features/admin/PlatformMetricsWidgets.tsx`
  - `frontend/src/features/admin/TenantHealthTable.tsx`
  - `frontend/src/features/admin/InfrastructureHealthPanel.tsx`
  - `frontend/src/features/admin/ErrorRateChart.tsx`
  - `frontend/src/features/admin/useOperationalDashboard.ts`
- **Acceptance:** Dashboard shows all metrics updated every 10 seconds via SignalR, loads < 3 seconds

---

## Phase 8: Testing & Documentation

### T033: [P] Write Unit Tests for Reporting Domain and Application
- **Description:** Comprehensive unit tests for audit events, analytics, queries
- **Files:**
  - `backend/src/Modules/Reporting/Tests/Domain/AuditEventTests.cs`
  - `backend/src/Modules/Reporting/Tests/Application/AuditLoggingTests.cs`
  - `backend/src/Modules/Reporting/Tests/Application/AnalyticsCalculationTests.cs`
  - `backend/src/Modules/Reporting/Tests/Application/ExportGenerationTests.cs`
  - `backend/src/Modules/Reporting/Tests/Application/RetentionPolicyTests.cs`
- **Acceptance:** > 85% code coverage, all scenarios tested

### T034: [P] Write Integration Tests for Reporting APIs
- **Description:** Integration tests for audit logging, exports, analytics
- **Files:**
  - `backend/src/Modules/Reporting/Tests/Integration/AuditLoggingWorkflowTests.cs`
  - `backend/src/Modules/Reporting/Tests/Integration/ExportWorkflowTests.cs`
  - `backend/src/Modules/Reporting/Tests/Integration/AnalyticsQueryTests.cs`
  - `backend/src/Modules/Reporting/Tests/Integration/SignalRBroadcastTests.cs`
- **Acceptance:** Full workflows tested end-to-end, all queries validated

### T035: [P] Write Frontend Component Tests
- **Description:** Unit and integration tests for React components
- **Files:**
  - `frontend/src/features/reporting/__tests__/AnalyticsDashboard.test.tsx`
  - `frontend/src/features/reporting/__tests__/AuditExportModal.test.tsx`
  - `frontend/src/features/reporting/__tests__/LiveMetricsWidget.test.tsx`
  - `frontend/src/features/admin/__tests__/OperationalDashboard.test.tsx`
- **Acceptance:** > 80% coverage, user interactions tested

### T036: Create Reporting Module Documentation
- **Description:** API documentation, data dictionary, integration guides
- **Files:**
  - `docs/modules/reporting/README.md`
  - `docs/modules/reporting/audit-events.md`
  - `docs/modules/reporting/analytics-schema.md`
  - `docs/modules/reporting/export-formats.md`
  - `docs/modules/reporting/retention-policy.md`
  - `docs/modules/reporting/INTEGRATION_GUIDE.md`
- **Acceptance:** Complete documentation with examples

---

## Checkpoint 1: Audit Events (After T010)
**Criteria:**
- All event types logged with required fields
- Correlation IDs propagating correctly
- Immutability enforced
- Retention policy working
- All Phase 2 tests passing

## Checkpoint 2: Export & Analytics (After T022)
**Criteria:**
- CSV/JSON exports generating correctly
- Analytics snapshots created daily
- All analytics queries working
- Date range filtering accurate
- All Phase 3-4 tests passing

## Checkpoint 3: Real-Time & Admin Dashboard (After T028)
**Criteria:**
- SignalR broadcasting usage changes
- Real-time metric updates working
- Platform admin dashboard loading
- Infrastructure health checks passing
- Admin operational dashboard complete

## Checkpoint 4: Full Feature Complete (After T036)
**Criteria:**
- All 5 user stories implemented (US-10.1 through US-10.5)
- Tenant analytics dashboard fully functional
- Audit export workflow working
- Platform admin dashboard live
- All tests passing > 85% coverage
- Documentation complete

---

## Dependencies Between Tasks

- **T001-T006** (Setup): Must complete before all other tasks
- **T007-T010**: Audit logging foundation, needed by T011-T022
- **T011-T015**: Audit export, can proceed in parallel
- **T016-T022**: Analytics & queries, parallel tasks
- **T023-T024**: Real-time updates, depend on T016-T022
- **T025-T028**: Admin dashboard, depend on T023-T024
- **T029-T032**: Frontend, depend on T016-T028
- **T033-T036**: Testing/docs, depend on all implementations

---

## Success Metrics

- Audit events persisted within 100ms
- 100% audit coverage for all security/compliance events
- Audit export CSV generated in < 2 minutes for 1 year
- Tenant cannot view other tenant's data (100% isolation)
- Real-time updates via SignalR < 1 second delay
- Platform admin dashboard loads < 3 seconds
- Audit retention policies enforced monthly
- Analytics snapshots accurate within 1%
- Correlation ID tracing 100% coverage

---

**Notes:**
- All audit events are immutable once created
- Database partitioning strategy: by month on audit_event table
- Redis caching: Usage aggregates for near real-time queries
- SQLServer indexes: (tenantId, timestamp), (eventType), (actorUserId)
- PDPL 7-year retention for all audit events
- Analytics snapshots: daily aggregation, historical tracking
- Export files: stored in MinIO, valid for 7 days
- SignalR hubs: tenant-scoped channels (tenant.{id}.report.snapshot)
