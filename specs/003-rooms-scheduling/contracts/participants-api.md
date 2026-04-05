# API Contract: Participants & Recordings

**Auth**: JWT Bearer

---

## Participants

### GET /api/v1/tenants/{tenantId}/room-occurrences/{occurrenceId}/participants

List current participants. Accessible to any room participant.

**Response** `200 OK`:
```json
{
  "participants": [
    {
      "id": "rps_abc",
      "userId": "usr_123",
      "displayName": "Ahmed",
      "role": "Moderator",
      "joinedAt": "2026-04-10T14:00:30Z",
      "audioState": "Unmuted",
      "videoState": "On"
    },
    {
      "id": "rps_def",
      "userId": null,
      "displayName": "Guest User",
      "role": "Guest",
      "joinedAt": "2026-04-10T14:05:00Z",
      "audioState": "Muted",
      "videoState": "Off"
    }
  ],
  "totalCount": 2
}
```

**Note**: During Live rooms, this data is served from Redis cache for sub-millisecond reads. For Ended rooms, it falls back to SQL Server.

---

## SignalR Hub: RoomHub

**Path**: `/hubs/rooms`
**Auth**: JWT Bearer (connection-level)

### Client → Server

| Method | Params | Description |
|--------|--------|-------------|
| `JoinRoomGroup` | `occurrenceId` | Subscribe to room events |
| `LeaveRoomGroup` | `occurrenceId` | Unsubscribe from room events |

### Server → Client (Broadcast to room group)

| Event | Payload | Trigger |
|-------|---------|---------|
| `ParticipantJoined` | `{ participantId, userId, displayName, role, joinedAt }` | Participant connects |
| `ParticipantLeft` | `{ participantId, userId, leftAt }` | Participant disconnects |
| `ParticipantMediaChanged` | `{ participantId, audioState, videoState }` | Audio/video state change |
| `RoomStatusChanged` | `{ occurrenceId, status, graceStartedAt?, graceExpiresAt? }` | Room state transition |
| `ModeratorChanged` | `{ occurrenceId, newModeratorUserId, newModeratorName }` | Moderator handover |
| `RecordingStatusChanged` | `{ occurrenceId, isRecording }` | Recording start/stop |

---

## Recordings

### GET /api/v1/tenants/{tenantId}/room-occurrences/{occurrenceId}/recording

Get recording metadata. **Visibility-gated**: Private (organizer only), Shared (participants), Public (any tenant member).

**Response** `200 OK`:
```json
{
  "id": "rec_abc",
  "roomOccurrenceId": "occ_ghi789",
  "durationSeconds": 3600,
  "fileSizeBytes": 52428800,
  "status": "Ready",
  "visibility": "Shared",
  "downloadUrl": "https://minio.muntada.local/recordings/...",
  "transcripts": [
    {
      "language": "en",
      "status": "Ready",
      "downloadUrl": "https://minio.muntada.local/transcripts/...",
      "textDownloadUrl": "https://minio.muntada.local/transcripts/..."
    }
  ],
  "createdAt": "2026-04-10T15:00:00Z"
}
```

**Note**: `downloadUrl` and `textDownloadUrl` are pre-signed S3 URLs (15 min expiry).

**Errors**:
- `403` — Visibility restriction (user not authorized)
- `404` — No recording for this room

---

## LiveKit Webhook Handler

### POST /api/v1/webhooks/livekit

Receives LiveKit webhook events. **Auth**: HMAC-SHA256 signature verification.

**Handled events**:
- `participant_joined` → Create/update `RoomParticipantState`, broadcast via SignalR, trigger `Scheduled → Live` if first participant
- `participant_left` → Update `RoomParticipantState.LeftAt`, broadcast via SignalR, trigger `Live → Grace` if moderator
- `track_published` / `track_unpublished` → Update `MediaState`, broadcast via SignalR
- `egress_ended` → Update `Recording.Status` to Ready/Failed, publish `RecordingCompleted`

**Response**: `200 OK` (idempotent — duplicate events are safely ignored via event ID tracking).
