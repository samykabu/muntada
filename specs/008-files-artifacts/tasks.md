# Epic 8: Files & Artifacts Module - Task Breakdown

**Document Version:** 1.0
**Last Updated:** 2026-04-03
**Module:** Files
**Dependencies:** Epic 3 (Rooms), Epic 2 (Tenancy), Epic 10 (Reporting & Audit)

---

## Phase 1: Setup & Infrastructure

### T001: Create Files Module Structure
- **Description:** Initialize Files module directory structure and base configuration
- **Files to Create:**
  - `backend/src/Modules/Files/Domain/` (entities, value objects)
  - `backend/src/Modules/Files/Application/` (commands, queries, handlers)
  - `backend/src/Modules/Files/Infrastructure/` (persistence, MinIO, malware scanning)
  - `backend/src/Modules/Files/Api/` (controllers, DTOs)
- **Acceptance:** Module scaffold created with proper folder structure

### T002: Define Files Domain Models
- **Description:** Create core Files domain entities: FileArtifact, FileScanResult, FileDownloadAudit
- **Files:**
  - `backend/src/Modules/Files/Domain/FileArtifact.cs`
  - `backend/src/Modules/Files/Domain/FileScanResult.cs`
  - `backend/src/Modules/Files/Domain/FileDownloadAudit.cs`
  - `backend/src/Modules/Files/Domain/FileStatus.cs` (enum)
  - `backend/src/Modules/Files/Domain/RecipientType.cs` (enum)
  - `backend/src/Modules/Files/Domain/ValueObjects/MinIOReference.cs`
- **Acceptance:** All entities created with proper validation and aggregate root pattern

### T003: Create Database Schema and Migrations
- **Description:** Create SQL Server schema for Files module with tables and indexes
- **Files:**
  - `backend/src/Modules/Files/Infrastructure/Persistence/FilesDbContext.cs`
  - `backend/src/Modules/Files/Infrastructure/Persistence/Migrations/` (initial schema)
- **Acceptance:** Schema with proper relationships, soft delete support, audit trail columns

### T004: Implement MinIO Integration Service
- **Description:** Create wrapper for MinIO operations (presigned URLs, bucket management, file ops)
- **Files:**
  - `backend/src/Modules/Files/Infrastructure/MinIOFileStorageService.cs`
  - `backend/src/Modules/Files/Infrastructure/PresignedUrlGenerator.cs`
  - `backend/src/Modules/Files/Infrastructure/MinIOClientWrapper.cs`
- **Acceptance:** Presigned URL generation < 100ms, file upload/download/delete working

### T005: Implement Malware Scanning Integration
- **Description:** Create RabbitMQ publisher and webhook handler for malware scanning
- **Files:**
  - `backend/src/Modules/Files/Infrastructure/MalwareScanPublisher.cs`
  - `backend/src/Modules/Files/Api/ScanResultWebhookController.cs`
  - `backend/src/Modules/Files/Infrastructure/ScanResultProcessor.cs`
- **Acceptance:** Scan requests published to RabbitMQ, webhook handler processes scan results

### T006: Implement File Validation Service
- **Description:** Create MIME type validation and file size enforcement
- **Files:**
  - `backend/src/Modules/Files/Domain/Services/FileValidationService.cs`
  - `backend/src/Modules/Files/Domain/AllowedMimeTypes.cs`
- **Acceptance:** Only whitelisted MIME types allowed, size limits enforced per plan

---

## Phase 2: Core File Operations

### T007: [P] Implement Presigned URL Generation Endpoint [US-8.1]
- **Description:** Create API endpoint to generate MinIO presigned URLs for upload
- **Files:**
  - `backend/src/Modules/Files/Api/FileUploadController.cs`
  - `backend/src/Modules/Files/Application/Commands/GeneratePresignedUrlCommand.cs`
  - `backend/src/Modules/Files/Application/Handlers/GeneratePresignedUrlHandler.cs`
  - `backend/src/Modules/Files/Application/Queries/GetUploadLimitsQuery.cs`
- **Acceptance:** Endpoint returns presigned URL with 15-min expiry, validates tenant plan limits

### T008: [P] Implement File Upload and Artifact Creation [US-8.1]
- **Description:** Create FileArtifact record after successful upload, trigger scan workflow
- **Files:**
  - `backend/src/Modules/Files/Application/Commands/CreateFileArtifactCommand.cs`
  - `backend/src/Modules/Files/Application/Handlers/CreateFileArtifactHandler.cs`
  - `backend/src/Modules/Files/Api/FileUploadCompleteController.cs`
