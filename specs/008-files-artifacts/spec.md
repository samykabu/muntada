# Epic 8: Files & Artifacts Module

## Overview
The Files & Artifacts Module provides moderators with the ability to upload, distribute, and manage files within moderated audio rooms. Files are scanned for malware before being made available for download, support targeted delivery to specific participants, and include comprehensive audit trails for all downloads.

## User Stories

### US-8.1: Moderator Uploads File to Room
**Priority:** P0 (Critical)
**Story Points:** 8

As a room moderator, I want to upload a file to the room so that participants can access relevant materials during the session.

**Acceptance Scenarios:**

**Scenario 1: Upload with Presigned URL (Happy Path)**
```gherkin
Given I am a room moderator in an active room
  And I have a file (PDF, 2MB) ready to upload
When I click the upload button in the room interface
  And select my file from the file picker
  And confirm the upload action
Then a presigned URL is returned for direct upload to MinIO
  And the file upload begins via the presigned URL
  And the FileArtifact record is created with status "Uploaded"
  And the file metadata (name, size, MIME type, uploader, timestamp) is persisted
  And the file transitions to "Scanning" status
  And a scan request is published to RabbitMQ with correlation ID
```

**Scenario 2: Targeted Delivery - Specific Recipients**
```gherkin
Given a file is being uploaded
When I select delivery recipients as "Specific Participants"
  And I select 3 participant names from the participant list
Then the file is marked with recipient list ["participant1_id", "participant2_id", "participant3_id"]
  And a FileScanResult record is created with target_participant_ids populated
  And only selected participants can download the file once Ready
```

**Scenario 3: Targeted Delivery - All Participants**
```gherkin
Given a file is being uploaded
When I select delivery recipients as "All Participants"
Then the file is marked with recipient_type = "ALL_PARTICIPANTS"
  And all room participants (excluding guest listeners) can download once Ready
```

**Scenario 4: Targeted Delivery - Moderator Only**
```gherkin
Given a file is being uploaded
When I select delivery recipients as "Moderator Only"
Then the file is marked with recipient_type = "MODERATOR_ONLY"
  And only the uploading moderator can download the file
  And no participants see the file in their available downloads
```

**Scenario 5: Upload During Inactive Room**
```gherkin
Given the room is inactive (not in active status)
When I attempt to upload a file
Then the upload is rejected with error "Room is not active"
  And no FileArtifact is created
```

### US-8.2: Malware Scan Workflow
**Priority:** P0 (Critical)
**Story Points:** 5

As a platform, I want to scan all uploaded files for malware before making them available to participants so that we maintain a secure environment.

**Acceptance Scenarios:**

**Scenario 1: Scan Worker Processes Request**
```gherkin
Given a file has been uploaded and is in "Scanning" status
  And a scan request is in the RabbitMQ queue with file_artifact_id
When the scan worker picks up the message
  And runs the file through the malware scanning service
Then the FileScanResult record is updated with scan results
  And if no threats detected: file transitions to "Ready" status
  And if threats detected: file transitions to "Rejected" status with rejection_reason
  And a FileScanCompleted audit event is produced
```

**Scenario 2: Scan Timeout**
```gherkin
Given a file is in "Scanning" status
  And the scan has been processing for > 30 minutes
When the scan timeout is triggered
Then the file transitions to "Scanning" status with timeout flag
  And a retry message is published to RabbitMQ (max 3 retries)
  And if all retries exhausted: file transitions to "Rejected" with reason "Scan timeout"
```

**Scenario 3: Scan Result Persistence**
```gherkin
Given a scan completes with threat_found = true
When the FileScanResult is persisted
Then scan_timestamp is recorded
  And threat_description is stored
  And scan_metadata (scanner version, signature version) is included
  And the result is immutable (audit trail on any updates)
```

### US-8.3: Download File (Status-Gated)
**Priority:** P0 (Critical)
**Story Points:** 5

As a participant, I want to download a file that was shared by the moderator, and I can only download files that have been scanned and approved.

**Acceptance Scenarios:**

**Scenario 1: Download Ready File**
```gherkin
Given a file is in "Ready" status
  And I am a participant in the file's recipient list
When I click the download button
Then the file is downloaded directly from MinIO
  And a FileDownloadAudit event is created with timestamp, participant_id, file_id, tenant_id
  And the audit event is persisted to enable compliance reporting
  And the download count on the FileArtifact increments
```

