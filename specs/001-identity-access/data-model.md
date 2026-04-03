# Data Model: Identity & Access Management

**Epic**: 001-identity-access
**Schema**: `[identity]`
**Date**: 2026-04-03
**Status**: Approved

---

## Entity Definitions

### User (Aggregate Root)

The central identity entity. All authentication flows resolve to a User.

| Property | Type | Constraints | Description |
|---|---|---|---|
| Id | `string` (prefix: `usr_`) | PK, opaque ID | Unique user identifier |
| Email | `Email` (Value Object) | Unique, NOT NULL, max 256 | Lowercased, validated email address |
| PasswordHash | `PasswordHash` (Value Object) | NOT NULL | BCrypt hash (cost 12) of user password |
| PhoneNumber | `PhoneNumber` (Value Object) | Nullable, E.164 format, max 20 | Optional phone for OTP login |
| Status | `UserStatus` (enum) | NOT NULL | Unverified, Active, Suspended, Deleted |
| FullName | `string` | NOT NULL, max 200 | User display name |
| ProfilePictureUrl | `string` | Nullable, max 2048 | URL to profile image in MinIO |
| CreatedAt | `DateTimeOffset` | NOT NULL | Account creation timestamp (UTC) |
| UpdatedAt | `DateTimeOffset` | NOT NULL | Last modification timestamp (UTC) |
| LastLoginAt | `DateTimeOffset` | Nullable | Most recent successful login (UTC) |

**Value Objects**:
- `Email`: Validates format (RFC 5322 simplified), enforces lowercase normalization, max 256 chars.
- `PasswordHash`: Encapsulates bcrypt hash string. Never exposes plaintext. Provides `Verify(plaintext)` method.
- `PhoneNumber`: Validates E.164 format (`+[country code][number]`), max 20 chars.

**Business Rules**:
- Email is immutable after registration (future: email change flow in a separate epic).
- PasswordHash is updated only via password change or password reset flows.
- PhoneNumber can be added/updated by the user after registration.
- Status transitions are enforced by the domain (see state diagram below).

---

### Session (Aggregate Root)

Represents an authenticated session bound to a device/browser.

| Property | Type | Constraints | Description |
|---|---|---|---|
| Id | `string` (prefix: `ses_`) | PK, opaque ID | Unique session identifier |
| UserId | `string` | FK to User, NOT NULL | Owning user |
| TenantId | `string` | Nullable | Tenant context (null for platform-level sessions) |
| Status | `SessionStatus` (enum) | NOT NULL | Active, Revoked, Expired |
| DeviceInfo | `DeviceInfo` (Value Object) | NOT NULL | User-Agent, IP address, device fingerprint |
| RefreshTokenId | `string` | FK to RefreshToken, Nullable | Current active refresh token |
| CreatedAt | `DateTimeOffset` | NOT NULL | Session creation timestamp (UTC) |
| LastActivityAt | `DateTimeOffset` | NOT NULL | Last request timestamp (UTC) |
| ExpiresAt | `DateTimeOffset` | NOT NULL | Session expiration (idle timeout: 24h) |

**Value Objects**:
- `DeviceInfo`: Contains `UserAgent` (string, max 500), `IpAddress` (string, max 45 for IPv6), `DeviceFingerprint` (string, nullable, max 256).

**Business Rules**:
- A user can have multiple active sessions (multi-device support).
- Sessions expire after 24 hours of inactivity (sliding expiration).
- Revoking a session immediately invalidates the associated refresh token.
- Session creation logs an audit event with device info.

---

### RefreshToken

Bound to a session. Supports token rotation.

| Property | Type | Constraints | Description |
|---|---|---|---|
| Id | `string` | PK, opaque ID | Unique token identifier |
| SessionId | `string` | FK to Session, NOT NULL | Owning session |
| TokenHash | `string` | NOT NULL, max 256 | BCrypt hash of the opaque token |
| Status | `RefreshTokenStatus` (enum) | NOT NULL | Active, Used, Revoked |
| CreatedAt | `DateTimeOffset` | NOT NULL | Token creation timestamp (UTC) |
| ExpiresAt | `DateTimeOffset` | NOT NULL | Token expiration (30 days) |
| RevokedAt | `DateTimeOffset` | Nullable | When token was revoked/rotated |

