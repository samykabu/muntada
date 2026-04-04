# API Contract: Tenants

**Base Path**: `/api/v1/tenants`

---

## POST /api/v1/tenants — Create Tenant

**Auth**: Required (Bearer JWT)
**Description**: Create a new organization (tenant). Creator becomes Owner automatically.

**Request Body**:
```json
{
  "name": "Acme Corporation",
  "slug": "acme-corp",
  "industry": "technology",
  "teamSize": "11-50"
}
```

| Field | Type | Required | Constraints |
|-------|------|----------|-------------|
| name | string | Yes | 3-100 chars |
| slug | string | No | Auto-generated from name if omitted. Lowercase alphanumeric + hyphens, 3-63 chars, unique |
| industry | string | No | Optional dropdown value |
| teamSize | string | No | Optional range string |

**Response 201**:
```json
{
  "id": "tnt_abc123",
  "name": "Acme Corporation",
  "slug": "acme-corp",
  "status": "Active",
  "billingStatus": "Trial",
  "trialEndsAt": "2026-04-17T00:00:00Z",
  "createdAt": "2026-04-03T12:00:00Z"
}
```

**Error Responses**:
- `400` — Validation errors (name too short, slug format invalid)
- `409` — Slug already taken

---

## GET /api/v1/tenants/{tenantId} — Get Tenant

**Auth**: Required (Bearer JWT, must be member of tenant)
**Description**: Retrieve tenant details including branding.

**Response 200**:
```json
{
  "id": "tnt_abc123",
  "name": "Acme Corporation",
  "slug": "acme-corp",
  "status": "Active",
  "billingStatus": "Trial",
  "trialEndsAt": "2026-04-17T00:00:00Z",
  "branding": {
    "logoUrl": "https://minio.muntada.com/tenants/tnt_abc123/logo-128.png",
    "primaryColor": "#1A73E8",
    "secondaryColor": "#F1F3F4",
    "customDomain": null
  },
  "createdAt": "2026-04-03T12:00:00Z",
  "updatedAt": "2026-04-03T12:00:00Z"
}
```

**Error Responses**:
- `403` — Not a member of this tenant
- `404` — Tenant not found

---

## PATCH /api/v1/tenants/{tenantId}/branding — Update Branding

**Auth**: Required (Bearer JWT, Owner or Admin role)
**Description**: Update tenant branding (logo, colors, custom domain).

**Request Body** (multipart/form-data):
| Field | Type | Required | Constraints |
|-------|------|----------|-------------|
| logo | file | No | Max 5MB, image/png or image/jpeg |
| primaryColor | string | No | Hex format #RRGGBB |
| secondaryColor | string | No | Hex format #RRGGBB |
| customDomain | string | No | Valid hostname, unique |

**Response 200**: Updated tenant response (same as GET)

**Error Responses**:
- `400` — Invalid color format, file too large
- `403` — Insufficient role (must be Owner or Admin)
- `409` — Custom domain already taken
