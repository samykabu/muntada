# Performance Targets & Validation (T113 / T114)

These tasks require a full Aspire stack run and cannot be validated offline.

## Performance Targets

| Metric | Target | How to Validate |
|--------|--------|-----------------|
| Room template CRUD (p95) | < 100 ms | k6 or Bombardier against POST/GET/PATCH `/api/v1/tenants/{tid}/room-templates` |
| Room join (p95) | < 200 ms | Load test POST `/api/v1/tenants/{tid}/room-occurrences/{oid}/invites/join` |
| Participant list for live room (Redis read) | < 10 ms | Measure `ListParticipantsQueryHandler` via OpenTelemetry span duration |
| Occurrence generation (30-day horizon) | < 5 s for 100 series | Trigger `OccurrenceGenerationJob` manually, observe Aspire traces |
| Grace period message delivery | < 2 s latency | Publish `GracePeriodExpiredMessage`, measure time-to-consume |
| Recording start (egress initiation) | < 3 s | Measure `StartRecordingCommandHandler` span including LiveKit API call |
| SignalR broadcast latency | < 100 ms | WebSocket client measuring `ParticipantJoined` event delay |

## How to Run

1. Start the full Aspire stack: `dotnet run --project aspire/Muntada.AppHost`
2. Wait for all health checks to pass (SQL, Redis, RabbitMQ, MinIO, LiveKit)
3. Apply migrations: `dotnet ef database update --startup-project backend/src/Muntada.Api --project backend/src/Modules/Rooms`
4. Run integration tests: `dotnet test backend/tests/Modules/Rooms.Tests --filter Category=Integration`
5. For load testing: use k6 scripts (TODO: add to `tests/load/`) or Bombardier CLI

## Telemetry Verification

Confirm that OpenTelemetry spans appear in the Aspire dashboard:
- `Rooms.TemplateCreation`
- `Rooms.SeriesCreation`
- `Rooms.RoomTransition`
- `Rooms.ParticipantJoin`
- `Rooms.InviteGeneration`
- `Rooms.RecordingOperation`
- `Rooms.WebhookProcessing`

## Structured Logging Verification

Confirm structured log entries (event IDs 3000-3079) appear in the Aspire console:
- `TemplateCreated` (3000)
- `SeriesCreated` (3010)
- `RoomTransition` (3020)
- `ParticipantJoined` (3040)
- `InviteSent` (3050)
- `ModeratorAssigned` (3060)
- `ModeratorHandover` (3061)
- `RecordingStarted` (3070)
