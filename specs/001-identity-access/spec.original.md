# Epic 1: Identity & Access Module

**Version:** 1.0
**Status:** Specification
**Last Updated:** 2026-04-03
**Owner:** Platform / Security

---

## Overview

This epic implements a comprehensive identity and access management system for Muntada, supporting multiple authentication methods (email+password, phone OTP), session management, token lifecycle, guest access, and Personal Access Tokens (PATs) for API integration. The module provides the foundation for all subsequent modules that require user context and authorization.

### Scope

- User registration (email+password, phone OTP)
- Login (email+password, phone OTP challenge/verify)
- JWT access token issuance (short-lived, ~15 min) + refresh token lifecycle (bound to revocable sessions)
- Session management (list, revoke, device tracking)
- OTP challenge/verify flow with rate limiting and expiration
- Guest magic link generation and redemption (listen-only access)
- Organization/tenant switching
- Personal Access Token (PAT) management (hash-only storage, scoped to tenant+actions)
- Password reset flow with secure link expiration
- Rate limiting on authentication endpoints (brute force protection)

### Dependencies

- **Shared Kernel** (Epic 0) for base entity types, ID generation, event publishing
- **Redis** for session store, rate limit counters, OTP cache
- **SQL Server** for persistent identity data
- **RabbitMQ** for integration events (UserCreated, PasswordChanged, etc.)

---

## User Stories

### US-1.1: User Registration with Email & Password
**Priority:** P1
**Story Points:** 8
**Owner:** Backend / Security

As a new user, I want to register with an email and password so that I can create an account and access the platform.

#### Acceptance Criteria

**Given** I am on the registration page
**When** I enter a valid email (RFC 5322), password (minimum 12 chars, 1 uppercase, 1 number, 1 special char), and confirm password
**Then** the form validates locally and I can submit

**Given** I submit the registration form
**When** the backend validates the input
**Then** if the email is already registered, I receive error "Email already in use" (without disclosing if account exists, for privacy)

**Given** all validation passes
**When** the user is created
**Then** a User record is stored with:
- `Id` (opaque ID, prefix `usr_`)
- `Email` (lowercase, trimmed, unique across system)
- `PasswordHash` (bcrypt with cost factor 12)
- `Status: Unverified` (until email verification)
- `CreatedAt, UpdatedAt`

**Given** the user is created
**When** an email verification link is sent
**Then** the user receives an email with a time-limited link (valid for 24 hours) to verify their email address

**Given** the user clicks the verification link
**When** the token is validated
**Then** the `User.Status` transitions to `Active` and they can now log in

**Given** a user forgets to verify within 24 hours
**When** they attempt to log in with an unverified email
**Then** they receive error "Please verify your email first" with option to resend verification link

#### Definition of Done
- User registration endpoint: `POST /api/identity/auth/register`
- Email validation (RFC 5322)
- Password validation rules (12+ chars, complexity)
- Email verification flow with token generation/validation
- Duplicate email check with safe error message
- Account lockout after failed verification attempts (after 5 attempts, email blocked for 1 hour)
- Integration event: `UserRegistered` published to RabbitMQ
- Unit tests for all validation rules and edge cases
- Serilog structured logs for security events

---

### US-1.2: Login with Email & Password
**Priority:** P1
**Story Points:** 13
**Owner:** Backend / Security

As a registered user, I want to log in with my email and password so that I can authenticate and access my account.

#### Acceptance Criteria

**Given** I am on the login page
**When** I enter my email and password
**Then** the form validates that both fields are present and submits

**Given** I submit the login form
**When** the backend processes the request
**Then** the system checks for rate limiting (max 5 failed attempts per email per 15 minutes)

**Given** the rate limit is not exceeded
**When** the email exists and password is correct
**Then** a Session record is created with:
- `Id` (opaque ID, prefix `ses_`)
- `UserId` (reference to User)
- `Status: Active`
- `DeviceInfo: { UserAgent, IpAddress, Timestamp }`
- `RefreshTokenId` (reference to RefreshToken)
- `ExpiresAt` (session lifetime, configurable, default 30 days)
- `CreatedAt`

