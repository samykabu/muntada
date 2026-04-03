# API Contracts: Identity & Access Management

**Epic**: 001-identity-access
**Base Path**: `/api/v1/identity`
**Date**: 2026-04-03
**Status**: Approved

---

## Conventions

- All endpoints return JSON with `Content-Type: application/json`.
- All timestamps are ISO 8601 with UTC offset (`2026-04-03T12:00:00Z`).
- Authentication: `Authorization: Bearer <accessToken>` header unless noted otherwise.
- Rate-limited endpoints return `429 Too Many Requests` with `Retry-After` header.
- Error responses follow **RFC 9457 Problem Details** format (see Error Response Format section).
- Opaque IDs use prefixed format: `usr_`, `ses_`, `otp_`, `lnk_`, `pat_`, `prt_`, `evt_`.

---

## Registration & Login

### POST /api/v1/identity/auth/register

Creates a new user account with Unverified status and sends a verification email.

**Authentication**: None (public endpoint)

**Rate Limit**: 10 requests per IP per hour

**Request Body**:
```json
{
  "email": "user@example.com",
  "password": "SecureP@ssw0rd!",
  "confirmPassword": "SecureP@ssw0rd!",
  "fullName": "Ahmad Al-Rashid"
}
```

| Field | Type | Required | Validation |
|---|---|---|---|
| email | string | Yes | Valid email, max 256 chars |
| password | string | Yes | Min 12 chars, 1 uppercase, 1 number, 1 special char |
| confirmPassword | string | Yes | Must match password |
| fullName | string | Yes | Min 1, max 200 chars |

**Response 201 Created**:
```json
{
  "id": "usr_abc123def456",
  "email": "user@example.com",
  "fullName": "Ahmad Al-Rashid",
  "status": "Unverified",
  "createdAt": "2026-04-03T12:00:00Z"
}
```

**Error Responses**:
| Status | Condition |
|---|---|
| 400 | Validation failure (weak password, email format, password mismatch) |
| 409 | Email already registered (generic message: "Registration could not be completed") |
| 429 | Rate limit exceeded |

---

### POST /api/v1/identity/auth/login

Authenticates a user with email and password. Returns JWT access token and sets refresh token cookie.

**Authentication**: None (public endpoint)

**Rate Limit**: 5 failed attempts per email per 15 minutes

**Request Body**:
```json
{
  "email": "user@example.com",
  "password": "SecureP@ssw0rd!"
}
```

| Field | Type | Required | Validation |
|---|---|---|---|
| email | string | Yes | Valid email format |
| password | string | Yes | Non-empty |

**Response 200 OK**:
```json
{
  "accessToken": "eyJhbGciOiJSUzI1NiIsImtpZCI6Ims...",
  "expiresIn": 900,
  "tokenType": "Bearer",
  "user": {
    "id": "usr_abc123def456",
    "email": "user@example.com",
    "fullName": "Ahmad Al-Rashid",
    "status": "Active"
  }
}
```

**Response Headers**:
```
Set-Cookie: refresh_token=<opaque>; HttpOnly; Secure; SameSite=Strict; Path=/api/v1/identity/auth/refresh; Max-Age=2592000
```

**Error Responses**:
| Status | Condition |
|---|---|
| 400 | Validation failure (missing fields) |
| 401 | Invalid email or password (generic message: "Invalid email or password") |
| 403 | Account is Unverified (includes message to check email or resend verification) |
| 423 | Account is Suspended |
| 429 | Rate limit exceeded (too many failed attempts) |

---

### POST /api/v1/identity/auth/verify-email

Verifies a user's email address using the token from the verification email.

**Authentication**: None (public endpoint)

**Request Body**:
```json
{
  "token": "base64url-encoded-verification-token"
}
```

| Field | Type | Required | Validation |
|---|---|---|---|
| token | string | Yes | Non-empty, Base64URL format |

**Response 200 OK**:
```json
{
  "message": "Email verified successfully."
}
```

**Error Responses**:
| Status | Condition |
|---|---|
| 400 | Missing or malformed token |
| 410 | Token expired or already used |

---

### POST /api/v1/identity/auth/resend-verification

Resends the email verification link. Invalidates any previously active verification tokens.

**Authentication**: None (public endpoint)

