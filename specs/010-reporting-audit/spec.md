# Epic 10: Reporting & Audit Module

## Overview
The Reporting & Audit Module provides comprehensive audit logging for all security-relevant and compliance-relevant actions across the platform, exposes tenant-specific analytics and usage dashboards, and enables compliance exports. All audit events are immutable and include correlation IDs for request tracing. Platform admins access operational dashboards, while tenant admins see only their own aggregated data. Real-time aggregates are published via SignalR for live dashboard updates.

## User Stories

### US-10.1: Audit Event Logging
**Priority:** P0 (Critical)
**Story Points:** 8

As the platform, I want to log all security and compliance-relevant actions so that we can audit tenant activity and demonstrate compliance with regulations.

**Acceptance Scenarios:**

**Scenario 1: Room Creation Audit Event**
```gherkin
Given a user creates a room
When the room is successfully created
Then an AuditEvent is created with:
  - event_type = "room.created"
  - actor_user_id = creator's user ID
  - resource_id = room ID
  - resource_type = "Room"
  - tenant_id = room's tenant ID
  - timestamp = creation timestamp (UTC)
  - correlation_id = unique request ID (from request header or generated)
  - action_details = {room_name, room_config, visibility}
  - ip_address = requester's IP
  - user_agent = requester's browser/app
  And the AuditEvent is persisted to database (immutable)
```

**Scenario 2: User Authentication Audit Event**
```gherkin
Given a user logs in via OTP
When authentication succeeds
Then an AuditEvent is created with:
  - event_type = "auth.login_success"
  - actor_user_id = user ID
  - resource_type = "User"
  - tenant_id = user's tenant ID (if applicable)
  - timestamp = login timestamp
  - correlation_id = login request ID
  - action_details = {auth_method: "otp", device_id (if available)}
  - ip_address, user_agent
```

**Scenario 3: File Download Audit Event**
```gherkin
Given a participant downloads a file
When the download completes successfully
Then an AuditEvent is created with:
  - event_type = "file.downloaded"
  - actor_user_id = downloader's user ID
  - resource_id = file artifact ID
  - resource_type = "FileArtifact"
  - tenant_id = file's tenant ID
  - timestamp = download timestamp
  - action_details = {file_name, file_size, download_reason (implicit)}
  - ip_address, user_agent
```

**Scenario 4: Payment Received Audit Event**
```gherkin
Given a payment is received and confirmed
When the PaymentTransaction status transitions to "Completed"
Then an AuditEvent is created with:
  - event_type = "payment.received"
  - actor_user_id = null (system-initiated)
  - resource_id = payment transaction ID
  - resource_type = "PaymentTransaction"
  - tenant_id = paying tenant ID
  - timestamp = payment confirmation timestamp
  - action_details = {amount, payment_method, invoice_id}
```

**Scenario 5: Admin User Impersonation Audit Event**
```gherkin
Given a platform admin impersonates a user
When the impersonation session begins
Then an AuditEvent is created with:
  - event_type = "admin.impersonation_start"
  - actor_user_id = admin's user ID
  - resource_id = impersonated user ID
  - resource_type = "ImpersonationSession"
  - tenant_id = target user's tenant ID
  - timestamp = impersonation start
  - action_details = {impersonation_reason, impersonation_justification, target_user_id, target_user_email}
  - ip_address = admin's IP
```

**Scenario 6: Subscription State Change Audit Event**
```gherkin
Given a subscription status changes
When the subscription transitions from Active to Suspended
Then an AuditEvent is created with:
  - event_type = "subscription.status_changed"
  - actor_user_id = null (system-initiated due to payment failure)
  - resource_id = subscription ID
  - resource_type = "Subscription"
  - tenant_id = subscription's tenant ID
  - timestamp = state change timestamp
  - action_details = {old_status: "Active", new_status: "Suspended", reason: "payment_failure"}
```

### US-10.2: Audit Export for Compliance
**Priority:** P1 (High)
**Story Points:** 5

As a tenant admin, I want to export audit logs in standard formats so that I can demonstrate compliance with regulations to auditors.

**Acceptance Scenarios:**

**Scenario 1: Export Audit Logs as CSV**
```gherkin
Given I am a tenant admin
When I navigate to Audit Settings
  And select date range (e.g., 2026-01-01 to 2026-03-31)
  And select event types to include (or all)
  And click "Export as CSV"
Then a background job is queued to export all AuditEvents for my tenant
  And an AuditExport record is created with status = "Pending"
  And the job processes audit events and generates CSV file
  And CSV file is stored in MinIO with tenant-scoped URL
  And I receive email with download link (valid for 7 days)
  And an audit event "audit_export_requested" is recorded
```