**Scenario 2: Cannot Download Non-Ready Files**
```gherkin
Given a file is in "Scanning", "Rejected", "Expired", or "Deleted" status
When I attempt to download the file
Then the download is blocked
  And an error message is displayed: "File is not available for download"
  And an attempted_download audit event is recorded (for security)
```

**Scenario 3: Participant Not in Recipient List**
```gherkin
Given a file is in "Ready" status
  And my participant ID is NOT in the file's recipient list
When I attempt to download
Then the download is blocked
  And error "You do not have permission to download this file"
  And an unauthorized_download attempt audit event is recorded
```

**Scenario 4: Guest Listener Cannot Download**
```gherkin
Given a file is shared in the room
  And I am a guest listener (not a participant)
When I attempt to download the file
Then the download is blocked
  And error "Guest listeners cannot download files"
  And a security audit event is recorded
```

### US-8.4: File Expiry Management
**Priority:** P1 (High)
**Story Points:** 3

As a platform admin, I want files to automatically expire based on tenant-configured retention policies so that storage is managed and data retention complies with regulations.

**Acceptance Scenarios:**

**Scenario 1: File Auto-Expires After Configured Period**
```gherkin
Given a file is in "Ready" status
  And the tenant's file_retention_days is 30
  And the file was uploaded 30 days ago
When the expiry check job runs (daily)
Then the file transitions to "Expired" status
  And expiry_timestamp is recorded
  And participants cannot download the file (treated as non-Ready)
  And an audit event "file_expired" is recorded
```

**Scenario 2: Expiry Configuration Per Tenant**
```gherkin
Given I am a tenant admin
When I configure file_retention_days = 7
  And a file is uploaded
Then the file will automatically expire 7 days after upload
  And existing files follow the new policy for expiry calculation
```

**Scenario 3: Expired File Cannot Be Downloaded**
```gherkin
Given a file is in "Expired" status
When a participant attempts to download
Then the download is blocked
  And error "File has expired and is no longer available"
  And no audit event is recorded (not a security incident, expected behavior)
```

### US-8.5: File Deletion and Cleanup
**Priority:** P1 (High)
**Story Points:** 3

As a room moderator, I want to delete a file from a room so that I can remove files that are no longer relevant or were uploaded by mistake.

**Acceptance Scenarios:**

**Scenario 1: Moderator Deletes File**
```gherkin
Given I am a room moderator
  And a file is in the room (any status except Deleted)
When I click the delete button on the file
Then the file transitions to "Deleted" status
  And the deleted_timestamp and deleted_by_user_id are recorded
  And participants cannot download the file
  And a file_deleted audit event is created
```

**Scenario 2: Deleted File is Soft Delete**
```gherkin
Given a file is in "Deleted" status
When I query the FileArtifact record
Then the record still exists in the database
  And is marked with is_deleted = true
  And is excluded from user-facing file lists (filter where is_deleted = false)
```

**Scenario 3: Physical Deletion from MinIO**
```gherkin
Given a file is in "Deleted" status
  And the file has been deleted for > 30 days
When the cleanup worker runs
Then the file object is deleted from MinIO
  And the FileArtifact record is marked with minio_deleted = true
  And storage space is reclaimed
```

## Functional Requirements

### File Upload & Storage
1. **Presigned URL Generation**: Generate MinIO presigned URLs (valid for 15 minutes) for moderator-initiated file uploads. URL must be unique, single-use if possible, and scoped to the correct bucket/tenant.
2. **Streaming Upload Support**: Support multipart uploads for files > 50MB via presigned URLs.
3. **MinIO Bucket Isolation**: Files stored in per-tenant MinIO buckets with naming convention `muntada-tenant-{tenant_id}`.
4. **Upload Validation**: Validate file size against tenant plan limits (e.g., P1 plan: 100MB max file, P2 plan: 1GB max file) before upload begins.
5. **MIME Type Validation**: Only allow whitelisted MIME types (PDF, DOCX, PPTX, XLS, JPG, PNG, ZIP, MP4, MP3). Block executable types (EXE, COM, BAT, etc.).

### Malware Scanning
6. **RabbitMQ Scan Workflow**: Publish scan request to `files.scan.queue` with payload: `{file_artifact_id, bucket, object_key, file_size, mime_type}`.
7. **Scan Status Transitions**: FileArtifact state machine: `Uploaded → Scanning → (Ready|Rejected)`. No manual state changes allowed after Scanning starts.
8. **Scan Retry Logic**: Up to 3 automatic retries on scan timeout with exponential backoff (30s, 60s, 120s).
9. **Scan Result Immutability**: Once FileScanResult is persisted, all fields are immutable. History logged via AuditEvent.
10. **Threat Handling**: Files with threats detected move to Rejected with reason stored. No recovery mechanism.