- **Acceptance:** FileArtifact created with status "Uploaded", scan request published to RabbitMQ

### T009: [P] Implement Recipient List Management [US-8.1]
- **Description:** Create logic to set and enforce recipient lists (all, specific, moderator-only)
- **Files:**
  - `backend/src/Modules/Files/Domain/Services/RecipientListValidator.cs`
  - `backend/src/Modules/Files/Application/Commands/SetFileRecipientCommand.cs`
  - `backend/src/Modules/Files/Application/Handlers/SetFileRecipientHandler.cs`
- **Acceptance:** Recipient types set correctly, enforced during download checks

### T010: [P] Implement Malware Scan Request Publishing [US-8.2]
- **Description:** Publish file scan requests to RabbitMQ when files are uploaded
- **Files:**
  - `backend/src/Modules/Files/Application/Events/FileUploadedEvent.cs`
  - `backend/src/Modules/Files/Application/Handlers/FileUploadedEventHandler.cs`
- **Acceptance:** Event published with correlation ID, RabbitMQ message includes file metadata

### T011: [P] Implement Scan Result Handler with Retry Logic [US-8.2]
- **Description:** Handle scan results from webhook, manage retries, update file status
- **Files:**
  - `backend/src/Modules/Files/Application/Commands/ProcessScanResultCommand.cs`
  - `backend/src/Modules/Files/Application/Handlers/ProcessScanResultHandler.cs`
  - `backend/src/Modules/Files/Infrastructure/ScanRetryService.cs`
- **Acceptance:** Results processed, file status transitions (Ready/Rejected), retries scheduled on timeout

### T012: [P] Implement File Download Authorization and Audit [US-8.3]
- **Description:** Create download endpoint with authorization checks and audit logging
- **Files:**
  - `backend/src/Modules/Files/Api/FileDownloadController.cs`
  - `backend/src/Modules/Files/Application/Queries/CanDownloadFileQuery.cs`
  - `backend/src/Modules/Files/Application/Handlers/CanDownloadFileQueryHandler.cs`
  - `backend/src/Modules/Files/Application/Commands/RecordFileDownloadCommand.cs`
  - `backend/src/Modules/Files/Application/Handlers/RecordFileDownloadHandler.cs`
- **Acceptance:** Authorization enforced, FileDownloadAudit created, download count incremented

### T013: [P] Implement File Status State Machine [US-8.2]
- **Description:** Enforce valid FileArtifact state transitions
- **Files:**
  - `backend/src/Modules/Files/Domain/Services/FileStatusTransitionValidator.cs`
  - `backend/src/Modules/Files/Domain/FileStatusTransitions.cs`
- **Acceptance:** Uploaded→Scanning→Ready/Rejected, immutable after Scanning starts

---

## Phase 3: File Lifecycle & Retention

### T014: [P] Implement File Auto-Expiry Job [US-8.4]
- **Description:** Create background job to auto-expire files per tenant retention policy
- **Files:**
  - `backend/src/Modules/Files/Infrastructure/Jobs/FileExpiryJob.cs`
  - `backend/src/Modules/Files/Application/Commands/ExpireFileCommand.cs`
  - `backend/src/Modules/Files/Application/Handlers/ExpireFileHandler.cs`
- **Acceptance:** Job runs daily, files transitioned to Expired after retentionDays

### T015: [P] Implement File Soft Delete [US-8.5]
- **Description:** Create moderator-initiated file deletion (soft delete with audit)
- **Files:**
  - `backend/src/Modules/Files/Application/Commands/DeleteFileCommand.cs`
  - `backend/src/Modules/Files/Application/Handlers/DeleteFileHandler.cs`
  - `backend/src/Modules/Files/Api/FileDeletionController.cs`
- **Acceptance:** File marked deleted, removed from user-facing lists, audit trail recorded

### T016: [P] Implement Physical File Cleanup Job [US-8.5]
- **Description:** Create background job for permanent deletion from MinIO after retention period
- **Files:**
  - `backend/src/Modules/Files/Infrastructure/Jobs/FileCleanupJob.cs`
  - `backend/src/Modules/Files/Application/Commands/PhysicallyDeleteFileCommand.cs`
