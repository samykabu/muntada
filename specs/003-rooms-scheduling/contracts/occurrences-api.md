# API Contract: Room Occurrences

**Base Path**: `/api/v1/tenants/{tenantId}/room-occurrences`
**Auth**: JWT Bearer

---

## POST /api/v1/tenants/{tenantId}/room-occurrences

Create a standalone one-off room. **Requires Admin/Owner.**

**Request**:
```json
{
  "templateId": "tpl_abc123",
  "title": "Ad-hoc Planning Meeting",
  "scheduledAt": "2026-04-10T14:00:00Z",
  "organizerTimeZoneId": "Asia/Riyadh",
  "moderatorUserId": "usr_456",
  "gracePeriodSeconds": 300,
  "settingsOverride": {
    "maxParticipants": 25
  }
}
```

**Response** `201 Created`:
```json
{
  "id": "occ_ghi789",
  "tenantId": "tnt_xyz",
  "roomSeriesId": null,
  "title": "Ad-hoc Planning Meeting",
  "scheduledAt": "2026-04-10T14:00:00Z",
  "organizerTimeZoneId": "Asia/Riyadh",
  "status": "Scheduled",
  "moderator": {
    "userId": "usr_456",
    "displayName": "Ahmed",
    "assignedAt": "2026-04-04T10:00:00Z"
  },
  "settings": { ... },
  "gracePeriodSeconds": 300,
  "createdBy": "usr_123",
  "createdAt": "2026-04-04T10:00:00Z"
}
```

---

## GET /api/v1/tenants/{tenantId}/room-occurrences

List occurrences. Accessible to all tenant members.

**Query**: `?status=Scheduled&from=2026-04-01&to=2026-04-30&seriesId=ser_def456&page=1&pageSize=50`

**Response** `200 OK`: Paginated list of occurrence DTOs.

---

## GET /api/v1/tenants/{tenantId}/room-occurrences/{occurrenceId}

Get occurrence details. Accessible to all tenant members.

**Response** `200 OK`: Full occurrence DTO with moderator, settings, participant count.

---

## PATCH /api/v1/tenants/{tenantId}/room-occurrences/{occurrenceId}

Update a single occurrence (title, settings, cancel). **Requires Admin/Owner.**

**Request**:
```json
{
  "title": "Updated Title",
  "isCancelled": true,
  "settingsOverride": {
    "maxParticipants": 50
  }
}
```

**Response** `200 OK`: Updated occurrence DTO.

---

## POST /api/v1/tenants/{tenantId}/room-occurrences/{occurrenceId}/transition

Transition room status. **Auth varies by transition.**

**Request**:
```json
{
  "targetStatus": "Live"
}
```

**Valid transitions** (see state machine in data-model.md):
- `Scheduled → Live` — triggered automatically by first participant connect (system)
- `Live → Ended` — moderator action or system (all leave)
- `Grace → Ended` — system (timeout) or moderator action

**Response** `200 OK`: Updated occurrence DTO.

**Errors**:
- `400` — Invalid transition
- `409` — Concurrency conflict

---

## PATCH /api/v1/tenants/{tenantId}/room-occurrences/{occurrenceId}/moderator

Change moderator assignment. **Requires Admin/Owner.** Room must be in Draft or Scheduled status.

**Request**:
```json
{
  "userId": "usr_789"
}
```

**Response** `200 OK`: Updated occurrence DTO.

---

## POST /api/v1/tenants/{tenantId}/room-occurrences/{occurrenceId}/moderator/handover

Handover moderator to a pre-authorized user. Room must be in Grace status.

**Request**:
```json
{
  "toUserId": "usr_789"
}
```

**Response** `200 OK`: Occurrence transitions back to Live with new moderator.

---

## GET /api/v1/tenants/{tenantId}/room-occurrences/{occurrenceId}/analytics

Get post-room analytics. Room must be in Ended or Archived status.

**Response** `200 OK`:
```json
{
  "totalParticipants": 15,
  "peakConcurrentParticipants": 12,
  "durationSeconds": 3600,
  "participantDwellTimes": [
    { "userId": "usr_123", "displayName": "Ahmed", "durationSeconds": 3540 }
  ],
  "audioParticipationRate": 0.85,
  "videoParticipationRate": 0.60
}
```
