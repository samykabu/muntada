# API Contract: Room Invites

**Base Path**: `/api/v1/tenants/{tenantId}/room-occurrences/{occurrenceId}/invites`
**Auth**: JWT Bearer

---

## POST /api/v1/tenants/{tenantId}/room-occurrences/{occurrenceId}/invites

Create invite(s). **Requires Admin/Owner.** Room must be Scheduled or Live.

**Request** (single or bulk):
```json
{
  "invites": [
    { "email": "user@example.com", "type": "Email" },
    { "userId": "usr_789", "type": "DirectLink" }
  ]
}
```

**Request** (guest magic link):
```json
{
  "invites": [
    { "type": "GuestMagicLink" }
  ]
}
```

**Response** `201 Created`:
```json
{
  "invites": [
    {
      "id": "inv_abc",
      "roomOccurrenceId": "occ_ghi789",
      "invitedEmail": "user@example.com",
      "inviteToken": "tok_...",
      "type": "Email",
      "status": "Pending",
      "joinUrl": "/rooms/occ_ghi789/join?token=tok_...",
      "createdAt": "2026-04-04T10:00:00Z",
      "expiresAt": "2026-04-11T10:00:00Z"
    }
  ]
}
```

**Errors**:
- `400` — Room not in Scheduled/Live status, invalid email, rate limit exceeded (100/room/day)
- `403` — Not Admin/Owner

---

## POST /api/v1/tenants/{tenantId}/room-occurrences/{occurrenceId}/invites/bulk

Bulk invite from CSV. **Requires Admin/Owner.**

**Request**: `multipart/form-data` with CSV file (column: `email`).

**Response** `201 Created`: List of created invite DTOs.

**Errors**: `400` — Invalid CSV, too many rows (max 500).

---

## GET /api/v1/tenants/{tenantId}/room-occurrences/{occurrenceId}/invites

List invites for an occurrence. **Requires Admin/Owner.**

**Query**: `?status=Pending&page=1&pageSize=50`

**Response** `200 OK`: Paginated list of invite DTOs.

---

## DELETE /api/v1/tenants/{tenantId}/room-occurrences/{occurrenceId}/invites/{inviteId}

Revoke an invite. **Requires Admin/Owner.** Invite must be Pending.

**Response** `204 No Content`.

**Side effect**: Invite token invalidated immediately.

---

## POST /api/v1/tenants/{tenantId}/room-occurrences/{occurrenceId}/join

Join a room. **Any authenticated user with valid invite or direct link. Guests via magic link (no auth).**

**Request** (authenticated):
```json
{
  "token": "tok_..."
}
```

**Request** (guest magic link):
```json
{
  "token": "tok_...",
  "displayName": "Guest User"
}
```

**Response** `200 OK`:
```json
{
  "participantId": "rps_abc",
  "roomOccurrenceId": "occ_ghi789",
  "role": "Member",
  "livekitToken": "eyJ...",
  "livekitUrl": "wss://livekit.muntada.local",
  "roomStatus": "Live"
}
```

**Errors**:
- `400` — Invalid/expired/revoked token
- `403` — Room not in Scheduled/Live status
- `409` — Room at max participant capacity
