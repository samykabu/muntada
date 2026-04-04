# API Contract: Usage

**Base Path**: `/api/v1/tenants/{tenantId}/usage`

---

## GET /api/v1/tenants/{tenantId}/usage — Current Usage

**Auth**: Required (Bearer JWT, Owner or Admin role)
**Description**: Get current resource usage against plan limits.

**Response 200**:
```json
{
  "tenantId": "tnt_abc123",
  "billingPeriod": {
    "start": "2026-04-01T00:00:00Z",
    "end": "2026-04-30T23:59:59Z"
  },
  "metrics": [
    {
      "resource": "rooms",
      "current": 45,
      "limit": 200,
      "unit": "rooms/month",
      "percentUsed": 22.5,
      "thresholdStatus": "normal"
    },
    {
      "resource": "storage",
      "current": 85.3,
      "limit": 200,
      "unit": "GB",
      "percentUsed": 42.65,
      "thresholdStatus": "normal"
    },
    {
      "resource": "recordingHours",
      "current": 48.5,
      "limit": 50,
      "unit": "hours/month",
      "percentUsed": 97.0,
      "thresholdStatus": "critical"
    },
    {
      "resource": "peakParticipants",
      "current": 67,
      "limit": 100,
      "unit": "participants",
      "percentUsed": 67.0,
      "thresholdStatus": "normal"
    }
  ],
  "retentionSetting": "90 days (recordings)",
  "lastUpdatedAt": "2026-04-03T06:00:00Z"
}
```

**Threshold statuses**: `normal` (< 80%), `warning` (80-94%), `critical` (95-99%), `exceeded` (100%)

---

## GET /api/v1/tenants/{tenantId}/usage/history — Usage History

**Auth**: Required (Bearer JWT, Owner or Admin role)
**Description**: Get daily usage trends for the past 30 days.

**Query Parameters**:
| Param | Type | Default | Description |
|-------|------|---------|-------------|
| days | int | 30 | Number of days (max 90) |
| metric | string | "all" | Filter: rooms, storage, recordingHours, participants, all |

**Response 200**:
```json
{
  "tenantId": "tnt_abc123",
  "period": {
    "start": "2026-03-04",
    "end": "2026-04-03"
  },
  "dailySnapshots": [
    {
      "date": "2026-04-03",
      "roomsCreated": 5,
      "roomsCreatedMonth": 45,
      "peakParticipants": 67,
      "storageUsedGB": 85.3,
      "recordingHoursUsed": 48.5
    }
  ]
}
```