**Given** a valid session is created
**When** JWT tokens are issued
**Then** I receive:
- `accessToken` (JWT, valid for 15 minutes, contains `sub` [user ID], `aud` [tenant ID], `scope`)
- `refreshToken` (HTTP-only cookie, valid for 30 days, bound to session)

**Given** email or password is incorrect
**When** login fails
**Then** a generic error is returned "Invalid email or password" (no information leakage), and the failed attempt is logged

**Given** the account is locked due to too many failed attempts
**When** the user tries to log in
**Then** they receive error "Too many failed attempts. Please try again in 15 minutes." and a message "Forgot password?" link is shown

**Given** I successfully log in
**When** I navigate to a protected resource
**Then** my access token is sent in the `Authorization: Bearer <token>` header

#### Definition of Done
- Login endpoint: `POST /api/identity/auth/login`
- Rate limiting middleware (5 failed attempts per 15 minutes per email)
- Session creation and storage in SQL Server
- JWT token generation (HS256 or RS256 with kid)
- Refresh token generation and storage (opaque, hashed for security)
- Device tracking (User-Agent, IP, timestamp)
- Failed login logging and alerting
- Session validation on each protected request
- Unit and integration tests for authentication flow
- Secure token storage (refresh token as HTTP-only cookie)

---

### US-1.3: Token Refresh & Session Management
**Priority:** P1
**Story Points:** 13
**Owner:** Backend / Security

As a user with an active session, I want my access token to automatically refresh when it expires so that I remain logged in without re-entering credentials.

#### Acceptance Criteria

**Given** I have an active session with a valid refresh token
**When** my access token expires (after 15 minutes)
**Then** the frontend detects the expiration (401 response) and automatically requests a new access token

**Given** the frontend sends a refresh request with the refresh token
**When** the backend validates the refresh token
**Then** it checks:
- Token is in the database and not revoked
- Token hash matches the stored hash (bcrypt)
- Session is still Active
- Session has not expired
- IP address matches (or within configurable geo-distance)

**Given** all validations pass
**When** a new access token is issued
**Then** I receive a new JWT with the same claims but fresh expiration timestamp

**Given** the refresh token is invalid or session is revoked
**When** the refresh request is made
**Then** a 401 error is returned and the user is redirected to login

**Given** I want to see all my active sessions
**When** I call `GET /api/identity/sessions`
**Then** I receive a list of active sessions with:
- `sessionId`
- `deviceInfo` (User-Agent, IP, approximate location)
- `createdAt`
- `lastActivityAt`
- `isCurrent` (flag indicating current session)

**Given** I want to revoke a specific session
**When** I call `DELETE /api/identity/sessions/{sessionId}`
**Then** the session is marked as Revoked and the refresh token is invalidated

**Given** I want to revoke all other sessions (e.g., lost device)
**When** I call `DELETE /api/identity/sessions?exceptCurrent=true`
**Then** all sessions except the current one are revoked

**Given** a session is idle for 24 hours with no activity
**When** the background job runs
**Then** the session is marked as Expired and refresh token is invalidated

#### Definition of Done
- Refresh endpoint: `POST /api/identity/auth/refresh`
- Session list endpoint: `GET /api/identity/sessions`
- Session revoke endpoint: `DELETE /api/identity/sessions/{sessionId}`
- Refresh token validation logic (hash comparison, expiration, revocation)
- Session activity tracking (update `lastActivityAt` on each request)
- Idle session cleanup background job
- IP address tracking and geolocation (optional)
- Rate limiting on refresh endpoint (prevent token exhaustion)
- Unit and integration tests for session lifecycle
- Secure cookie handling (SameSite=Strict, Secure, HttpOnly)

---

### US-1.4: Phone OTP Authentication
**Priority:** P1
**Story Points:** 21
**Owner:** Backend / Security

As a user, I want to log in with phone OTP so that I can authenticate without a password (passwordless auth).

#### Acceptance Criteria