- **Acceptance:** Files deleted from MinIO after 30 days of soft delete, minio_deleted flag set

### T017: [P] Implement File Listing with Filters [US-8.3]
- **Description:** Create endpoints to list files in room with authorization and filtering
- **Files:**
  - `backend/src/Modules/Files/Api/FileQueryController.cs`
  - `backend/src/Modules/Files/Application/Queries/GetRoomFilesQuery.cs`
  - `backend/src/Modules/Files/Application/Handlers/GetRoomFilesQueryHandler.cs`
- **Acceptance:** List endpoint paginated, filters by status, recipient access enforced

### T018: [P] Implement File Metadata Retrieval [US-8.3]
- **Description:** Create endpoint to get detailed file metadata
- **Files:**
  - `backend/src/Modules/Files/Application/Queries/GetFileMetadataQuery.cs`
  - `backend/src/Modules/Files/Application/Handlers/GetFileMetadataQueryHandler.cs`
- **Acceptance:** Returns all metadata fields, authorization enforced

---

## Phase 4: Frontend & User Interface

### T019: [P] Implement File Upload Component [US-8.1]
- **Description:** Create React component for file selection and upload flow
- **Files:**
  - `frontend/src/features/files/FileUploadDialog.tsx`
  - `frontend/src/features/files/FileUploadProgressBar.tsx`
  - `frontend/src/features/files/useFileUpload.ts`
  - `frontend/src/features/files/fileUploadApi.ts`
- **Acceptance:** Dialog opens, file selected, presigned URL fetched, multipart upload initiated

### T020: [P] Implement Recipient Selection UI [US-8.1]
- **Description:** Create UI for moderator to select file recipients
- **Files:**
  - `frontend/src/features/files/RecipientSelector.tsx`
  - `frontend/src/features/files/RecipientTypeSelector.tsx`
  - `frontend/src/features/files/ParticipantChecklistModal.tsx`
- **Acceptance:** Radio buttons for all/specific/moderator, participant list for specific selection

### T021: [P] Implement Scan Status Display [US-8.2]
- **Description:** Create UI showing file scan status and results
- **Files:**
  - `frontend/src/features/files/FileScanStatusBadge.tsx`
  - `frontend/src/features/files/FileStatusIndicator.tsx`
- **Acceptance:** Shows Uploading/Scanning/Ready/Rejected status with icons

### T022: [P] Implement Files List View [US-8.3]
- **Description:** Create list of available files with status, size, download buttons
- **Files:**
  - `frontend/src/features/files/FilesList.tsx`
  - `frontend/src/features/files/FileRow.tsx`
  - `frontend/src/features/files/useFilesList.ts`
  - `frontend/src/features/files/fileListApi.ts`
- **Acceptance:** Paginated list, filters by status, shows file metadata, download buttons present

### T023: [P] Implement File Download Control [US-8.3]
- **Description:** Create download button with authorization check and audit
- **Files:**
  - `frontend/src/features/files/FileDownloadButton.tsx`
  - `frontend/src/features/files/useFileDownload.ts`
- **Acceptance:** Button disabled if not authorized, download initiated on click, audit logged

### T024: [P] Implement File Deletion UI (Moderator) [US-8.5]
- **Description:** Create delete button with confirmation modal for moderators
- **Files:**
  - `frontend/src/features/files/FileDeleteButton.tsx`
  - `frontend/src/features/files/FileDeleteConfirmModal.tsx`
  - `frontend/src/features/files/useFileDelete.ts`
- **Acceptance:** Button only visible to moderators, confirmation required, file removed from list

### T025: [P] Implement Real-Time File Status Updates [US-8.2, US-8.3]
- **Description:** Setup SignalR for live file status updates in UI
- **Files:**
  - `frontend/src/features/files/fileStatusSignalRHub.ts`
  - `frontend/src/features/files/useFileStatusUpdates.ts`
- **Acceptance:** Status changes broadcast via SignalR, UI updates in real-time

---

## Phase 5: Testing & Documentation

### T026: [P] Write Unit Tests for Files Domain and Application
- **Description:** Comprehensive unit tests for all commands, handlers, queries
- **Files:**
  - `backend/src/Modules/Files/Tests/Domain/FileArtifactTests.cs`
  - `backend/src/Modules/Files/Tests/Domain/FileStatusTransitionTests.cs`
  - `backend/src/Modules/Files/Tests/Application/RecipientListTests.cs`
  - `backend/src/Modules/Files/Tests/Application/DownloadAuthorizationTests.cs`
  - `backend/src/Modules/Files/Tests/Application/ScanRetryTests.cs`
