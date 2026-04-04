# Implementation Plan: Rooms & Scheduling

**Branch**: `003-rooms-scheduling` | **Date**: 2026-04-04 | **Spec**: [spec.md](spec.md)
**Input**: Feature specification from `/specs/003-rooms-scheduling/spec.md`

## Summary

Implement the Rooms & Scheduling module for Muntada — a multi-tenant audio room management system with room templates, recurring series (iCal RRULE), one-off rooms, a strict lifecycle state machine (Draft → Scheduled → Live → Grace → Ended → Archived), participant invites (email, direct link, guest magic link), single-moderator assignment with handover, real-time participant tracking via Redis + SignalR, recording to MinIO with optional transcription, and post-room analytics. Built as a new modular monolith module following Identity/Tenancy patterns with Clean Architecture layers (Domain, Application, Infrastructure, Api).

## Technical Context

**Language/Version**: C# / .NET 10, TypeScript 5.x / React 19
**Primary Dependencies**: ASP.NET Core 10, Entity Framework Core 10, MediatR 14.1.0, FluentValidation 12.1.1, MassTransit 9.1.0 (RabbitMQ), Stateless (state machine), iCal.NET (recurrence rules), LiveKit Server SDK for .NET, SignalR, AWSSDK.S3 (MinIO), StackExchange.Redis
**Storage**: SQL Server (`[rooms]` schema), Redis (participant presence cache), MinIO (recordings/transcripts)
**Testing**: xUnit (unit tests), Playwright (integration/E2E), React Testing Library (frontend)
**Target Platform**: Linux containers on self-managed Kubernetes, .NET Aspire 13.2 for local dev
**Project Type**: Web service (modular monolith module) + React SPA feature
**Performance Goals**: Room creation < 200ms p95, participant join < 500ms p95, state transitions < 100ms, real-time updates < 2s
**Constraints**: Single moderator per room, 100 concurrent rooms with 100 participants each, PDPL audit retention 7 years, GCC region only
**Scale/Scope**: 100 concurrent rooms, 10,000 participants per tenant, 30-day occurrence generation window

## Constitution Check

*GATE: Must pass before Phase 0 research. Re-check after Phase 1 design.*

| # | Principle | Status | Notes |
|---|-----------|--------|-------|
| I | Modular Monolith Discipline | PASS | New `Rooms` module with own `[rooms]` schema, integration events via MassTransit, no cross-schema joins |
| II | Server-Authoritative State | PASS | All room lifecycle, moderator, and participant state managed server-side; clients reflect via SignalR events |
| III | API-First for Multi-Client Readiness | PASS | All capabilities exposed via versioned REST APIs (`/api/v1/rooms/...`) before any UI |
| IV | Test-First for Critical Paths | PASS | TDD mandatory for state machine transitions, authorization policies, invite token validation |
| V | Invite-Only Security Model | PASS | Room access is invite-only; join tokens validated against invite, tenant policy, room state; guests are listen-only |
| VI | Observability from Day One | PASS | OpenTelemetry traces for room lifecycle, join flow, recording; structured logging with correlation IDs |
| VII | Explicit Over Implicit | PASS | Explicit state machine (Stateless library), integration events, opaque IDs with prefixes, idempotent webhook handlers |
| VIII | Clean Code & Documentation | PASS | Clean Architecture layers, XML doc comments on all public types, DRY/KISS |
| IX | Component Reusability | PASS | Shared React components for room cards, participant lists, calendar views |
| X | AI-Safe Database Migrations | PASS | All migrations via `dotnet ef migrations add` CLI only |
| XI | Comprehensive Testing Strategy | PASS | xUnit for unit tests, Playwright for E2E, all tests pass before commit |
| XII | Aspire-First Local Development | PASS | Rooms module registered in Aspire AppHost; all dependencies (SQL, Redis, RabbitMQ, MinIO, LiveKit) already declared |

**Gate Result**: ALL PASS — no violations requiring justification.

## Project Structure

### Documentation (this feature)

