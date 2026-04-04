# API Contract: Feature Toggles

**Base Path**: `/api/v1/admin/feature-toggles` (internal admin API)

---

## GET /api/v1/admin/feature-toggles — List Feature Toggles

**Auth**: Required (Bearer JWT, system admin)
**Description**: List all feature toggles with their current state.

**Response 200**:
```json
{
  "toggles": [
    {
      "id": "ftg_abc123",
      "featureName": "livekit-3d-viz",
      "isEnabled": true,
      "scope": "PerTenant",
      "canaryPercentage": 0,
      "overrideCount": 3,
      "createdAt": "2026-04-01T00:00:00Z",
      "updatedAt": "2026-04-03T10:00:00Z"
    }
  ]
}
```

---

## POST /api/v1/admin/feature-toggles — Create Feature Toggle

**Auth**: Required (Bearer JWT, system admin)
**Description**: Create a new feature toggle (disabled by default).

**Request Body**:
```json
{
  "featureName": "advanced-analytics",
  "scope": "Canary",
  "canaryPercentage": 5
}
```

| Field | Type | Required | Constraints |
|-------|------|----------|-------------|
| featureName | string | Yes | Unique, kebab-case, max 100 chars |
| scope | string | Yes | Global, PerTenant, PerUser, PerRegion, Canary |
| canaryPercentage | int | No | 0-100, required if scope is Canary |

**Response 201**: Created toggle response

**Error Responses**:
- `400` — Validation errors
- `409` — Feature name already exists

---

## PATCH /api/v1/admin/feature-toggles/{toggleId} — Update Feature Toggle

**Auth**: Required (Bearer JWT, system admin)
**Description**: Update toggle state, scope, or canary percentage.

**Request Body**:
```json
{
  "isEnabled": true,
  "canaryPercentage": 25
}
```

**Response 200**: Updated toggle response

---

## PUT /api/v1/admin/feature-toggles/{toggleId}/overrides/{tenantId} — Set Tenant Override

**Auth**: Required (Bearer JWT, system admin)
**Description**: Enable or disable a feature for a specific tenant.

**Request Body**:
```json
{
  "isEnabled": true
}
```

**Response 200**: Updated toggle with override response

---

## DELETE /api/v1/admin/feature-toggles/{toggleId}/overrides/{tenantId} — Remove Tenant Override

**Auth**: Required (Bearer JWT, system admin)
**Description**: Remove a tenant-specific override (tenant falls back to global state).

**Response 204**: No content

---

## GET /api/v1/tenants/{tenantId}/features — Check Enabled Features (Tenant API)

**Auth**: Required (Bearer JWT, must be member of tenant)
**Description**: List features enabled for the current tenant (used by frontend to show/hide UI).

**Response 200**:
```json
{
  "enabledFeatures": [
    "recording",
    "guest-access",
    "custom-branding",
    "advanced-analytics"
  ]
}
```