**Business Rules**:
- On refresh, the current token is marked `Used` and a new token is issued (rotation).
- If a `Used` token is presented again, the entire session is revoked (replay detection).
- Expired tokens are cleaned up by a background job.

---

### OtpChallenge (Aggregate Root)

Tracks a phone OTP verification attempt.

| Property | Type | Constraints | Description |
|---|---|---|---|
| Id | `string` (prefix: `otp_`) | PK, opaque ID | Unique challenge identifier |
| PhoneNumber | `string` | NOT NULL, E.164, max 20 | Target phone number |
| CodeHash | `string` | NOT NULL, max 256 | BCrypt hash of the 6-digit OTP code |
| Status | `OtpChallengeStatus` (enum) | NOT NULL | Pending, Verified, Expired, Locked |
| FailedAttempts | `int` | NOT NULL, default 0 | Failed verification count (max 3) |
| CreatedAt | `DateTimeOffset` | NOT NULL | Challenge creation timestamp (UTC) |
| ExpiresAt | `DateTimeOffset` | NOT NULL | Challenge expiration (15 minutes) |

**Business Rules**:
- Max 3 failed attempts per challenge; after 3 failures, status transitions to `Locked`.
- Challenge expires after 15 minutes regardless of attempt count.
- Creating a new challenge for the same phone number does NOT invalidate previous challenges (they expire naturally).
- Rate limit: max 3 OTP challenges per phone number per hour.

---

### GuestMagicLink (Aggregate Root)

A tokenized link for guest access to a specific room occurrence.

| Property | Type | Constraints | Description |
|---|---|---|---|
| Id | `string` (prefix: `lnk_`) | PK, opaque ID | Unique link identifier |
| RoomOccurrenceId | `string` | NOT NULL | Target room occurrence |
| CreatedBy | `string` | FK to User, NOT NULL | Organizer who created the link |
| TokenHash | `string` | NOT NULL, max 64 | SHA256 hash of the 32-byte random token |
| Status | `MagicLinkStatus` (enum) | NOT NULL | Active, Revoked, Expired |
| ExpiresAt | `DateTimeOffset` | NOT NULL | Link expiration (default 7 days) |
| UsageCount | `int` | NOT NULL, default 0 | Number of times the link has been used |

**Business Rules**:
- Token is 32 bytes of cryptographic randomness, Base64URL-encoded for URL safety.
- SHA256 hash is stored (not bcrypt) because tokens are high-entropy; SHA256 enables indexed lookup.
- Organizer can revoke the link at any time.
- Usage count tracks how many guests have used the link (no single-use limit by default).

---

### PersonalAccessToken (Aggregate Root)

Scoped API token for developer/programmatic access.

| Property | Type | Constraints | Description |
|---|---|---|---|
| Id | `string` (prefix: `pat_`) | PK, opaque ID | Unique PAT identifier |
| UserId | `string` | FK to User, NOT NULL | Owning user |
| TenantId | `string` | NOT NULL | Tenant scope |
| Name | `string` | NOT NULL, max 100 | Human-readable token name |
| TokenHash | `string` | NOT NULL, max 256 | BCrypt hash of the token value |
| Scopes | `List<string>` | NOT NULL | Permission scopes (e.g., `rooms:read`, `rooms:write`) |
| Status | `PatStatus` (enum) | NOT NULL | Active, Revoked |
| CreatedAt | `DateTimeOffset` | NOT NULL | Token creation timestamp (UTC) |
| ExpiresAt | `DateTimeOffset` | NOT NULL | Token expiration |
| LastUsedAt | `DateTimeOffset` | Nullable | Most recent API call using this PAT |

