# API Contract: Plans

**Base Path**: `/api/v1/tenants/{tenantId}/plan`

---

## GET /api/v1/tenants/{tenantId}/plan — Get Current Plan

**Auth**: Required (Bearer JWT, must be member of tenant)
**Description**: Retrieve the tenant's current plan and its limits.

**Response 200**:
```json
{
  "id": "pln_abc123",
  "planName": "Professional",
  "tier": "Professional",
  "monthlyPriceUsd": 99.00,
  "startDate": "2026-04-01T00:00:00Z",
  "limits": {
    "maxRoomsPerMonth": 200,
    "maxParticipantsPerRoom": 100,
    "maxStorageGB": 200,
    "maxRecordingHoursPerMonth": 50,
    "maxDataRetentionDays": 365,
    "allowRecording": true,
    "allowGuestAccess": true,
    "allowCustomBranding": true
  }
}
```

---

## GET /api/v1/plans/available — List Available Plans

**Auth**: Required (Bearer JWT)
**Description**: List all available plan definitions for comparison.

**Response 200**:
```json
{
  "plans": [
    {
      "id": "pdef_free",
      "name": "Free",
      "tier": "Free",
      "monthlyPriceUsd": 0,
      "limits": { ... }
    },
    {
      "id": "pdef_starter",
      "name": "Starter",
      "tier": "Starter",
      "monthlyPriceUsd": 29.00,
      "limits": { ... }
    }
  ]
}
```

---

## POST /api/v1/tenants/{tenantId}/plan/upgrade — Upgrade Plan

**Auth**: Required (Bearer JWT, Owner role)
**Description**: Upgrade to a higher-tier plan. Effective immediately with pro-rated charges.

**Request Body**:
```json
{
  "targetPlanDefinitionId": "pdef_professional"
}
```

**Response 200**:
```json
{
  "previousPlan": "Starter",
  "newPlan": "Professional",
  "effectiveDate": "2026-04-03T15:00:00Z",
  "proratedChargeUsd": 46.67,
  "newLimits": { ... }
}
```

**Error Responses**:
- `400` — Target plan is not higher tier than current
- `403` — Only Owners can change plans

---

## POST /api/v1/tenants/{tenantId}/plan/downgrade — Downgrade Plan

**Auth**: Required (Bearer JWT, Owner role)
**Description**: Downgrade to a lower-tier plan.

**Request Body**:
```json
{
  "targetPlanDefinitionId": "pdef_starter",
  "effectiveDate": "immediate"
}
```

| Field | Type | Required | Constraints |
|-------|------|----------|-------------|
| targetPlanDefinitionId | string | Yes | Must be lower tier than current |
| effectiveDate | string | Yes | "immediate" or "next-billing-cycle" |

**Response 200**:
```json
{
  "previousPlan": "Professional",
  "newPlan": "Starter",
  "effectiveDate": "2026-04-03T15:00:00Z",
  "usageWarnings": [
    {
      "resource": "storage",
      "currentUsage": 80,
      "newLimit": 50,
      "unit": "GB",
      "message": "Current storage usage (80 GB) exceeds new plan limit (50 GB)"
    }
  ],
  "newLimits": { ... }
}
```

**Error Responses**:
- `400` — Target plan is not lower tier, or validation errors
- `403` — Only Owners can change plans
- `409` — Another plan change is in progress