**Given** I am on the login page
**When** I select "Sign in with phone" and enter a valid phone number (E.164 format: +<country-code><number>)
**Then** the form validates the phone number and allows me to proceed

**Given** I submit the phone number
**When** the system processes the request
**Then** an OtpChallenge record is created with:
- `Id` (opaque ID, prefix `otp_`)
- `PhoneNumber` (E.164 format)
- `Code` (6-digit numeric code, generated via secure random)
- `Status: Pending`
- `Attempts: 0`
- `CreatedAt`
- `ExpiresAt` (15 minutes from creation)

**Given** the OTP challenge is created
**When** an SMS is sent
**Then** the user receives an SMS with the message: "Your Muntada verification code is: 123456. Valid for 15 minutes."

**Given** I receive the SMS
**When** I enter the 6-digit code
**Then** the system validates:
- Code matches the challenge
- Challenge has not expired
- Attempt count < 3 (after 3 failed attempts, challenge is locked for 15 minutes)

**Given** the code is valid
**When** I submit the verification
**Then** the OtpChallenge status transitions to `Verified` and a Session is created (same as email+password login)

**Given** I enter an incorrect code
**When** I submit
**Then** the attempt count increments, and I receive error "Invalid code" with remaining attempts displayed

**Given** I exceed 3 failed attempts
**When** I try to verify again
**Then** I receive error "Too many attempts. Request a new code." and the challenge is locked

**Given** I don't complete OTP verification within 15 minutes
**When** the challenge expires
**Then** an expired challenge is cleaned up and the user is asked to restart the login

#### Definition of Done
- OTP challenge endpoint: `POST /api/identity/auth/otp/challenge`
- OTP verification endpoint: `POST /api/identity/auth/otp/verify`
- OTP code generation (cryptographically secure random)
- OTP storage with expiration (Redis or SQL with TTL)
- SMS gateway integration (Twilio, AWS SNS, or local SMS provider)
- Attempt tracking and rate limiting
- Challenge lockout after 3 failed attempts
- Cleanup job for expired challenges
- Unit and integration tests for OTP flow
- Audit logging of OTP attempts (security risk)
- No logging of OTP codes (security requirement)

---

### US-1.5: Guest Magic Link Access
**Priority:** P2
**Story Points:** 13
**Owner:** Backend / Community

As a room organizer, I want to generate magic links that allow guests to join audio rooms without creating an account so that I can invite external participants with minimal friction.

#### Acceptance Criteria

**Given** I am a room organizer
**When** I request to generate a guest magic link for a specific room
**Then** a GuestMagicLink record is created with:
- `Id` (opaque ID, prefix `lnk_`)
- `RoomOccurrenceId` (reference to the room)
- `Token` (cryptographically secure random, URL-safe, 32 bytes)
- `TokenHash` (SHA256 hash for storage)
- `Status: Active`
- `ExpiresAt` (configurable, default 7 days)
- `CreatedBy` (organizer user ID)
- `CreatedAt`

**Given** the magic link is generated
**When** I send the link to a guest
**Then** the guest receives a URL like `https://muntada.com/join?token=abc123...`

**Given** the guest clicks the link
**When** the system validates the token
**Then** it checks:
- Token hash matches a stored link
- Link is Active (not revoked)
- Link has not expired
- Room is Scheduled or Live

**Given** all validations pass
**When** the guest accesses the room
**Then** they are granted:
- `GuestSession` (temporary, no persistent account)
- `Permissions: [Listen]` (read-only, cannot speak)
- Access token (valid for room duration + 1 hour)

**Given** the guest joins the room
**When** they attempt to speak or perform other actions
**Then** they receive error "Guest access is read-only" (or equivalent)

**Given** I want to revoke the guest link
**When** I call `DELETE /api/identity/magic-links/{linkId}`
**Then** the link status transitions to Revoked and new guests cannot access it

**Given** a guest link expires after 7 days
**When** the background job runs
**Then** the link status transitions to Expired