**Rate Limit**: 3 requests per email per hour

**Request Body**:
```json
{
  "email": "user@example.com"
}
```

| Field | Type | Required | Validation |
|---|---|---|---|
| email | string | Yes | Valid email format |

**Response 200 OK**:
```json
{
  "message": "If an unverified account exists, a verification email has been sent."
}
```

Note: Always returns 200 regardless of whether the email exists or the account is already verified. This prevents user enumeration.

**Error Responses**:
| Status | Condition |
|---|---|
| 400 | Invalid email format |
| 429 | Rate limit exceeded |

---

## Token & Sessions

### POST /api/v1/identity/auth/refresh

Refreshes an expired access token using the refresh token from the HTTP-only cookie.

**Authentication**: Refresh token cookie (automatic, no body required)

**Request Body**: None (refresh token is read from the `refresh_token` cookie)

**Response 200 OK**:
```json
{
  "accessToken": "eyJhbGciOiJSUzI1NiIsImtpZCI6Ims...",
  "expiresIn": 900,
  "tokenType": "Bearer"
}
```

**Response Headers**:
```
Set-Cookie: refresh_token=<new-opaque>; HttpOnly; Secure; SameSite=Strict; Path=/api/v1/identity/auth/refresh; Max-Age=2592000
```

Note: A new refresh token is issued on each refresh (rotation). The previous token is marked as Used.

**Error Responses**:
| Status | Condition |
|---|---|
| 401 | Missing, invalid, expired, or revoked refresh token |

---

### POST /api/v1/identity/auth/logout

Logs out the current user by revoking the active session and clearing the refresh token cookie.

**Authentication**: Required (Bearer token)

**Request Body**: None

**Response 204 No Content**

**Response Headers**:
```
Set-Cookie: refresh_token=; HttpOnly; Secure; SameSite=Strict; Path=/api/v1/identity/auth/refresh; Max-Age=0
```

**Side Effects**:
- Current session marked as `Revoked`
- Refresh token invalidated
- `UserLoggedOutEvent` published

**Error Responses**:
| Status | Condition |
|---|---|
| 401 | Not authenticated |

---

### GET /api/v1/identity/sessions

Lists all sessions for the authenticated user.

**Authentication**: Required (Bearer token)

**Query Parameters**: None

**Response 200 OK**:
```json
{
  "sessions": [
    {
      "id": "ses_xyz789abc012",
      "deviceInfo": {
        "userAgent": "Mozilla/5.0 (Windows NT 10.0; Win64; x64)...",
        "ipAddress": "203.0.113.42",
        "deviceFingerprint": null
      },
      "isCurrent": true,
      "createdAt": "2026-04-03T12:00:00Z",
      "lastActivityAt": "2026-04-03T14:30:00Z",
      "expiresAt": "2026-04-04T14:30:00Z"
    },
    {
      "id": "ses_def456ghi789",
      "deviceInfo": {
        "userAgent": "Mozilla/5.0 (iPhone; CPU iPhone OS 17_0)...",
        "ipAddress": "198.51.100.17",
        "deviceFingerprint": null
      },
      "isCurrent": false,
      "createdAt": "2026-04-02T08:00:00Z",
      "lastActivityAt": "2026-04-03T09:15:00Z",
      "expiresAt": "2026-04-04T09:15:00Z"
    }
  ]
}
```

---

### DELETE /api/v1/identity/sessions/{sessionId}

Revokes a specific session. The user can only revoke their own sessions.

**Authentication**: Required (Bearer token)

**Path Parameters**:
| Parameter | Type | Description |
|---|---|---|
| sessionId | string | Session ID to revoke (e.g., `ses_def456ghi789`) |

**Response 204 No Content**: Session successfully revoked.

**Error Responses**:
| Status | Condition |
|---|---|
| 401 | Not authenticated |
| 403 | Session does not belong to the authenticated user |
| 404 | Session not found |

---

### DELETE /api/v1/identity/sessions?exceptCurrent=true

Revokes all sessions for the authenticated user except the current one.

**Authentication**: Required (Bearer token)

**Query Parameters**:
| Parameter | Type | Required | Description |
|---|---|---|---|
| exceptCurrent | boolean | Yes | Must be `true` |