**Scenario 2: CSV Export Format**
```gherkin
Given an audit export CSV file is generated
When I open the file
Then each row contains:
  - timestamp (ISO 8601)
  - actor_user_id
  - event_type
  - resource_type
  - resource_id
  - action
  - ip_address
  - user_agent
  - correlation_id
  - metadata (JSON string)
And the file is sorted by timestamp ascending
```

**Scenario 3: Export Audit Logs as JSON**
```gherkin
Given I am a tenant admin
When I select JSON format for export
Then a JSON file is generated with array of audit events
  And each event includes all fields (timestamp, actor, event_type, resource, etc.)
  And the file is minified (no extra whitespace)
  And stored in MinIO and download link provided
```

**Scenario 4: Export Only Specific Event Types**
```gherkin
Given I am exporting audit logs
When I filter to include only: ["auth.login_success", "file.downloaded", "payment.received"]
Then the export includes only audit events with those event_types
  And other event types are excluded
  And the export count shows filtered total
```

**Scenario 5: Export Processing Status**
```gherkin
Given an export has been requested
When the background job is processing (for a large date range, e.g., 1 year)
Then the AuditExport record status = "Processing"
  And I can view progress (e.g., "Processed 50,000 of 150,000 events")
  And when complete, status transitions to "Completed"
  And download link is activated
```

### US-10.3: Tenant Analytics Dashboard
**Priority:** P1 (High)
**Story Points:** 8

As a tenant admin, I want to view analytics about my room usage, participants, recordings, and file sharing so that I can understand platform utilization.

**Acceptance Scenarios:**

**Scenario 1: Analytics Overview Dashboard**
```gherkin
Given I am a tenant admin
When I navigate to the Analytics dashboard
Then I see (for the current month and last 12 months):
  - Total rooms created
  - Total unique participants
  - Average room duration (minutes)
  - Total recording minutes
  - Total files uploaded
  - Total files downloaded
  - Average participants per room
  - Peak concurrent rooms
  - Total chat messages (if chat enabled)
  - Storage used (GB)
And all metrics are presented with charts and trends
```

**Scenario 2: Room Usage Analytics**
```gherkin
Given I am viewing the Room Usage section
When I view the dashboard
Then I see:
  - Daily room count (bar chart, last 30 days)
  - Weekly room count (line chart, last 12 weeks)
  - Monthly room count (bar chart, last 12 months)
  - Room duration distribution (histogram)
  - Participant count distribution (histogram)
  - Rooms by status (active, inactive, archived)
And I can filter by room visibility (public, private, unlisted)
```

**Scenario 3: Participant Analytics**
```gherkin
Given I am viewing Participant Analytics
When I view the dashboard
Then I see:
  - Total unique participants (all-time)
  - New participants per month (trend chart)
  - Participants by region (if IP geolocation available)
  - Repeat participants (users who joined multiple rooms)
  - Average session duration per participant
  - Participant growth rate
```

**Scenario 4: Recording Analytics**
```gherkin
Given I am viewing Recording Analytics
When I view the dashboard
Then I see:
  - Total recording minutes (all-time)
  - Recording minutes by month (trend chart)
  - Average recording duration per room
  - Rooms with recording enabled (count and %)
  - Storage used by recordings (GB)
  - Recording consumption trend
```

**Scenario 5: File Sharing Analytics**
```gherkin
Given I am viewing File Sharing Analytics
When I view the dashboard
Then I see:
  - Total files uploaded
  - Total files downloaded
  - Download rate (downloads / uploads)
  - Top 5 most downloaded files
  - Average file size
  - Files rejected by malware scan (count and %)
  - Storage used by files (GB)
  - File lifecycle: uploaded → ready → expired distribution
```

**Scenario 6: Chat Activity Analytics (if enabled)**
```gherkin
Given chat is enabled for rooms
When I view Chat Analytics
Then I see:
  - Total chat messages
  - Messages per day (trend chart)
  - Average message length
  - Rooms with chat enabled (count and %)
  - Moderation actions taken (messages deleted, users muted)
```

**Scenario 7: Date Range Filtering**
```gherkin
Given I am viewing any analytics dashboard
When I select a date range (e.g., "Last 90 Days" or custom)
Then all charts and metrics update to reflect only data from that range
  And trend lines are recalculated
  And comparison metrics (YoY, MoM) are updated
  And the page state is preserved on refresh (via URL params)
```

### US-10.4: Real-Time Usage Aggregates via SignalR
**Priority:** P2 (Medium)
**Story Points:** 5

