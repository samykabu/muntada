# API Contract: Room Series

**Base Path**: `/api/v1/tenants/{tenantId}/room-series`
**Auth**: JWT Bearer, requires `Admin` or `Owner` tenant role

---

## POST /api/v1/tenants/{tenantId}/room-series

Create a recurring room series.

**Request**:
```json
{
  "templateId": "tpl_abc123",
  "title": "Weekly Standup",
  "recurrenceRule": "FREQ=WEEKLY;BYDAY=MO,WE",
  "organizerTimeZoneId": "Asia/Riyadh",
  "startsAt": "2026-04-07T09:00:00Z",
  "endsAt": null,
  "moderatorUserId": "usr_456",
  "gracePeriodSeconds": 300
}
```

**Response** `201 Created`:
```json
{
  "id": "ser_def456",
  "tenantId": "tnt_xyz",
  "templateId": "tpl_abc123",
  "title": "Weekly Standup",
  "recurrenceRule": "FREQ=WEEKLY;BYDAY=MO,WE",
  "organizerTimeZoneId": "Asia/Riyadh",
  "startsAt": "2026-04-07T09:00:00Z",
  "endsAt": null,
  "status": "Active",
  "createdBy": "usr_123",
  "createdAt": "2026-04-04T10:00:00Z",
  "occurrencesGenerated": 9
}
```

**Errors**:
- `400` — Invalid RRULE syntax, invalid timezone, template not found
- `403` — Not Admin/Owner

---

## GET /api/v1/tenants/{tenantId}/room-series

List series for a tenant.

**Query**: `?status=Active&page=1&pageSize=20`

**Response** `200 OK`: Paginated list of series DTOs.

---

## GET /api/v1/tenants/{tenantId}/room-series/{seriesId}

Get series details with next 5 upcoming occurrences.

**Response** `200 OK`: Series DTO with `upcomingOccurrences[]`.

---

## PATCH /api/v1/tenants/{tenantId}/room-series/{seriesId}

Update series (recurrence, title, end date).

**Request**:
```json
{
  "recurrenceRule": "FREQ=WEEKLY;BYDAY=MO,WE,FR",
  "endsAt": "2026-12-31T23:59:59Z"
}
```

**Response** `200 OK`: Updated series DTO.

**Side effect**: Future occurrences are regenerated.

---

## POST /api/v1/tenants/{tenantId}/room-series/{seriesId}/end

End a series (no future occurrences generated).

**Response** `200 OK`: Series DTO with `status: "Ended"`.