**Response 204 No Content**: All other sessions successfully revoked.

**Error Responses**:
| Status | Condition |
|---|---|
| 400 | `exceptCurrent` is missing or false |
| 401 | Not authenticated |

---

## Phone OTP

### POST /api/v1/identity/auth/otp/challenge

Initiates a phone OTP challenge. Sends a 6-digit code via SMS.

**Authentication**: None (public endpoint)

**Rate Limit**: 3 challenges per phone number per hour

**Request Body**:
```json
{
  "phoneNumber": "+966501234567"
}
```

| Field | Type | Required | Validation |
|---|---|---|---|
| phoneNumber | string | Yes | E.164 format |

**Response 200 OK**:
```json
{
  "challengeId": "otp_mno345pqr678",
  "expiresAt": "2026-04-03T12:15:00Z"
}
```

Note: Returns 200 regardless of whether a user with that phone number exists (prevents enumeration). If no user exists, the SMS is not actually sent.

**Error Responses**:
| Status | Condition |
|---|---|
| 400 | Invalid phone number format |
| 429 | Rate limit exceeded |

---

### POST /api/v1/identity/auth/otp/verify

Verifies the OTP code and creates a session if valid.

**Authentication**: None (public endpoint)

**Request Body**:
```json
{
  "challengeId": "otp_mno345pqr678",
  "code": "482917"
}
```

| Field | Type | Required | Validation |
|---|---|---|---|
| challengeId | string | Yes | Valid challenge ID |
| code | string | Yes | Exactly 6 digits |

**Response 200 OK** (identical structure to login):
```json
{
  "accessToken": "eyJhbGciOiJSUzI1NiIsImtpZCI6Ims...",
  "expiresIn": 900,
  "tokenType": "Bearer",
  "user": {
    "id": "usr_abc123def456",
    "email": "user@example.com",
    "fullName": "Ahmad Al-Rashid",
    "status": "Active"
  }
}
```

**Response Headers**:
```
Set-Cookie: refresh_token=<opaque>; HttpOnly; Secure; SameSite=Strict; Path=/api/v1/identity/auth/refresh; Max-Age=2592000
```

**Error Responses**:
| Status | Condition |
|---|---|
| 400 | Missing or invalid fields |
| 401 | Invalid code (remaining attempts included in response body) |
| 410 | Challenge expired or locked (max attempts exceeded) |

---

## Guest Magic Links

### POST /api/v1/identity/magic-links

Creates a guest magic link for a room occurrence.

**Authentication**: Required (Bearer token, must be room organizer)

**Request Body**:
```json
{
  "roomOccurrenceId": "roc_stu901vwx234",
  "expiresInDays": 7
}
```

| Field | Type | Required | Validation |
|---|---|---|---|
| roomOccurrenceId | string | Yes | Valid room occurrence ID |
| expiresInDays | integer | No | 1-30, default 7 |

**Response 201 Created**:
```json
{
  "id": "lnk_yza567bcd890",
  "roomOccurrenceId": "roc_stu901vwx234",
  "url": "https://app.muntada.sa/join?token=base64url-encoded-token",
  "expiresAt": "2026-04-10T12:00:00Z",
  "createdAt": "2026-04-03T12:00:00Z"
}
```

Note: The plaintext token is embedded in the URL and returned exactly once. Only the SHA256 hash is stored.

**Error Responses**:
| Status | Condition |
|---|---|
| 400 | Validation failure |
| 401 | Not authenticated |
| 403 | User is not the room organizer |
| 404 | Room occurrence not found |

---

### GET /api/v1/identity/magic-links/validate?token=...

Validates a magic link token and creates a guest session.

**Authentication**: None (public endpoint)

**Query Parameters**:
| Parameter | Type | Required | Description |
|---|---|---|---|
| token | string | Yes | Base64URL-encoded magic link token |

**Response 200 OK**:
```json
{
  "guestSessionId": "ses_gst_efg123hij456",
  "roomOccurrenceId": "roc_stu901vwx234",
  "permissions": ["listen"],
  "expiresAt": "2026-04-03T14:00:00Z"
}
```

**Error Responses**:
| Status | Condition |
|---|---|
| 400 | Missing or malformed token |
| 404 | Token not found (generic, does not disclose existence) |
| 410 | Link expired or revoked |