- **Acceptance:** > 85% code coverage, all spec scenarios tested

### T027: [P] Write Integration Tests for Files APIs
- **Description:** Integration tests for full file upload/download/delete workflows
- **Files:**
  - `backend/src/Modules/Files/Tests/Integration/FileUploadWorkflowTests.cs`
  - `backend/src/Modules/Files/Tests/Integration/MalwareScanWorkflowTests.cs`
  - `backend/src/Modules/Files/Tests/Integration/DownloadAuthorizationTests.cs`
  - `backend/src/Modules/Files/Tests/Integration/FileExpiryTests.cs`
  - `backend/src/Modules/Files/Tests/Integration/FileDeletionTests.cs`
- **Acceptance:** All user story scenarios automated, tests pass

### T028: [P] Write Frontend Component Tests
- **Description:** Unit and integration tests for React components
- **Files:**
  - `frontend/src/features/files/__tests__/FileUploadDialog.test.tsx`
  - `frontend/src/features/files/__tests__/RecipientSelector.test.tsx`
  - `frontend/src/features/files/__tests__/FilesList.test.tsx`
  - `frontend/src/features/files/__tests__/FileDownloadButton.test.tsx`
  - `frontend/src/features/files/__tests__/FileDeleteButton.test.tsx`
- **Acceptance:** > 80% coverage, all user interactions tested

### T029: Create Files Module Documentation
- **Description:** API documentation, workflow diagrams, integration guides
- **Files:**
  - `docs/modules/files/README.md`
  - `docs/modules/files/api.md`
  - `docs/modules/files/upload-workflow.md`
  - `docs/modules/files/malware-scanning.md`
  - `docs/modules/files/recipient-types.md`
  - `docs/modules/files/INTEGRATION_GUIDE.md`
- **Acceptance:** Complete documentation with examples

---

## Checkpoint 1: File Upload & Scanning (After T013)
**Criteria:**
- Presigned URL generation < 100ms
- FileArtifact created with Uploaded status
- Scan requests published to RabbitMQ
- Scan results processed with retry logic
- File status transitions work correctly
- All Phase 2 unit tests passing

## Checkpoint 2: Downloads & Authorization (After T018)
**Criteria:**
- Authorization checks working for all recipient types
- FileDownloadAudit events created for every download
- Download count incremented correctly
- File listing respects authorization
- Metadata retrieval working

## Checkpoint 3: Full Feature Complete (After T029)
**Criteria:**
- All 5 user stories implemented (US-8.1 through US-8.5)
- File expiry job running and transitioning files to Expired
- Physical cleanup job deleting files from MinIO
- Frontend UI complete with upload/list/download
- Real-time status updates via SignalR
- Integration tests covering all workflows
- Documentation complete

---

## Dependencies Between Tasks

- **T001-T006** (Setup): Must complete before all other tasks
- **T007-T009**: Core file operations, proceed in parallel, needed by T010-T013
- **T010-T013**: Building on T007-T009, can proceed in parallel
- **T014-T018**: Depend on T007-T013 for file context, can proceed in parallel
- **T019-T025**: Frontend tasks, depend on T007-T018 for API endpoints
- **T026-T028**: Testing, depend on T007-T025 for implementations
- **T029**: Documentation, depends on all implementations

---

## Success Metrics

- Presigned URL generation < 100ms for p95
- File upload with scan completes in < 500ms
- Malware scan completes within 5 minutes for 99% of files < 500MB
- Download audit events persisted within 100ms
- File expiry job processes all files within 1 hour
- Guest listeners blocked from downloads (100% enforcement)
- Recipient list filtering accurate for all 4 types
- File status immutable after Scanning starts

---

**Notes:**
- All file operations include audit trail entries
- Files module integrates with Epic 10 (Reporting & Audit)
- MinIO bucket structure: `muntada-tenant-{tenantId}/`
- File naming: `{roomId}/{timestamp}_{filename}`
- Scan retry backoff: 30s, 60s, 120s (max 3 retries)
- File download links: 24-hour pre-signed URLs
- SQLServer indexes on (roomId, status) and (tenantId, createdAt)