#### Definition of Done
- Magic link generation endpoint: `POST /api/identity/magic-links`
- Magic link validation endpoint: `GET /api/identity/magic-links/validate?token=...`
- Magic link revocation endpoint: `DELETE /api/identity/magic-links/{linkId}`
- Token generation (cryptographically secure, 32+ bytes)
- Token hashing (SHA256) for storage
- Guest session creation
- Permission enforcement (listen-only)
- Expiration and cleanup jobs
- Unit and integration tests
- Audit logging of magic link generation and usage
- Rate limiting on magic link generation (max 10 per room per day)

---

### US-1.6: Personal Access Token (PAT) Management
**Priority:** P2
**Story Points:** 13
**Owner:** Backend / Integration

As a developer, I want to create Personal Access Tokens so that I can integrate with Muntada APIs without sharing my credentials.

#### Acceptance Criteria

**Given** I am a user
**When** I navigate to settings and request a new Personal Access Token
**Then** a dialog appears asking for:
- Token name (e.g., "Mobile App Integration")
- Scope (list of permissions, e.g., `rooms:read`, `rooms:write`, `users:read`)
- Expiration (7 days, 30 days, 90 days, or custom)

**Given** I submit the creation request
**When** the backend processes it
**Then** a PersonalAccessToken record is created with:
- `Id` (opaque ID, prefix `pat_`)
- `UserId` (reference to User)
- `TenantId` (reference to Tenant, scoped to current tenant)
- `Name`
- `TokenHash` (bcrypt hash, never store plaintext)
- `Scopes` (list of granted permissions)
- `Status: Active`
- `CreatedAt`
- `ExpiresAt`
- `LastUsedAt` (nullable)

**Given** the token is created
**When** the response is sent
**Then** the plaintext token is displayed ONCE with warning "Save this token securely. You won't see it again." and the user is prompted to copy it

**Given** the user closes the dialog without saving
**When** they navigate away
**Then** the token is no longer retrievable (only hash is stored)

**Given** I have a PAT
**When** I use it to authenticate an API request with `Authorization: Bearer <pat_token>`
**Then** the backend validates:
- Token hash matches a stored PAT
- Token is Active (not revoked)
- Token has not expired
- Scopes include the requested action

**Given** the PAT is valid
**When** the request is processed
**Then** the context includes:
- `UserId` (from PAT)
- `TenantId` (from PAT, enforced isolation)
- `Scopes` (for authorization checks)

**Given** I want to revoke a PAT
**When** I call `DELETE /api/identity/pats/{patId}`
**Then** the token status transitions to Revoked and it can no longer be used

**Given** I want to list all my PATs
**When** I call `GET /api/identity/pats`
**Then** I receive a list of PATs with:
- `id`
- `name`
- `scopes`
- `createdAt`
- `expiresAt`
- `lastUsedAt` (if applicable)

#### Definition of Done
- PAT creation endpoint: `POST /api/identity/pats`
- PAT list endpoint: `GET /api/identity/pats`
- PAT revocation endpoint: `DELETE /api/identity/pats/{patId}`
- Token hash generation (bcrypt) and validation
- Scope-based authorization (middleware)
- Token expiration tracking
- `LastUsedAt` tracking for monitoring
- Audit logging of PAT creation, use, and revocation
- Rate limiting on token creation (max 50 per user per tenant)
- Unit and integration tests
- Documentation with examples

---

### US-1.7: Password Reset Flow
**Priority:** P2
**Story Points:** 8
**Owner:** Backend / Security

As a user, I want to reset my password if I forget it so that I can regain access to my account.

#### Acceptance Criteria

**Given** I am on the login page
**When** I click "Forgot password?"
**Then** I am presented with a form asking for my email address

**Given** I enter my email
**When** I submit the form
**Then** the system checks if the email exists (without disclosing if account exists)

**Given** the email exists
**When** the request is processed
**Then** a PasswordResetToken record is created with:
- `Id` (opaque ID, prefix `prt_`)
- `UserId` (reference to User)
- `Token` (cryptographically secure random, 32 bytes)
- `TokenHash` (SHA256)
- `Status: Pending`
- `CreatedAt`
- `ExpiresAt` (1 hour from creation)