---

### DELETE /api/v1/identity/magic-links/{linkId}

Revokes a guest magic link.

**Authentication**: Required (Bearer token, must be the link creator)

**Path Parameters**:
| Parameter | Type | Description |
|---|---|---|
| linkId | string | Magic link ID (e.g., `lnk_yza567bcd890`) |

**Response 204 No Content**: Link successfully revoked.

**Error Responses**:
| Status | Condition |
|---|---|
| 401 | Not authenticated |
| 403 | User did not create this link |
| 404 | Link not found |

---

## Personal Access Tokens (PATs)

### POST /api/v1/identity/pats

Creates a new Personal Access Token. The plaintext token is returned exactly once.

**Authentication**: Required (Bearer token)

**Request Body**:
```json
{
  "name": "CI/CD Pipeline Token",
  "scopes": ["rooms:read", "rooms:write", "recordings:read"],
  "expiresInDays": 90
}
```

| Field | Type | Required | Validation |
|---|---|---|---|
| name | string | Yes | Min 1, max 100 chars |
| scopes | string[] | Yes | At least one valid scope |
| expiresInDays | integer | Yes | 1-365 |

**Response 201 Created**:
```json
{
  "id": "pat_klm012nop345",
  "name": "CI/CD Pipeline Token",
  "token": "mnt_pat_a1b2c3d4e5f6g7h8i9j0k1l2m3n4o5p6",
  "scopes": ["rooms:read", "rooms:write", "recordings:read"],
  "expiresAt": "2026-07-02T12:00:00Z",
  "createdAt": "2026-04-03T12:00:00Z"
}
```

**IMPORTANT**: The `token` field is the plaintext value and is returned ONLY in this response. Subsequent GET requests will NOT include it. The user must save it immediately.

**Error Responses**:
| Status | Condition |
|---|---|
| 400 | Validation failure (invalid scopes, missing name) |
| 401 | Not authenticated |

---

### GET /api/v1/identity/pats

Lists all PATs for the authenticated user in the current tenant.

**Authentication**: Required (Bearer token)

**Response 200 OK**:
```json
{
  "tokens": [
    {
      "id": "pat_klm012nop345",
      "name": "CI/CD Pipeline Token",
      "scopes": ["rooms:read", "rooms:write", "recordings:read"],
      "status": "Active",
      "createdAt": "2026-04-03T12:00:00Z",
      "expiresAt": "2026-07-02T12:00:00Z",
      "lastUsedAt": "2026-04-03T14:30:00Z"
    }
  ]
}
```

Note: The plaintext token value is NEVER returned in list responses.

---

### DELETE /api/v1/identity/pats/{patId}

Revokes a Personal Access Token.

**Authentication**: Required (Bearer token)

**Path Parameters**:
| Parameter | Type | Description |
|---|---|---|
| patId | string | PAT ID (e.g., `pat_klm012nop345`) |

**Response 204 No Content**: PAT successfully revoked.

**Error Responses**:
| Status | Condition |
|---|---|
| 401 | Not authenticated |
| 403 | PAT does not belong to the authenticated user |
| 404 | PAT not found |

---

## Password Reset

### POST /api/v1/identity/auth/forgot-password

Initiates a password reset flow. Always returns 200 regardless of whether the email exists.

**Authentication**: None (public endpoint)

**Rate Limit**: 3 requests per email per hour

**Request Body**:
```json
{
  "email": "user@example.com"
}
```

| Field | Type | Required | Validation |
|---|---|---|---|
| email | string | Yes | Valid email format |

**Response 200 OK**:
```json
{
  "message": "If an account with that email exists, a password reset link has been sent."
}
```

Note: Always returns 200 to prevent email enumeration. If the account does not exist, no email is sent.

**Error Responses**:
| Status | Condition |
|---|---|
| 400 | Invalid email format |
| 429 | Rate limit exceeded |

---

### POST /api/v1/identity/auth/reset-password

Resets the password using a valid reset token. All other sessions are revoked on success.

**Authentication**: None (public endpoint)

**Request Body**:
```json
{
  "token": "base64url-encoded-reset-token",
  "newPassword": "NewSecureP@ssw0rd!",
  "confirmNewPassword": "NewSecureP@ssw0rd!"
}
```