**Business Rules**:
- Plaintext token is returned exactly once at creation time. Only the bcrypt hash is stored.
- PATs are scoped to a specific tenant; they cannot cross tenant boundaries.
- Scopes are validated against a predefined set of allowed permissions.
- `LastUsedAt` is updated asynchronously (fire-and-forget or background job) to avoid write amplification on every API call.

---

### PasswordResetToken

One-time-use token for password recovery.

| Property | Type | Constraints | Description |
|---|---|---|---|
| Id | `string` (prefix: `prt_`) | PK, opaque ID | Unique token identifier |
| UserId | `string` | FK to User, NOT NULL | Target user |
| TokenHash | `string` | NOT NULL, max 64 | SHA256 hash of the reset token |
| Status | `PasswordResetTokenStatus` (enum) | NOT NULL | Active, Used, Expired |
| CreatedAt | `DateTimeOffset` | NOT NULL | Token creation timestamp (UTC) |
| ExpiresAt | `DateTimeOffset` | NOT NULL | Token expiration (1 hour) |

**Business Rules**:
- Token is 32 bytes of cryptographic randomness, SHA256-hashed for storage.
- One-time use: after successful password reset, status transitions to `Used`.
- Creating a new reset token does NOT invalidate previous active tokens (they expire at 1 hour).
- Rate limit: max 3 password reset requests per email per hour.

---

### EmailVerificationToken

One-time-use token for email address confirmation.

| Property | Type | Constraints | Description |
|---|---|---|---|
| Id | `string` (prefix: `evt_`) | PK, opaque ID | Unique token identifier |
| UserId | `string` | FK to User, NOT NULL | Target user |
| TokenHash | `string` | NOT NULL, max 64 | SHA256 hash of the verification token |
| Status | `EmailVerificationTokenStatus` (enum) | NOT NULL | Active, Used, Expired |
| CreatedAt | `DateTimeOffset` | NOT NULL | Token creation timestamp (UTC) |
| ExpiresAt | `DateTimeOffset` | NOT NULL | Token expiration (24 hours) |

**Business Rules**:
- Token is 32 bytes of cryptographic randomness, SHA256-hashed for storage.
- One-time use: after successful verification, status transitions to `Used`.
- Requesting a new verification email invalidates all previous active tokens for that user.

---

## State Transition Diagrams

### User Status

```
                    ┌──────────────┐
                    │  Unverified  │
                    └──────┬───────┘
                           │ EmailVerified
                           ▼
                    ┌──────────────┐
              ┌────►│    Active    │◄────┐
              │     └──────┬───────┘     │
              │            │             │
              │ Unsuspend  │ Suspend     │ Unsuspend
              │            ▼             │
              │     ┌──────────────┐     │
              └─────│  Suspended   │─────┘
                    └──────┬───────┘
                           │ Delete
                           ▼
                    ┌──────────────┐
                    │   Deleted    │
                    └──────────────┘
```

**Allowed Transitions**:
| From | To | Trigger |
|---|---|---|
| Unverified | Active | Email verification completed |
| Active | Suspended | Admin suspends user |
| Suspended | Active | Admin unsuspends user |
| Active | Deleted | User self-deletes or admin deletes |
| Suspended | Deleted | Admin deletes suspended user |

**Forbidden Transitions**:
- `Unverified` cannot transition directly to `Suspended` or `Deleted`.
- `Deleted` is a terminal state; no transitions out.

### Session Status

```
        ┌──────────────┐
        │    Active    │
        └──────┬───┬───┘
               │   │
     Revoke    │   │  Idle timeout / Token expiry
               ▼   ▼
    ┌─────────┐   ┌─────────┐
    │ Revoked │   │ Expired │
    └─────────┘   └─────────┘
```

**Allowed Transitions**:
| From | To | Trigger |
|---|---|---|
| Active | Revoked | User revokes session, admin revokes, or password change |
| Active | Expired | 24-hour idle timeout or refresh token expiry |