```text
specs/003-rooms-scheduling/
├── plan.md              # This file
├── spec.md              # Business specification (clarified)
├── spec-source.md       # Original detailed spec with C# entities
├── tasks-source.md      # Original task breakdown (T301-T329)
├── research.md          # Phase 0 output
├── data-model.md        # Phase 1 output
├── quickstart.md        # Phase 1 output
├── contracts/           # Phase 1 output (API contracts)
│   ├── templates-api.md
│   ├── series-api.md
│   ├── occurrences-api.md
│   ├── invites-api.md
│   └── participants-api.md
└── checklists/
    └── requirements.md  # Spec quality checklist
```

### Source Code (repository root)

```text
backend/
├── src/
│   ├── Modules/
│   │   └── Rooms/
│   │       ├── Rooms.csproj
│   │       ├── Domain/
│   │       │   ├── Template/
│   │       │   │   ├── RoomTemplate.cs          # Aggregate root
│   │       │   │   ├── RoomTemplateId.cs
│   │       │   │   └── RoomSettings.cs          # Value object
│   │       │   ├── Series/
│   │       │   │   ├── RoomSeries.cs            # Aggregate root
│   │       │   │   ├── RoomSeriesId.cs
│   │       │   │   └── RoomSeriesStatus.cs
│   │       │   ├── Occurrence/
│   │       │   │   ├── RoomOccurrence.cs         # Aggregate root + state machine
│   │       │   │   ├── RoomOccurrenceId.cs
│   │       │   │   ├── RoomOccurrenceStatus.cs
│   │       │   │   └── ModeratorAssignment.cs    # Value object
│   │       │   ├── Invite/
│   │       │   │   ├── RoomInvite.cs             # Entity
│   │       │   │   ├── RoomInviteId.cs
│   │       │   │   └── RoomInviteStatus.cs
│   │       │   ├── Participant/
│   │       │   │   ├── RoomParticipantState.cs   # Entity
│   │       │   │   ├── RoomParticipantStateId.cs
│   │       │   │   └── MediaState.cs
│   │       │   ├── Recording/
│   │       │   │   ├── Recording.cs              # Aggregate root
│   │       │   │   ├── RecordingId.cs
│   │       │   │   ├── RecordingStatus.cs
│   │       │   │   ├── RecordingVisibility.cs
│   │       │   │   ├── Transcript.cs
│   │       │   │   └── TranscriptStatus.cs
│   │       │   └── Events/
│   │       │       └── RoomsEvents.cs            # All domain + integration events
│   │       ├── Application/
│   │       │   ├── Commands/
│   │       │   │   ├── CreateRoomTemplateCommandHandler.cs
│   │       │   │   ├── UpdateRoomTemplateCommandHandler.cs
│   │       │   │   ├── CreateRoomSeriesCommandHandler.cs
│   │       │   │   ├── UpdateRoomSeriesCommandHandler.cs
│   │       │   │   ├── CreateRoomOccurrenceCommandHandler.cs
│   │       │   │   ├── UpdateRoomOccurrenceCommandHandler.cs
│   │       │   │   ├── TransitionRoomStatusCommandHandler.cs
│   │       │   │   ├── AssignModeratorCommandHandler.cs
│   │       │   │   ├── HandoverModeratorCommandHandler.cs
│   │       │   │   ├── GenerateRoomInviteCommandHandler.cs
│   │       │   │   ├── RevokeRoomInviteCommandHandler.cs
│   │       │   │   ├── JoinRoomCommandHandler.cs
│   │       │   │   ├── StartRecordingCommandHandler.cs
│   │       │   │   └── StopRecordingCommandHandler.cs
│   │       │   ├── Queries/
│   │       │   │   ├── GetRoomTemplateQueryHandler.cs
│   │       │   │   ├── ListRoomTemplatesQueryHandler.cs
│   │       │   │   ├── GetRoomSeriesQueryHandler.cs
│   │       │   │   ├── ListRoomOccurrencesQueryHandler.cs
│   │       │   │   ├── GetRoomOccurrenceQueryHandler.cs
│   │       │   │   ├── ListRoomInvitesQueryHandler.cs
│   │       │   │   ├── ListParticipantsQueryHandler.cs
│   │       │   │   └── GetRoomAnalyticsQueryHandler.cs
│   │       │   ├── BackgroundJobs/
│   │       │   │   ├── OccurrenceGenerationJob.cs
│   │       │   │   ├── GracePeriodExpiryJob.cs
│   │       │   │   ├── RetentionCleanupJob.cs
│   │       │   │   └── TranscriptionJob.cs
│   │       │   ├── Services/
│   │       │   │   ├── IRecurrenceService.cs
│   │       │   │   ├── IGracePeriodService.cs
│   │       │   │   ├── IRecordingService.cs
│   │       │   │   └── ITranscriptionService.cs
│   │       │   └── Validators/
│   │       │       ├── CreateRoomTemplateValidator.cs
│   │       │       ├── CreateRoomSeriesValidator.cs
│   │       │       ├── CreateRoomOccurrenceValidator.cs
│   │       │       └── GenerateRoomInviteValidator.cs
│   │       ├── Infrastructure/
│   │       │   ├── RoomsDbContext.cs              # [rooms] schema
│   │       │   ├── Services/
│   │       │   │   ├── RecurrenceService.cs       # iCal.NET integration
│   │       │   │   ├── GracePeriodService.cs      # Timer management
│   │       │   │   ├── MinIoRecordingService.cs   # S3 upload/download
│   │       │   │   ├── TranscriptionService.cs    # Speech-to-text integration
│   │       │   │   └── LiveKitWebhookHandler.cs   # Webhook processing
│   │       │   ├── Cache/
│   │       │   │   └── ParticipantStateCache.cs   # Redis participant tracking
│   │       │   ├── RoomsLogging.cs
│   │       │   └── RoomsTelemetry.cs
│   │       └── Api/
│   │           ├── Controllers/
│   │           │   ├── TemplatesController.cs
│   │           │   ├── SeriesController.cs
│   │           │   ├── OccurrencesController.cs
│   │           │   ├── InvitesController.cs
│   │           │   ├── ParticipantsController.cs
│   │           │   └── RecordingsController.cs
│   │           ├── Hubs/
│   │           │   └── RoomHub.cs                 # SignalR hub for real-time updates
│   │           └── Dtos/
│   │               ├── RoomTemplateDto.cs
│   │               ├── RoomSeriesDto.cs
│   │               ├── RoomOccurrenceDto.cs
│   │               ├── RoomInviteDto.cs
│   │               ├── ParticipantDto.cs
│   │               ├── RecordingDto.cs
│   │               └── RoomAnalyticsDto.cs
│   └── Modules/
│       └── Rooms.Tests/
│           ├── Rooms.Tests.csproj
│           ├── Unit/
│           │   ├── Domain/
│           │   │   ├── RoomOccurrenceStateMachineTests.cs
│           │   │   ├── RoomTemplateTests.cs
│           │   │   ├── RoomInviteTests.cs
│           │   │   └── ModeratorAssignmentTests.cs
│           │   └── Application/
│           │       ├── CreateRoomTemplateTests.cs
│           │       ├── CreateRoomSeriesTests.cs
│           │       ├── RecurrenceServiceTests.cs
│           │       └── GracePeriodServiceTests.cs
│           └── Integration/
│               ├── RoomLifecycleTests.cs
│               ├── InviteFlowTests.cs
│               └── RecordingLifecycleTests.cs

frontend/
└── src/
    └── features/
        └── rooms/
            ├── api/
            │   ├── roomsApi.ts
            │   ├── seriesApi.ts
            │   └── invitesApi.ts
            ├── components/
            │   ├── RoomTemplateForm.tsx
            │   ├── RoomCalendar.tsx
            │   ├── ParticipantList.tsx
            │   ├── RoomStatusBadge.tsx
            │   ├── InviteDialog.tsx
            │   └── RecordingPlayer.tsx
            ├── hooks/
            │   └── useRoom.ts
            └── pages/
                ├── CreateRoomPage.tsx
                ├── RoomCalendarPage.tsx
                └── LiveRoomPage.tsx
```

**Structure Decision**: Follows the established modular monolith pattern from Identity and Tenancy modules. The Rooms module is a new C# project under `backend/src/Modules/Rooms/` with Domain/Application/Infrastructure/Api layers. Frontend follows the existing feature-based structure under `frontend/src/features/rooms/`.

## Complexity Tracking

> No violations detected — table not needed.