### File Access Control
11. **Recipient List Enforcement**: Every download check must verify participant ID against file's recipient_list before serving. Enforce at API layer and audit layer.
12. **Guest Listener Exclusion**: Deny all file downloads to guest listeners (is_guest_listener = true) regardless of recipient list.
13. **Ready-State Gating**: Only files with status = "Ready" are downloadable. All other statuses (Scanning, Rejected, Expired, Deleted) block downloads.
14. **Download Audit**: Every successful and failed download attempt produces FileDownloadAudit event with: participant_id, file_artifact_id, timestamp, tenant_id, success_flag, error_reason (if failed).

### File Expiry & Retention
15. **Auto-Expiry Job**: Daily batch job checks all Ready files; transitions to Expired if `created_at + retention_days < now`. Retention days sourced from tenant config.
16. **Configurable Retention**: Tenant admins set file_retention_days (range: 1-365 days). Applies to all new uploads; existing files updated retroactively on policy change.

### Metadata Persistence
17. **File Metadata Schema**: Store file_name, file_size, mime_type, uploader_user_id, upload_timestamp, scan_result, expiry_timestamp, recipient_type, recipient_ids (JSON array).
18. **Artifact Lifecycle Tracking**: Track created_at, uploaded_at, scanning_started_at, scanning_completed_at, ready_at, expired_at, deleted_at for compliance audits.

### Key Entities

**FileArtifact**
- `file_artifact_id` (UUID, PK)
- `room_id` (UUID, FK → Room)
- `tenant_id` (UUID, FK → Tenant)
- `uploader_user_id` (UUID, FK → User)
- `file_name` (string, 255)
- `file_size` (long, bytes)
- `mime_type` (string, 50)
- `status` (enum: Uploaded, Scanning, Ready, Rejected, Expired, Deleted)
- `recipient_type` (enum: ALL_PARTICIPANTS, SPECIFIC_PARTICIPANTS, MODERATOR_ONLY)
- `recipient_ids` (JSON array of UUIDs, nullable)
- `minio_bucket` (string)
- `minio_object_key` (string)
- `created_at` (datetime)
- `uploaded_at` (datetime, nullable)
- `scanning_started_at` (datetime, nullable)
- `scanning_completed_at` (datetime, nullable)
- `ready_at` (datetime, nullable)
- `expired_at` (datetime, nullable)
- `deleted_at` (datetime, nullable)
- `deleted_by_user_id` (UUID, nullable)
- `download_count` (int)
- `is_deleted` (bool, default false)
- `minio_deleted` (bool, default false)
- `created_at_utc` (datetime)
- `updated_at_utc` (datetime)

**FileScanResult**
- `scan_result_id` (UUID, PK)
- `file_artifact_id` (UUID, FK → FileArtifact, unique)
- `tenant_id` (UUID, FK → Tenant)
- `scan_timestamp` (datetime)
- `threat_found` (bool)
- `threat_description` (string, nullable)
- `scanner_name` (string)
- `scanner_version` (string)
- `signature_version` (string)
- `scan_duration_ms` (int)
- `retry_count` (int, default 0)
- `created_at_utc` (datetime)
- `updated_at_utc` (datetime)

**FileDownloadAudit**
- `download_audit_id` (UUID, PK)
- `file_artifact_id` (UUID, FK → FileArtifact)
- `participant_user_id` (UUID, FK → User)
- `room_id` (UUID, FK → Room)
- `tenant_id` (UUID, FK → Tenant)
- `download_timestamp` (datetime)
- `success` (bool)
- `error_reason` (string, nullable)
- `ip_address` (string, nullable)
- `user_agent` (string, nullable)
- `created_at_utc` (datetime)

## Success Criteria

- [ ] Presigned URL generation completes in < 100ms for 95th percentile of requests
- [ ] Files transition from Scanning to Ready/Rejected within 5 minutes for 99% of files < 500MB
- [ ] Malware scan retries occur automatically with no manual intervention
- [ ] Download audit events are persisted within 100ms of download attempt
- [ ] File expiry job processes all tenant files within 1 hour daily
- [ ] Guest listeners are consistently blocked from downloading files (100% enforcement in tests)
- [ ] Recipient list filtering is accurate for all 4 delivery types (All, Specific, Moderator, None)
- [ ] File status transitions are immutable once Scanning starts (no manual overrides)
- [ ] MIME type validation blocks all dangerous types (EXE, COM, BAT, DLL, etc.)
- [ ] Deleted files are immediately unavailable to users but remain in database for audit (soft delete)