| Field | Type | Required | Validation |
|---|---|---|---|
| token | string | Yes | Non-empty, Base64URL format |
| newPassword | string | Yes | Min 12 chars, 1 uppercase, 1 number, 1 special char |
| confirmNewPassword | string | Yes | Must match newPassword |

**Response 200 OK**:
```json
{
  "message": "Password reset successfully. All other sessions have been revoked."
}
```

**Error Responses**:
| Status | Condition |
|---|---|
| 400 | Validation failure (weak password, password mismatch) |
| 410 | Token expired or already used |

---

## Error Response Format (RFC 9457 Problem Details)

All error responses use the RFC 9457 Problem Details for HTTP APIs format:

```json
{
  "type": "https://muntada.sa/errors/validation-failure",
  "title": "Validation Failure",
  "status": 400,
  "detail": "One or more validation errors occurred.",
  "instance": "/api/v1/identity/auth/register",
  "traceId": "00-abcdef1234567890-abcdef12-01",
  "errors": {
    "password": [
      "Password must be at least 12 characters.",
      "Password must contain at least one special character."
    ],
    "confirmPassword": [
      "Passwords do not match."
    ]
  }
}
```

| Field | Type | Description |
|---|---|---|
| type | string (URI) | Problem type identifier |
| title | string | Short human-readable summary |
| status | integer | HTTP status code |
| detail | string | Detailed explanation |
| instance | string | Request path that generated the error |
| traceId | string | OpenTelemetry trace ID for correlation |
| errors | object | Field-level validation errors (optional, 400 responses only) |

### Problem Type Registry

| Type URI | Title | Status |
|---|---|---|
| `https://muntada.sa/errors/validation-failure` | Validation Failure | 400 |
| `https://muntada.sa/errors/authentication-failure` | Authentication Failure | 401 |
| `https://muntada.sa/errors/forbidden` | Forbidden | 403 |
| `https://muntada.sa/errors/not-found` | Not Found | 404 |
| `https://muntada.sa/errors/conflict` | Conflict | 409 |
| `https://muntada.sa/errors/gone` | Gone | 410 |
| `https://muntada.sa/errors/locked` | Locked | 423 |
| `https://muntada.sa/errors/rate-limited` | Rate Limited | 429 |
| `https://muntada.sa/errors/internal` | Internal Server Error | 500 |

---

## HTTP Status Code Reference

| Status Code | Usage in Identity Module |
|---|---|
| 200 OK | Successful login, token refresh, OTP verify, email verify, password reset, list resources |
| 201 Created | Registration, PAT creation, magic link creation |
| 204 No Content | Session revocation, PAT revocation, magic link revocation |
| 400 Bad Request | Validation failures (malformed input, weak password, format errors) |
| 401 Unauthorized | Invalid credentials, expired/invalid tokens, missing authentication |
| 403 Forbidden | Insufficient permissions (not owner, unverified account, wrong tenant) |
| 404 Not Found | Resource does not exist or user has no access |
| 409 Conflict | Duplicate resource (email already registered) |
| 410 Gone | Expired or consumed one-time tokens |
| 423 Locked | Account suspended |
| 429 Too Many Requests | Rate limit exceeded (includes `Retry-After` header) |
| 500 Internal Server Error | Unexpected server errors |

---

## JWT Access Token Claims

```json
{
  "sub": "usr_abc123def456",
  "email": "user@example.com",
  "name": "Ahmad Al-Rashid",
  "tid": "tnt_xyz789",
  "scopes": ["rooms:read", "rooms:write"],
  "sid": "ses_xyz789abc012",
  "jti": "unique-token-id",
  "iat": 1712145600,
  "exp": 1712146500,
  "iss": "https://api.muntada.sa",
  "aud": "https://app.muntada.sa"
}
```

| Claim | Description |
|---|---|
| sub | User ID |
| email | User email |
| name | User display name |
| tid | Tenant ID (null for platform-level) |
| scopes | Permission scopes |
| sid | Session ID |
| jti | JWT ID (for blacklisting) |
| iat | Issued at (Unix timestamp) |
| exp | Expiration (Unix timestamp, iat + 900s) |
| iss | Issuer |
| aud | Audience |