**Given** the token is created
**When** an email is sent
**Then** the user receives an email with link: `https://muntada.com/reset-password?token=abc123...`

**Given** the user clicks the reset link
**When** the system validates the token
**Then** it checks token exists, matches hash, is Pending, and not expired

**Given** validation passes
**When** the user enters a new password
**Then** the password is validated (same rules as registration)

**Given** the new password is valid
**When** the user submits
**Then** the User.PasswordHash is updated and PasswordResetToken status transitions to Used

**Given** the password reset is successful
**When** the user is redirected
**Then** they can log in with their new password

**Given** the reset token expires after 1 hour
**When** the user tries to use it
**Then** they receive error "Reset link has expired" and are prompted to request a new one

#### Definition of Done
- Forgot password endpoint: `POST /api/identity/auth/forgot-password`
- Reset password endpoint: `POST /api/identity/auth/reset-password`
- Password reset token generation and validation
- Token expiration (1 hour)
- Email sending integration
- Token usage tracking (one-time use)
- Audit logging of password resets
- Rate limiting (max 3 reset requests per email per hour)
- Unit and integration tests

---

## Functional Requirements

### Registration & Onboarding

**FR-1.1:** The system shall support user registration via email+password with password complexity validation (minimum 12 characters, at least 1 uppercase letter, 1 number, 1 special character).

**FR-1.2:** User email addresses shall be case-insensitive and stored in lowercase. Email uniqueness shall be enforced at the database level with a unique index on `Users.Email`.

**FR-1.3:** Passwords shall be hashed using bcrypt (cost factor 12) and never stored in plaintext. Password hashes shall be salted automatically by bcrypt.

**FR-1.4:** Email verification shall be mandatory for account activation. Verification tokens shall expire after 24 hours. Unverified accounts cannot log in.

### Authentication Methods

**FR-1.5:** The system shall support two authentication methods: (1) email+password, (2) phone OTP. Each method shall have independent flow and rate limiting.

**FR-1.6:** Phone numbers shall be validated as E.164 format (e.g., +966501234567 for Saudi Arabia). International country codes shall be supported.

**FR-1.7:** OTP codes shall be 6 digits, generated via cryptographically secure random (not predictable). OTP shall expire after 15 minutes. Failed verification attempts shall be limited to 3 per challenge.

**FR-1.8:** SMS delivery shall be handled by a configured gateway (Twilio, AWS SNS, or internal provider). Failed SMS sends shall trigger alerts and retries (max 3 retries with exponential backoff).

### Token & Session Management

**FR-1.9:** JWT access tokens shall be issued upon successful authentication. Access tokens shall have a short lifetime (15 minutes by default, configurable). Claims shall include: `sub` (user ID), `aud` (tenant ID), `scope` (permissions), `iat` (issued at), `exp` (expiration).

**FR-1.10:** JWT tokens shall be signed using RS256 (asymmetric) or HS256 (symmetric) with a configurable algorithm. Token signing key rotation shall be supported (kids in token header).

**FR-1.11:** Refresh tokens shall be opaque strings (not JWTs) and stored securely in HTTP-only cookies with `Secure` and `SameSite=Strict` flags.

**FR-1.12:** Sessions shall be stored in SQL Server and Redis (for performance). Session state shall include: user ID, tenant ID, device info (User-Agent, IP), creation timestamp, and expiration.

**FR-1.13:** Session revocation shall be immediate (token blacklist stored in Redis). Revoked sessions shall reject any refresh attempts.

**FR-1.14:** Rate limiting shall be enforced on login endpoints (max 5 failed attempts per email per 15 minutes). Rate limiting state shall be stored in Redis with automatic expiration.

### Guest Access & Magic Links

**FR-1.15:** Magic links shall be generated for guest access to specific rooms. Magic links shall have a configurable expiration (default 7 days).

**FR-1.16:** Guest sessions shall grant read-only permissions by default. Guest permissions shall be enforced at the authorization layer on every request.

**FR-1.17:** Magic links shall be one-time use if configured. Each link usage shall be logged for audit trails.

### Personal Access Tokens (PAT)

