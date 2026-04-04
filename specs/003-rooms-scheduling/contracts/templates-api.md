# API Contract: Room Templates

**Base Path**: `/api/v1/tenants/{tenantId}/room-templates`
**Auth**: JWT Bearer, requires `Admin` or `Owner` tenant role

---

## POST /api/v1/tenants/{tenantId}/room-templates

Create a room template.

**Request**:
```json
{
  "name": "Weekly Standup",
  "description": "Standard standup template",
  "settings": {
    "maxParticipants": 50,
    "allowGuestAccess": true,
    "allowRecording": true,
    "allowTranscription": false,
    "defaultTranscriptionLanguage": null,
    "autoStartRecording": false
  }
}
```

**Response** `201 Created`:
```json
{
  "id": "tpl_abc123",
  "tenantId": "tnt_xyz",
  "name": "Weekly Standup",
  "description": "Standard standup template",
  "settings": { ... },
  "createdBy": "usr_123",
  "createdAt": "2026-04-04T10:00:00Z",
  "updatedAt": "2026-04-04T10:00:00Z"
}
```

**Errors**:
- `400` — Validation failure (name too short, max participants > plan limit)
- `403` — User is not Admin/Owner
- `409` — Template name already exists in tenant

---

## GET /api/v1/tenants/{tenantId}/room-templates

List templates for a tenant.

**Query**: `?page=1&pageSize=20`

**Response** `200 OK`:
```json
{
  "items": [ { ...templateDto } ],
  "totalCount": 5,
  "page": 1,
  "pageSize": 20
}
```

---

## GET /api/v1/tenants/{tenantId}/room-templates/{templateId}

Get a single template.

**Response** `200 OK`: Single template DTO.

**Errors**: `404` — Template not found.

---

## PATCH /api/v1/tenants/{tenantId}/room-templates/{templateId}

Update template (all fields except name).

**Request**:
```json
{
  "description": "Updated description",
  "settings": {
    "maxParticipants": 100
  }
}
```

**Response** `200 OK`: Updated template DTO.

**Errors**:
- `400` — Validation failure
- `403` — Not Admin/Owner
- `404` — Not found
- `409` — Concurrency conflict (stale version)