As a tenant admin viewing the dashboard, I want to see live updates of current usage (concurrent rooms, active participants) via SignalR.

**Acceptance Scenarios:**

**Scenario 1: Real-Time Concurrent Rooms Count**
```gherkin
Given I am viewing a real-time dashboard
When a room transitions to "Active" status
Then a SignalR message is sent to tenant.{tenant_id}.report.snapshot
  And the message includes: concurrent_rooms_current = new count
  And my dashboard updates the "Concurrent Rooms" widget in real-time (no page refresh)
```

**Scenario 2: Real-Time Active Participants**
```gherkin
Given I am viewing a real-time dashboard
When a participant joins a room
Then a SignalR message is sent to tenant.{tenant_id}.report.snapshot
  And the message includes: active_participants_current = new count
  And my dashboard updates the "Active Participants" widget in real-time
```

**Scenario 3: Real-Time Event Stream**
```gherkin
Given I am viewing a real-time dashboard
When events occur (file uploaded, recording started, chat message sent)
Then SignalR messages are sent to tenant.{tenant_id}.report.snapshot
  And each message includes timestamp, event_type, event_count
  And my dashboard updates event counters in real-time
```

**Scenario 4: SignalR Connection Resilience**
```gherkin
Given a SignalR connection is established
When the connection drops (network outage)
Then the dashboard shows "Disconnected" status
  And automatic reconnection is attempted after 3 seconds
  And once reconnected, cached metrics are pulled from backend
  And real-time updates resume
```

### US-10.5: Operational Dashboard (Platform Admin)
**Priority:** P1 (High)
**Story Points:** 5

As a platform admin, I want to view operational metrics across all tenants so that I can monitor platform health and identify issues.

**Acceptance Scenarios:**

**Scenario 1: Platform-Wide Metrics Dashboard**
```gherkin
Given I am a platform admin
When I navigate to the Operational Dashboard
Then I see (live, updated every 10 seconds):
  - Total active rooms (all tenants)
  - Total active participants (all tenants)
  - Total tenants (active, suspended, cancelled)
  - Rooms created today, this week, this month
  - Files uploaded today, this week, this month
  - Payments received today, this week, this month (total SAR)
  - API request rate (requests per second)
  - Error rate (failed requests %)
  - Average API latency (p50, p95, p99)
  - MinIO storage used (total GB)
  - RabbitMQ queue depths (files.scan.queue, webhooks.deliver.queue, etc.)
```

**Scenario 2: Tenant Health Overview**
```gherkin
Given I am viewing the operational dashboard
When I click on "Tenant Health"
Then I see a list of all tenants with:
  - Tenant name and ID
  - Subscription status (Active, Suspended, Cancelled)
  - Current concurrent rooms / limit
  - Current storage used / limit
  - OTP sends this month / limit
  - Any warnings or errors (limit exceeded, payment overdue, etc.)
And I can sort by status, rooms, storage, etc.
```

**Scenario 3: Infrastructure Health**
```gherkin
Given I am viewing the operational dashboard
When I click on "Infrastructure"
Then I see:
  - LiveKit server status (connected, latency)
  - MinIO cluster status (healthy, disk usage)
  - RabbitMQ broker status (connected, queue depth)
  - Redis cluster status (connected, memory usage)
  - Database connection pool status (active connections, idle)
  - Worker process status (files.scan.worker running, webhooks.worker running, etc.)
```

**Scenario 4: Error Rate Monitoring**
```gherkin
Given I am viewing the operational dashboard
When errors occur (payment failures, scan failures, etc.)
Then error counts are displayed by error category
  And top 5 errors are shown with frequency
  And error rate trend chart shows spikes
  And I can filter errors by date range or type
```

## Functional Requirements

### Audit Event Logging
1. **Audit Event Schema**: Every audit event contains: event_id (UUID), event_type (string), actor_user_id (UUID, nullable), resource_id (UUID), resource_type (string), tenant_id (UUID), timestamp (datetime UTC), correlation_id (UUID or string), action_details (JSON), ip_address (string, nullable), user_agent (string, nullable), created_at (immutable).
2. **Immutable Audit Records**: Once persisted, AuditEvent records cannot be updated or deleted. Any corrections create new events. Audit trail is append-only.
3. **Correlation ID Propagation**: Every request includes a unique correlation_id (X-Correlation-ID header or generated). All audit events within a request share the same correlation_id for tracing.
4. **Comprehensive Event Coverage**: Log all security events (login, logout, permission changes), compliance events (data export, deletion), financial events (payment, refund, subscription change), operational events (room creation, recording start/stop), administrative events (admin impersonation, user removal).
5. **Event Type Enumeration**: Predefined event types: auth.*, room.*, file.*, payment.*, subscription.*, admin.*, recording.*, chat.*, audit.*. New event types require schema review.

