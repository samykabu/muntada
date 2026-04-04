# API Contract: Retention Policies

**Base Path**: `/api/v1/tenants/{tenantId}/retention-policies`

---

## GET /api/v1/tenants/{tenantId}/retention-policies — Get Retention Policy

**Auth**: Required (Bearer JWT, Owner or Admin role)
**Description**: Retrieve the tenant's data retention configuration.

**Response 200**:
```json
{
  "id": "rtn_abc123",
  "tenantId": "tnt_abc123",
  "policies": {
    "recordings": {
      "retentionDays": 90,
      "default": 90,
      "minAllowed": 1,
      "maxAllowed": 3650
    },
    "chatMessages": {
      "retentionDays": 365,
      "default": 365,
      "minAllowed": 1,
      "maxAllowed": 3650
    },
    "files": {
      "retentionDays": 365,
      "default": 365,
      "minAllowed": 1,
      "maxAllowed": 3650
    },
    "auditLogs": {
      "retentionDays": 2555,
      "default": 2555,
      "minAllowed": 2555,
      "maxAllowed": 3650
    },
    "userActivityLogs": {
      "retentionDays": 365,
      "default": 365,
      "minAllowed": 1,
      "maxAllowed": 3650
    }
  },
  "updatedAt": "2026-04-03T12:00:00Z"
}
```

---

## PATCH /api/v1/tenants/{tenantId}/retention-policies — Update Retention Policy

**Auth**: Required (Bearer JWT, Owner role)
**Description**: Update retention periods. Only provided fields are updated.

**Request Body**:
```json
{
  "recordingRetentionDays": 30,
  "chatMessageRetentionDays": 180,
  "auditLogRetentionDays": 2555
}
```

| Field | Type | Required | Constraints |
|-------|------|----------|-------------|
| recordingRetentionDays | int | No | 1-3650 |
| chatMessageRetentionDays | int | No | 1-3650 |
| fileRetentionDays | int | No | 1-3650 |
| auditLogRetentionDays | int | No | 2555-3650 (min 7 years) |
| userActivityLogRetentionDays | int | No | 1-3650 |

**Response 200**: Updated retention policy response

**Error Responses**:
- `400` — Validation error (e.g., audit log retention below 7 years)
- `403` — Only Owners can modify retention policies

**Side Effect**: If retention is reduced, existing data older than the new retention period is immediately scheduled for deletion (enters 7-day grace period).