**FR-1.18:** Personal Access Tokens shall be scoped to a user and tenant. Tokens shall not cross tenant boundaries. Token scopes shall define granular permissions (e.g., `rooms:read`, `rooms:write`, `users:read`).

**FR-1.19:** PAT tokens shall be hashed using bcrypt (cost factor 12) before storage. Plaintext tokens shall only be displayed once at creation.

**FR-1.20:** PAT expiration shall be tracked and enforced. Expired tokens shall be rejected with clear error messages. PAT usage shall be logged for audit purposes.

### Security & Compliance

**FR-1.21:** All authentication-related errors shall use generic messages ("Invalid email or password") without disclosing account existence (prevents user enumeration attacks).

**FR-1.22:** Failed authentication attempts shall be logged with IP address, User-Agent, timestamp, and email (for security analysis). Logs shall be retained for audit compliance.

**FR-1.23:** Passwords shall never be sent over unencrypted channels. TLS/SSL shall be enforced for all authentication endpoints. HSTS headers shall be present.

**FR-1.24:** Account lockout shall occur after N failed attempts (configurable, default 5) within a time window (configurable, default 15 minutes). Lockout duration shall be configurable (default 1 hour).

**FR-1.25:** The system shall support password reset flow with time-limited tokens (1 hour). Reset tokens shall be one-time use and invalidated after first use.

### Integration Events

**FR-1.26:** The following integration events shall be published to RabbitMQ:
- `UserRegistered` (when account is created)
- `UserEmailVerified` (when email verification completes)
- `UserLoggedIn` (when session is created)
- `UserLoggedOut` (when session is revoked)
- `PasswordChanged` (when password is updated)
- `SessionRevoked` (when single session is revoked)
- `PATCreated` (when PAT is generated)
- `PATRevoked` (when PAT is revoked)

---

## Key Entities

### User

```csharp
public class User : AggregateRoot<UserId>
{
    public Email Email { get; set; }                          // Unique, lowercase
    public PasswordHash PasswordHash { get; set; }            // bcrypt hash only
    public UserStatus Status { get; set; }                    // Unverified, Active, Suspended, Deleted
    public PhoneNumber? PhoneNumber { get; set; }             // E.164 format, optional
    public string? FullName { get; set; }
    public string? ProfilePictureUrl { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }
    public DateTime? LastLoginAt { get; set; }
    public ICollection<Session> Sessions { get; set; }
    public ICollection<PersonalAccessToken> PATs { get; set; }
}

public enum UserStatus
{
    Unverified,     // Email not yet verified
    Active,         // Fully activated
    Suspended,      // Temporarily disabled (by admin or security)
    Deleted         // Soft-delete
}
```

### Credential

```csharp
public class Credential : Entity<CredentialId>
{
    public UserId UserId { get; set; }
    public CredentialType Type { get; set; }               // Password, OTP, MagicLink, PAT
    public string CredentialHash { get; set; }             // Bcrypt or PBKDF2 hash
    public CredentialStatus Status { get; set; }           // Active, Revoked, Expired
    public DateTime CreatedAt { get; set; }
    public DateTime? ExpiresAt { get; set; }
    public DateTime? LastUsedAt { get; set; }
    public int FailedAttempts { get; set; }
}

public enum CredentialType
{
    Password,
    OTP,
    MagicLink,
    PAT
}

public enum CredentialStatus
{
    Active,
    Revoked,
    Expired
}
```

### Session

```csharp
public class Session : AggregateRoot<SessionId>
{
    public UserId UserId { get; set; }
    public TenantId? TenantId { get; set; }                 // Current tenant context
    public SessionStatus Status { get; set; }              // Active, Revoked, Expired
    public RefreshTokenId RefreshTokenId { get; set; }
    public DeviceInfo DeviceInfo { get; set; }             // User-Agent, IP, etc.
    public DateTime CreatedAt { get; set; }
    public DateTime? LastActivityAt { get; set; }
    public DateTime ExpiresAt { get; set; }                // 30 days default

    public void Revoke() => Status = SessionStatus.Revoked;
}

public class DeviceInfo
{
    public string UserAgent { get; set; }
    public string IpAddress { get; set; }
    public string? Country { get; set; }                   // From GeoIP lookup
    public DateTime DetectedAt { get; set; }
}

public enum SessionStatus
{
    Active,
    Revoked,
    Expired
}
```