**Forbidden Transitions**:
- `Revoked` and `Expired` are terminal states; no transitions out.

---

## SQL Schema Definition

```sql
-- ============================================================
-- Identity Module Schema
-- Schema: [identity]
-- ============================================================

CREATE SCHEMA [identity];
GO

-- ----------------------------------------------------------
-- Users
-- ----------------------------------------------------------
CREATE TABLE [identity].[Users] (
    [Id]                 NVARCHAR(30)     NOT NULL,
    [Email]              NVARCHAR(256)    NOT NULL,
    [PasswordHash]       NVARCHAR(256)    NOT NULL,
    [PhoneNumber]        NVARCHAR(20)     NULL,
    [Status]             TINYINT          NOT NULL,  -- 0=Unverified, 1=Active, 2=Suspended, 3=Deleted
    [FullName]           NVARCHAR(200)    NOT NULL,
    [ProfilePictureUrl]  NVARCHAR(2048)   NULL,
    [CreatedAt]          DATETIMEOFFSET   NOT NULL,
    [UpdatedAt]          DATETIMEOFFSET   NOT NULL,
    [LastLoginAt]        DATETIMEOFFSET   NULL,

    CONSTRAINT [PK_Users] PRIMARY KEY CLUSTERED ([Id]),
    CONSTRAINT [UQ_Users_Email] UNIQUE ([Email])
);

CREATE NONCLUSTERED INDEX [IX_Users_Email]
    ON [identity].[Users] ([Email])
    INCLUDE ([PasswordHash], [Status]);

CREATE NONCLUSTERED INDEX [IX_Users_PhoneNumber]
    ON [identity].[Users] ([PhoneNumber])
    WHERE [PhoneNumber] IS NOT NULL;

CREATE NONCLUSTERED INDEX [IX_Users_Status]
    ON [identity].[Users] ([Status]);

-- ----------------------------------------------------------
-- Sessions
-- ----------------------------------------------------------
CREATE TABLE [identity].[Sessions] (
    [Id]                 NVARCHAR(30)     NOT NULL,
    [UserId]             NVARCHAR(30)     NOT NULL,
    [TenantId]           NVARCHAR(30)     NULL,
    [Status]             TINYINT          NOT NULL,  -- 0=Active, 1=Revoked, 2=Expired
    [UserAgent]          NVARCHAR(500)    NOT NULL,
    [IpAddress]          NVARCHAR(45)     NOT NULL,
    [DeviceFingerprint]  NVARCHAR(256)    NULL,
    [RefreshTokenId]     NVARCHAR(30)     NULL,
    [CreatedAt]          DATETIMEOFFSET   NOT NULL,
    [LastActivityAt]     DATETIMEOFFSET   NOT NULL,
    [ExpiresAt]          DATETIMEOFFSET   NOT NULL,

    CONSTRAINT [PK_Sessions] PRIMARY KEY CLUSTERED ([Id]),
    CONSTRAINT [FK_Sessions_Users] FOREIGN KEY ([UserId])
        REFERENCES [identity].[Users] ([Id])
);

CREATE NONCLUSTERED INDEX [IX_Sessions_UserId_Status]
    ON [identity].[Sessions] ([UserId], [Status])
    INCLUDE ([CreatedAt], [LastActivityAt], [ExpiresAt]);

CREATE NONCLUSTERED INDEX [IX_Sessions_ExpiresAt]
    ON [identity].[Sessions] ([ExpiresAt])
    WHERE [Status] = 0;  -- Active sessions only

-- ----------------------------------------------------------
-- RefreshTokens
-- ----------------------------------------------------------
CREATE TABLE [identity].[RefreshTokens] (
    [Id]                 NVARCHAR(30)     NOT NULL,
    [SessionId]          NVARCHAR(30)     NOT NULL,
    [TokenHash]          NVARCHAR(256)    NOT NULL,
    [Status]             TINYINT          NOT NULL,  -- 0=Active, 1=Used, 2=Revoked
    [CreatedAt]          DATETIMEOFFSET   NOT NULL,
    [ExpiresAt]          DATETIMEOFFSET   NOT NULL,
    [RevokedAt]          DATETIMEOFFSET   NULL,

    CONSTRAINT [PK_RefreshTokens] PRIMARY KEY CLUSTERED ([Id]),
    CONSTRAINT [FK_RefreshTokens_Sessions] FOREIGN KEY ([SessionId])
        REFERENCES [identity].[Sessions] ([Id])
);

CREATE NONCLUSTERED INDEX [IX_RefreshTokens_SessionId_Status]
    ON [identity].[RefreshTokens] ([SessionId], [Status]);

-- ----------------------------------------------------------
-- OtpChallenges
-- ----------------------------------------------------------
CREATE TABLE [identity].[OtpChallenges] (
    [Id]                 NVARCHAR(30)     NOT NULL,
    [PhoneNumber]        NVARCHAR(20)     NOT NULL,
    [CodeHash]           NVARCHAR(256)    NOT NULL,
    [Status]             TINYINT          NOT NULL,  -- 0=Pending, 1=Verified, 2=Expired, 3=Locked
    [FailedAttempts]     INT              NOT NULL   DEFAULT 0,
    [CreatedAt]          DATETIMEOFFSET   NOT NULL,
    [ExpiresAt]          DATETIMEOFFSET   NOT NULL,

    CONSTRAINT [PK_OtpChallenges] PRIMARY KEY CLUSTERED ([Id])
);

CREATE NONCLUSTERED INDEX [IX_OtpChallenges_PhoneNumber_Status]
    ON [identity].[OtpChallenges] ([PhoneNumber], [Status])
    INCLUDE ([CreatedAt]);

-- ----------------------------------------------------------
-- GuestMagicLinks
-- ----------------------------------------------------------
CREATE TABLE [identity].[GuestMagicLinks] (
    [Id]                 NVARCHAR(30)     NOT NULL,
    [RoomOccurrenceId]   NVARCHAR(30)     NOT NULL,
    [CreatedBy]          NVARCHAR(30)     NOT NULL,
    [TokenHash]          NVARCHAR(64)     NOT NULL,
    [Status]             TINYINT          NOT NULL,  -- 0=Active, 1=Revoked, 2=Expired
    [ExpiresAt]          DATETIMEOFFSET   NOT NULL,
    [UsageCount]         INT              NOT NULL   DEFAULT 0,

    CONSTRAINT [PK_GuestMagicLinks] PRIMARY KEY CLUSTERED ([Id]),
    CONSTRAINT [FK_GuestMagicLinks_Users] FOREIGN KEY ([CreatedBy])
        REFERENCES [identity].[Users] ([Id])
);

CREATE UNIQUE NONCLUSTERED INDEX [IX_GuestMagicLinks_TokenHash]
    ON [identity].[GuestMagicLinks] ([TokenHash]);

CREATE NONCLUSTERED INDEX [IX_GuestMagicLinks_RoomOccurrenceId]
    ON [identity].[GuestMagicLinks] ([RoomOccurrenceId])
    WHERE [Status] = 0;  -- Active links only

-- ----------------------------------------------------------
-- PersonalAccessTokens
-- ----------------------------------------------------------
CREATE TABLE [identity].[PersonalAccessTokens] (
    [Id]                 NVARCHAR(30)     NOT NULL,
    [UserId]             NVARCHAR(30)     NOT NULL,
    [TenantId]           NVARCHAR(30)     NOT NULL,
    [Name]               NVARCHAR(100)    NOT NULL,
    [TokenHash]          NVARCHAR(256)    NOT NULL,
    [Scopes]             NVARCHAR(MAX)    NOT NULL,  -- JSON array of scope strings
    [Status]             TINYINT          NOT NULL,  -- 0=Active, 1=Revoked
    [CreatedAt]          DATETIMEOFFSET   NOT NULL,
    [ExpiresAt]          DATETIMEOFFSET   NOT NULL,
    [LastUsedAt]         DATETIMEOFFSET   NULL,

    CONSTRAINT [PK_PersonalAccessTokens] PRIMARY KEY CLUSTERED ([Id]),
    CONSTRAINT [FK_PersonalAccessTokens_Users] FOREIGN KEY ([UserId])
        REFERENCES [identity].[Users] ([Id])
);

CREATE NONCLUSTERED INDEX [IX_PersonalAccessTokens_UserId_Status]
    ON [identity].[PersonalAccessTokens] ([UserId], [Status])
    INCLUDE ([TenantId], [Name], [ExpiresAt]);

-- ----------------------------------------------------------
-- PasswordResetTokens
-- ----------------------------------------------------------
CREATE TABLE [identity].[PasswordResetTokens] (
    [Id]                 NVARCHAR(30)     NOT NULL,
    [UserId]             NVARCHAR(30)     NOT NULL,
    [TokenHash]          NVARCHAR(64)     NOT NULL,
    [Status]             TINYINT          NOT NULL,  -- 0=Active, 1=Used, 2=Expired
    [CreatedAt]          DATETIMEOFFSET   NOT NULL,
    [ExpiresAt]          DATETIMEOFFSET   NOT NULL,

    CONSTRAINT [PK_PasswordResetTokens] PRIMARY KEY CLUSTERED ([Id]),
    CONSTRAINT [FK_PasswordResetTokens_Users] FOREIGN KEY ([UserId])
        REFERENCES [identity].[Users] ([Id])
);

CREATE NONCLUSTERED INDEX [IX_PasswordResetTokens_TokenHash]
    ON [identity].[PasswordResetTokens] ([TokenHash])
    WHERE [Status] = 0;  -- Active tokens only

CREATE NONCLUSTERED INDEX [IX_PasswordResetTokens_UserId]
    ON [identity].[PasswordResetTokens] ([UserId])
    INCLUDE ([Status], [CreatedAt]);

-- ----------------------------------------------------------
-- EmailVerificationTokens
-- ----------------------------------------------------------
CREATE TABLE [identity].[EmailVerificationTokens] (
    [Id]                 NVARCHAR(30)     NOT NULL,
    [UserId]             NVARCHAR(30)     NOT NULL,
    [TokenHash]          NVARCHAR(64)     NOT NULL,
    [Status]             TINYINT          NOT NULL,  -- 0=Active, 1=Used, 2=Expired
    [CreatedAt]          DATETIMEOFFSET   NOT NULL,
    [ExpiresAt]          DATETIMEOFFSET   NOT NULL,

    CONSTRAINT [PK_EmailVerificationTokens] PRIMARY KEY CLUSTERED ([Id]),
    CONSTRAINT [FK_EmailVerificationTokens_Users] FOREIGN KEY ([UserId])
        REFERENCES [identity].[Users] ([Id])
);

CREATE UNIQUE NONCLUSTERED INDEX [IX_EmailVerificationTokens_TokenHash]
    ON [identity].[EmailVerificationTokens] ([TokenHash])
    WHERE [Status] = 0;  -- Active tokens only

CREATE NONCLUSTERED INDEX [IX_EmailVerificationTokens_UserId]
    ON [identity].[EmailVerificationTokens] ([UserId])
    INCLUDE ([Status], [CreatedAt]);
```

### Index Strategy Notes

- **Filtered indexes** on `Status = 0` (Active) are used where queries predominantly target active records (sessions, tokens, magic links). This reduces index size and improves query performance.
- **INCLUDE columns** on covering indexes avoid key lookups for common query patterns (e.g., login validates email + password hash + status in a single index seek).
- **TokenHash unique indexes** on GuestMagicLinks and EmailVerificationTokens enable O(1) token lookup during validation.
- **Composite indexes** (e.g., `UserId + Status`) support the most common query patterns: "list active sessions for user", "list active PATs for user".
