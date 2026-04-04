# API Contract: Members

**Base Path**: `/api/v1/tenants/{tenantId}/members`

---

## GET /api/v1/tenants/{tenantId}/members — List Members

**Auth**: Required (Bearer JWT, must be member of tenant)
**Description**: List all members of the tenant with pagination.

**Query Parameters**:
| Param | Type | Default | Description |
|-------|------|---------|-------------|
| page | int | 1 | Page number |
| pageSize | int | 20 | Items per page (max 100) |
| status | string | "Active" | Filter: Active, Pending, Inactive, All |

**Response 200**:
```json
{
  "items": [
    {
      "id": "mbr_xyz789",
      "userId": "usr_abc123",
      "email": "john@acme.com",
      "displayName": "John Doe",
      "role": "Owner",
      "status": "Active",
      "joinedAt": "2026-04-03T12:00:00Z",
      "lastActivityAt": "2026-04-03T14:30:00Z"
    }
  ],
  "totalCount": 1,
  "page": 1,
  "pageSize": 20
}
```

---

## POST /api/v1/tenants/{tenantId}/members/invite — Invite Member

**Auth**: Required (Bearer JWT, Owner or Admin role)
**Description**: Send an invitation to join the tenant.

**Request Body**:
```json
{
  "email": "jane@acme.com",
  "role": "Member",
  "message": "Welcome to our workspace!"
}
```

| Field | Type | Required | Constraints |
|-------|------|----------|-------------|
| email | string | Yes | Valid email address |
| role | string | Yes | Owner, Admin, or Member |
| message | string | No | Custom invite message, max 500 chars |

**Response 201**:
```json
{
  "id": "mbr_inv456",
  "email": "jane@acme.com",
  "role": "Member",
  "status": "Pending",
  "invitedAt": "2026-04-03T15:00:00Z",
  "expiresAt": "2026-04-10T15:00:00Z"
}
```

**Error Responses**:
- `400` — Invalid email or role
- `403` — Insufficient role (Admins cannot invite Owners)
- `409` — Email already has an active membership in this tenant

---

## POST /api/v1/tenants/{tenantId}/members/accept — Accept Invite

**Auth**: Required (Bearer JWT)
**Description**: Accept a pending invitation using the invite token.

**Request Body**:
```json
{
  "token": "base64url-encoded-invite-token"
}
```

**Response 200**: Membership response with status "Active"

**Error Responses**:
- `400` — Token expired or already used
- `404` — Token not found

---

## PATCH /api/v1/tenants/{tenantId}/members/{memberId}/role — Update Role

**Auth**: Required (Bearer JWT, Owner role)
**Description**: Change a member's role.

**Request Body**:
```json
{
  "role": "Admin"
}
```

**Response 200**: Updated membership response

**Error Responses**:
- `400` — Cannot remove last Owner
- `403` — Insufficient role (only Owners can change roles)

---

## DELETE /api/v1/tenants/{tenantId}/members/{memberId} — Remove Member

**Auth**: Required (Bearer JWT, Owner or Admin role)
**Description**: Remove a member from the tenant (soft delete to Inactive).

**Response 204**: No content

**Error Responses**:
- `400` — Cannot remove last Owner
- `403` — Insufficient role (Admins cannot remove Owners)