### RefreshToken

```csharp
public class RefreshToken : Entity<RefreshTokenId>
{
    public SessionId SessionId { get; set; }
    public string TokenHash { get; set; }                  // Bcrypt hash (never plaintext)
    public RefreshTokenStatus Status { get; set; }         // Active, Revoked, Expired
    public DateTime CreatedAt { get; set; }
    public DateTime ExpiresAt { get; set; }
    public DateTime? RevokedAt { get; set; }

    public bool IsValid() => Status == RefreshTokenStatus.Active && DateTime.UtcNow < ExpiresAt;
}

public enum RefreshTokenStatus
{
    Active,
    Revoked,
    Expired
}
```

### OtpChallenge

```csharp
public class OtpChallenge : AggregateRoot<OtpChallengeId>
{
    public PhoneNumber PhoneNumber { get; set; }
    public string Code { get; set; }                       // 6-digit code (not hashed)
    public OtpStatus Status { get; set; }                  // Pending, Verified, Expired
    public int FailedAttempts { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime ExpiresAt { get; set; }                // 15 minutes default

    public bool IsValid() => Status == OtpStatus.Pending && DateTime.UtcNow < ExpiresAt && FailedAttempts < 3;
    public void IncrementFailedAttempts() => FailedAttempts++;
}

public enum OtpStatus
{
    Pending,
    Verified,
    Expired
}
```

### GuestMagicLink

```csharp
public class GuestMagicLink : AggregateRoot<GuestMagicLinkId>
{
    public RoomOccurrenceId RoomOccurrenceId { get; set; }
    public UserId CreatedBy { get; set; }
    public string TokenHash { get; set; }                  // SHA256 hash
    public GuestMagicLinkStatus Status { get; set; }       // Active, Revoked, Expired
    public DateTime CreatedAt { get; set; }
    public DateTime ExpiresAt { get; set; }                // 7 days default
    public int UsageCount { get; set; }

    public bool IsValid() => Status == GuestMagicLinkStatus.Active && DateTime.UtcNow < ExpiresAt;
}

public enum GuestMagicLinkStatus
{
    Active,
    Revoked,
    Expired
}
```

### PersonalAccessToken

```csharp
public class PersonalAccessToken : AggregateRoot<PersonalAccessTokenId>
{
    public UserId UserId { get; set; }
    public TenantId TenantId { get; set; }                 // Tenant scope
    public string Name { get; set; }                        // User-provided name
    public string TokenHash { get; set; }                  // Bcrypt hash
    public List<string> Scopes { get; set; }               // List of permissions
    public PatStatus Status { get; set; }                  // Active, Revoked
    public DateTime CreatedAt { get; set; }
    public DateTime ExpiresAt { get; set; }
    public DateTime? LastUsedAt { get; set; }

    public bool IsValid() => Status == PatStatus.Active && DateTime.UtcNow < ExpiresAt;
}

public enum PatStatus
{
    Active,
    Revoked
}
```

### PasswordResetToken

```csharp
public class PasswordResetToken : Entity<PasswordResetTokenId>
{
    public UserId UserId { get; set; }
    public string TokenHash { get; set; }                  // SHA256 hash
    public PasswordResetTokenStatus Status { get; set; }   // Pending, Used, Expired
    public DateTime CreatedAt { get; set; }
    public DateTime ExpiresAt { get; set; }                // 1 hour default

    public bool IsValid() => Status == PasswordResetTokenStatus.Pending && DateTime.UtcNow < ExpiresAt;
}

public enum PasswordResetTokenStatus
{
    Pending,
    Used,
    Expired
}
```

---

## Success Criteria