## Edge Cases

1. **Concurrent Upload of Same File Name**: Two moderators upload files with identical names in the same room. System generates unique minio_object_keys with UUID suffix. Both artifacts are distinct records.

2. **Scan Worker Failure Mid-Scan**: Worker crashes while scanning. RabbitMQ message is requeued. After 3 failed retries, file transitions to Rejected with reason "Scan worker unavailable".

3. **Expired File Downloaded Between Expiry Check and Download**: File expires at T+30 days. Participant requests download at T+30 days 0h 0m 1s. Download blocked with "File has expired".

4. **Recipient List Updated After Scan Complete**: File is Ready with recipient_ids = [user1, user2]. Moderator adds user3 to recipient list. New list applies immediately; no re-scan needed.

5. **File Uploaded to Deleted Room**: Moderator uploads file to room. Room is deleted (soft delete). File remains in database but is inaccessible. Cannot be downloaded (room_id now points to deleted room).

6. **Malware Detected on Re-scan**: File was Ready. Signature update occurs. Re-scan triggered manually. Threats now detected. File transitions to Rejected. Participants who previously downloaded have audit record but cannot re-download.

7. **MinIO Outage During Upload**: Presigned URL generated. MinIO is unavailable. Upload fails on client side. FileArtifact remains in "Uploaded" status. Scan never triggers. After 24 hours, cleanup job deletes orphaned artifact.

8. **Large File on Slow Connection**: 500MB file upload. Connection drops at 99%. Upload incomplete. Presigned URL expires. User must re-upload. Orphaned multipart upload cleaned by MinIO lifecycle policy.

9. **File Size Exceeds Tenant Limit After Plan Downgrade**: File was uploaded when tenant was on P2 plan (1GB limit). Tenant downgrades to P1 plan (100MB limit). File is already uploaded. No action taken (historical files grandfathered). New uploads enforce P1 limit.

10. **All Participants Selected, Then Room Emptied**: File set to ALL_PARTICIPANTS. All participants leave room. File is still Ready. File list is empty. File cannot be downloaded by anyone.

## Assumptions

1. **Malware Scanner Available**: Assume ClamAV or equivalent scanning service is available via HTTP endpoint. Scan timeout is 30 minutes.

2. **MinIO Always Available**: Assume MinIO is deployed in HA cluster with automatic failover. Single presigned URL failures retry internally. Bucket exists for all tenants.

3. **RabbitMQ Reliability**: Assume RabbitMQ is configured with durable queues and persistent messages. No message loss on broker failure.

4. **File Size Accuracy**: Assume MIME type and file size reported by client are accurate. Server validates but does not re-hash files.

5. **Participant List Immutable During Download**: Assume participant list does not change mid-download. Download checks are point-in-time.

6. **Tenant Plan Limits Enforced Upstream**: Assume Tenancy module enforces file_retention_days and storage quotas. Files module trusts these values.

7. **Audit Events are Fire-and-Forget**: Assume FileDownloadAudit write failures do not block download. Audit is best-effort background operation.

8. **No File Encryption Required**: Assume files are stored in MinIO without additional encryption beyond MinIO's default TLS transport. Compliance per PDPL is tenant responsibility.

9. **Guest Listener Status is Reliable**: Assume is_guest_listener flag on User is always accurate and synchronized. No race conditions on guest status change.

10. **File Deletion Does Not Reclaim Space Immediately**: Assume deleted files remain in MinIO until cleanup job runs (24-48 hours later). Storage reporting reflects logical usage, not physical cleanup.

## Dependencies

- **Epic 3 (Rooms)**: File operations require active Room context. Room queries (active status, participant list, participant roles) must be available.
- **Epic 2 (Tenancy)**: File operations require Tenant context for isolation, retention policies, plan limits, and recipient access controls. Tenant policies enforce who can upload and download.
- **MinIO Deployment**: Requires MinIO cluster with per-tenant bucket provisioning and presigned URL generation support.
- **RabbitMQ Deployment**: Requires durable queue for scan requests. Scan worker consumer must be deployed separately.
- **Malware Scanning Service**: External or internal service capable of scanning files. HTTP endpoint with timeout handling.

---

**Document Version:** 1.0
**Last Updated:** 2026-04-03
**Module Owner:** Architecture Team
**Status:** Ready for Implementation
