# Quickstart: Rooms & Scheduling Module

**Branch**: `003-rooms-scheduling` | **Date**: 2026-04-04

## Prerequisites

- .NET 10 SDK
- Node.js 20+ / npm
- Docker Desktop (for Aspire dependencies)
- Git on `003-rooms-scheduling` branch

## Local Development Setup

### 1. Start All Services (Aspire)

```bash
dotnet run --project aspire/Muntada.AppHost
```

This provisions: SQL Server, Redis, RabbitMQ, MinIO, LiveKit, Frontend SPA, and the Backend API.

### 2. Verify Rooms Module Loaded

Open the Aspire dashboard (typically `https://localhost:17220`) and confirm:
- `muntada-api` is running with health checks passing
- SQL Server, Redis, RabbitMQ, MinIO, LiveKit containers are healthy

### 3. Apply Migrations

```bash
cd backend/src/Modules/Rooms
dotnet ef migrations add InitialRoomsSchema --startup-project ../../Muntada.Api
dotnet ef database update --startup-project ../../Muntada.Api
```

**Important**: Never generate migrations via AI tooling. Always use the CLI.

### 4. Verify API Endpoints

```bash
# Create a room template (requires Admin/Owner JWT)
curl -X POST https://localhost:7001/api/v1/tenants/{tenantId}/room-templates \
  -H "Authorization: Bearer <token>" \
  -H "Content-Type: application/json" \
  -d '{
    "name": "Test Template",
    "settings": {
      "maxParticipants": 50,
      "allowGuestAccess": true,
      "allowRecording": false,
      "allowTranscription": false,
      "autoStartRecording": false
    }
  }'
```

### 5. Run Tests

```bash
# Unit tests
cd backend/src/Modules/Rooms.Tests
dotnet test

# Frontend tests
cd frontend
npm test -- --filter rooms
```

## Key Architecture Decisions

| Decision | Choice | Rationale |
|----------|--------|-----------|
| State machine | Stateless library | Typed transitions, guard clauses, graph export |
| Recurrence | Ical.Net | RFC 5545 RRULE, timezone-aware, DST-correct |
| Timezone | IANA IDs + UTC storage | Industry standard, NodaTime compatibility |
| Real-time | Redis + SignalR | Sub-ms reads, group broadcasting |
| Grace timer | MassTransit ScheduleSend | Survives restarts, cancellable |
| Recording | LiveKit Egress → MinIO | Native capture, S3-compatible storage |
| Transcription | AssemblyAI (behind interface) | Arabic+English, swappable provider |

## Module Registration Checklist

When implementing the Rooms module, ensure:

- [ ] `Rooms.csproj` created under `backend/src/Modules/Rooms/`
- [ ] `RoomsDbContext` with `[rooms]` schema registered in `Program.cs`
- [ ] MediatR assembly registered for Rooms handlers
- [ ] Module added to Aspire AppHost (`Muntada.AppHost/AppHost.cs`)
- [ ] SignalR `RoomHub` mapped in API startup
- [ ] LiveKit webhook endpoint registered at `/api/v1/webhooks/livekit`
- [ ] Background jobs registered (OccurrenceGeneration, RetentionCleanup, GracePeriodExpiry)
- [ ] OpenTelemetry activity source registered for `Rooms` traces

## Validation Status

> **Validated**: pending full Aspire stack run. T113 (integration smoke test) and T114 (performance
> benchmarks) require a running Aspire environment with all infrastructure dependencies
> (SQL Server, Redis, RabbitMQ, MinIO, LiveKit). See
> `backend/tests/Modules/Rooms.Tests/Integration/PerformanceNotes.md` for performance
> targets and validation instructions.

## Reference Files

| File | Purpose |
|------|---------|
| `specs/003-rooms-scheduling/spec.md` | Business requirements (clarified) |
| `specs/003-rooms-scheduling/spec-source.md` | Original detailed spec with C# entities |
| `specs/003-rooms-scheduling/tasks-source.md` | Original task breakdown (T301-T329) |
| `specs/003-rooms-scheduling/research.md` | Technology decisions and rationale |
| `specs/003-rooms-scheduling/data-model.md` | Entity definitions, relationships, Redis schema |
| `specs/003-rooms-scheduling/contracts/` | API endpoint contracts |