### Audit Retention and Purging
6. **Retention Policy Enforcement**: Audit events retained for 7 years per Saudi PDPL regulations (configurable per tenant). Events older than retention period are purged via batch job (runs monthly).
7. **Soft Delete for Compliance**: Purged events are soft-deleted (marked is_purged = true) but remain in database for forensic purposes. Hard deletion (physical removal) only after 10 additional years.
8. **Retention Verification**: Monthly job verifies all tenants have retention policy set. Defaults to 7 years if not configured.

### Analytics and Aggregation
9. **Real-Time Aggregates**: UsageAggregate records updated in real-time (or near real-time via Redis cache) with: concurrent_rooms, active_participants, total_messages, total_files_transferred. Aggregates persisted to database at end of day.
10. **Historical Aggregates**: Daily snapshots stored as TenantAnalyticsSnapshot: snapshot_date, tenant_id, concurrent_rooms_peak, active_participants, total_messages, total_files, storage_used_bytes, recording_minutes.
11. **Analytics Aggregation Worker**: RabbitMQ consumer processes events from analytics.aggregate queue. Aggregates usage data into daily snapshots. Retries on failure with exponential backoff.

### Compliance and Data Privacy
12. **Cross-Tenant Data Isolation**: Tenant A can only view analytics for tenant A. Platform admins can view aggregated platform metrics but not per-tenant details (except for flagged issues).
13. **PDPL Compliance**: Audit logs include all required PDPL fields. Data retention complies with 7-year requirement. Export functionality enables proof of compliance.
14. **PII Minimization**: Audit events avoid storing sensitive data (passwords, API keys). action_details include only necessary context.

### Key Entities

**AuditEvent**
- `event_id` (UUID, PK)
- `event_type` (string, 100)
- `actor_user_id` (UUID, FK → User, nullable)
- `resource_id` (UUID, nullable)
- `resource_type` (string, 50, nullable)
- `tenant_id` (UUID, FK → Tenant)
- `timestamp` (datetime UTC, not nullable)
- `correlation_id` (string, 255)
- `action_details` (JSON, nullable)
- `ip_address` (string, 45, nullable)
- `user_agent` (string, 1000, nullable)
- `is_purged` (bool, default: false)
- `created_at_utc` (datetime, immutable)

**AuditExport**
- `export_id` (UUID, PK)
- `tenant_id` (UUID, FK → Tenant)
- `export_requested_by_user_id` (UUID, FK → User)
- `export_start_date` (datetime)
- `export_end_date` (datetime)
- `event_types_filter` (JSON array of strings, nullable for all)
- `status` (enum: Pending, Processing, Completed, Failed)
- `total_events_count` (int, nullable)
- `processed_events_count` (int, default: 0)
- `file_format` (enum: csv, json)
- `minio_export_url` (string, nullable)
- `error_reason` (string, nullable)
- `created_at_utc` (datetime)
- `updated_at_utc` (datetime)
- `completed_at_utc` (datetime, nullable)

**TenantAnalyticsSnapshot**
- `snapshot_id` (UUID, PK)
- `tenant_id` (UUID, FK → Tenant)
- `snapshot_date` (date)
- `concurrent_rooms_peak` (int)
- `concurrent_rooms_avg` (decimal)
- `active_participants_peak` (int)
- `total_rooms_created` (int)
- `total_files_uploaded` (int)
- `total_files_downloaded` (int)
- `total_chat_messages` (int)
- `total_recording_minutes` (int)
- `storage_used_bytes` (long)
- `created_at_utc` (datetime)

**UsageAggregate (Real-Time Cache)**
- `aggregate_id` (UUID, PK)
- `tenant_id` (UUID, FK → Tenant, unique per snapshot_period)
- `snapshot_period` (datetime, bucketed by minute or 5-minute interval)
- `concurrent_rooms_current` (int)
- `active_participants_current` (int)
- `messages_per_minute` (decimal)
- `files_per_minute` (decimal)
- `created_at_utc` (datetime)
- `updated_at_utc` (datetime)

## Success Criteria