- Users can register, verify email, and log in within 2 minutes
- JWT tokens are issued and validated on every protected request
- Sessions persist across browser restarts and can be managed by users
- OTP SMS is delivered within 10 seconds of challenge creation
- Magic links work seamlessly for guest access and expire correctly
- PATs can be created, used, and revoked without security issues
- Password reset emails are delivered within 30 seconds
- Rate limiting prevents brute force attacks (monitored via alerts)
- All authentication events are audit-logged for compliance
- No credentials (passwords, tokens) are logged or exposed in error messages
- Tests cover all success and failure paths with 100% code coverage
- Performance: authentication endpoints respond in < 200ms p95

---

## Edge Cases

1. **Concurrent Login Attempts:** If a user logs in simultaneously from two browsers, two sessions are created independently (allows concurrent sessions).

2. **Token Expiration During Request:** If access token expires mid-request, the request is rejected with 401, frontend refreshes token and retries.

3. **Refresh Token Rotation:** Optional: each refresh generates a new refresh token (rotate-on-refresh pattern) to reduce token lifetime exposure.

4. **Email Verification Resend:** If user requests resend while a valid verification link exists, the old link is invalidated and a new one is issued.

5. **Password Change During Session:** If user changes password, all active sessions except current are revoked (security measure).

6. **OTP Rate Limiting Race Condition:** Concurrent OTP verification attempts are serialized via database lock to prevent bypass.

7. **Magic Link Reuse:** If one-time-use is enabled, second usage is rejected immediately.

8. **PAT Expiration During API Call:** Request completes if token was valid at start, but subsequent requests are rejected (no mid-request revocation).

9. **SMS Delivery Failure:** Failed SMS sends are retried up to 3 times. User receives notification to request new OTP after 3 failures.

10. **GCC Region Compliance:** All authentication data is stored in GCC region (no data egress). Audit logs retained for 7 years per Saudi PDPL.

---

## Assumptions

1. **TLS/SSL:** All authentication endpoints use HTTPS. Self-signed certificates are acceptable for dev/staging; production uses valid CA certificates.

2. **Clock Synchronization:** Server clocks are synchronized via NTP. Token validation relies on consistent server time.

3. **Redis Availability:** Redis is available for session store and rate limiting. If Redis is down, sessions fall back to SQL Server (slower but functional).

4. **SMS Provider:** A configured SMS gateway (Twilio, AWS SNS, etc.) is available. Costs are managed separately.

5. **Email Delivery:** An SMTP server or email service (SendGrid, AWS SES, etc.) is configured. Delivery is assumed within 30 seconds for critical emails.

6. **Database Integrity:** SQL Server enforces unique constraints on Email and PAT token hashes. Database schema is migrated before deployment.

7. **Key Rotation:** JWT signing keys can be rotated without invalidating existing tokens (keys are versioned via `kid`).

8. **Audit Requirements:** Saudi PDPL requires 7-year retention of authentication events. A separate audit database or log sink is configured.

9. **Tenant Context:** Multi-tenant isolation is enforced at the application layer. Each request includes a tenant ID (from JWT or header).

10. **Compliance:** GCC region data residency, encryption, and PDPL compliance are handled by infrastructure (Epic 0) and assumed to be in place.

---

## Implementation Notes

- **Password Hashing:** Use bcrypt library (e.g., `BCrypt.Net-Next`) with cost factor 12. Never implement custom hashing.
- **Token Signing:** Use a mature JWT library (e.g., `System.IdentityModel.Tokens.Jwt`) with asymmetric keys for better security.
- **Rate Limiting:** Implement using Redis with sliding window algorithm (more accurate than fixed windows).
- **Error Messages:** Use generic error messages ("Invalid credentials") to prevent user enumeration. Log specific errors server-side for debugging.
- **Session Storage:** Use SQL Server for persistence and Redis for fast lookups (cache-aside pattern).
- **Audit Logging:** Use Serilog with structured logging. Include user ID, action, IP, timestamp in every log.
- **Testing:** Write unit tests for all validation rules, integration tests for full flows, and load tests for rate limiting.
- **Documentation:** Provide OpenAPI/Swagger documentation for all endpoints with example requests/responses.