- [ ] Audit events are persisted within 100ms of triggering action
- [ ] All security and compliance events are logged (100% coverage, validated by checklist)
- [ ] Audit export CSV generation completes within 2 minutes for 1 year of logs (50k+ events)
- [ ] Audit export JSON generation completes within 2 minutes for 1 year of logs
- [ ] Tenant cannot view another tenant's audit logs or analytics (100% isolation)
- [ ] Real-time dashboard updates via SignalR within 1 second of usage change
- [ ] Platform admin operational dashboard loads in < 3 seconds with live data
- [ ] Audit retention policies are enforced consistently (monthly purge job runs successfully)
- [ ] Analytics snapshots are accurate to within 1% of true counts
- [ ] Correlation ID tracing works for 100% of requests (no orphaned events)
- [ ] No audit event data is lost due to database failures (persisted before response)

## Edge Cases

1. **Concurrent Audit Events from Same User**: User logs in twice simultaneously (different tabs). Two separate login AuditEvents created with same actor_user_id but different event_ids and timestamps.

2. **Audit Event for Failed Operation**: File download fails due to missing file. AuditEvent still created with success = false, error_reason = "File not found".

3. **Export Initiated During Audit Purge**: Export requested for 2025 data. Monthly purge job runs during export. Export job locks audit_events table. Purge waits. No data loss.

4. **Large Analytics Export (Multiple Years)**: Export requested for all events from 2020-2026 (50k+ events). Background job processes in batches of 5000. Status updates as "Processing 15000 of 50000". Completes in < 5 minutes.

5. **Real-Time Aggregate Bucket Transition**: At exactly 00:00:00 UTC, a usage bucket transitions from current to historical. New aggregate bucket is created. No data loss. Previous bucket is closed.

6. **Correlation ID Missing**: Request arrives without X-Correlation-ID header. System generates UUID as correlation_id. All events in request share generated ID.

7. **Analytics Snapshot Duplicate Request**: End-of-day snapshot job runs twice (clock adjustment). Duplicate snapshot attempt is skipped due to unique constraint on (tenant_id, snapshot_date).

8. **Audit Event for Deleted Tenant**: Tenant is deleted (soft delete). Audit events for that tenant remain queryable (tenant_id still valid). Analytics snapshots are not generated for deleted tenants.

9. **SignalR Subscriber Count Exceeds Capacity**: 10,000 concurrent admin dashboards connected to tenant.{tenant_id}.report.snapshot. Hub manages broadcast efficiently. No missed messages. Consider connection limits in architecture.

10. **Retention Policy Changed Mid-Year**: January: retention = 7 years. July: retention changed to 2 years. Policy change affects future purges; past purges not retroactively applied.

## Assumptions

1. **Audit Events are Fire-and-Forget**: Failed audit writes do not block the triggering operation. Audit is logged best-effort asynchronously.

2. **Database Consistency is Strong**: Assumption that database primary is always available. Replicas may lag. Audit writes go to primary.

3. **Correlation IDs are Globally Unique**: UUIDs generated for correlation IDs do not collide. No custom correlation ID format validation in v1.

4. **Analytics Data Accuracy is Best-Effort**: Aggregates may lag real-time by a few seconds to minutes. Eventual consistency is acceptable.

5. **PDPL Compliance is Tenant Responsibility**: Platform provides tools (audit export, retention enforcement). Tenant is responsible for demonstrating compliance to regulators.

6. **SignalR Hub is Always Available**: Assume SignalR hub has 99.9% uptime. No failover configuration in v1.

7. **Event Type Enumeration is Stable**: New event types added via schema updates. No breaking changes to existing event_type values.

8. **Date Bucket Accuracy**: Assumption that server time is synchronized via NTP. No clock skew compensation needed.

9. **Tenant Deletion Soft-Deletes Audit Events**: Audit events for deleted tenants are not purged. Tenant admin cannot view deleted tenant's audit logs (access control).

10. **Export Job Resource Constraints**: Export job does not block normal database queries. Separate connection pool or scheduling ensures isolation.

## Dependencies

- **Epic 2 (Tenancy)**: Audit events and analytics tied to Tenant. Tenant admin role determines audit export access.
- **Epic 1 (Identity)**: User authentication events logged. Admin impersonation tracked via User and ImpersonationSession entities.
- **Epic 3 (Rooms)**: Room events (creation, activation, deactivation) drive concurrent room metering and room usage analytics.
- **Epic 8 (Files & Artifacts)**: File upload/download events drive file sharing analytics and audit trails.
- **RabbitMQ**: Analytics aggregation workers consume events from analytics.aggregate queue.
- **Redis**: Optional caching for real-time aggregates to reduce database load.
- **SignalR**: Real-time broadcasting of usage snapshots to connected admin dashboards.

---

**Document Version:** 1.0
**Last Updated:** 2026-04-03
**Module Owner:** Compliance & Ops Team
**Status:** Ready for Implementation
