# Feature Specification: Identity & Access Management

**Feature Branch**: `001-identity-access`
**Created**: 2026-04-03
**Status**: Draft
**Input**: Identity & Access Management module — registration, login (email+password, phone OTP), JWT/refresh token lifecycle, session management, guest magic links, Personal Access Tokens (PATs), and password reset. Foundation for all user-context-dependent modules.

## Clarifications

### Session 2026-04-03

- Q: Can users register with phone number only, or is email+password the only registration path? → A: Phone OTP is login-only. Registration requires email+password. Phone number is optional profile data added after registration.

## User Scenarios & Testing *(mandatory)*

### User Story 1 — User Registration & Email Verification (Priority: P1)

As a new user, I want to register with my email and password, then verify my email, so that I can create an account and access the Muntada platform.

**Why this priority**: Registration is the gateway to the platform. No other feature works without user accounts. This is the minimum viable identity.

**Independent Test**: Can be fully tested by registering a user, receiving a verification email, clicking the link, and confirming the account is active. Delivers a verified user record.

**Acceptance Scenarios**:

1. **Given** a visitor is on the registration page, **When** they enter a valid email, password (12+ chars, 1 uppercase, 1 number, 1 special char), and confirm password, **Then** the account is created with status "Unverified" and a verification email is sent within 30 seconds.
2. **Given** a visitor tries to register with an email already in use, **When** they submit the form, **Then** they receive a generic error that does not disclose whether the email exists (prevents user enumeration).
3. **Given** a user receives the verification email, **When** they click the verification link within 24 hours, **Then** their account status transitions to "Active" and they can log in.
4. **Given** a user does not verify within 24 hours, **When** they attempt to log in, **Then** they are prompted to resend the verification email.
5. **Given** a user requests verification resend, **When** a valid unverified account exists, **Then** any previous verification link is invalidated and a new one is sent.

---

### User Story 2 — Login with Email & Password (Priority: P1)

As a registered user, I want to log in with my email and password so that I can authenticate and receive access/refresh tokens for the platform.

**Why this priority**: Login is the core authentication path. Required before any protected feature can be used.

**Independent Test**: Can be tested by logging in with valid credentials and verifying JWT issuance, then testing invalid credentials and rate limiting.

**Acceptance Scenarios**:

1. **Given** a user with a verified account enters correct credentials, **When** they submit login, **Then** they receive a JWT access token (15-minute lifetime) and a refresh token (HTTP-only cookie, 30-day lifetime), and a session record is created with device info.
2. **Given** a user enters incorrect credentials, **When** they submit login, **Then** they receive a generic "Invalid email or password" error (no information leakage) and the failed attempt is logged.
3. **Given** a user has failed login 5 times within 15 minutes, **When** they attempt again, **Then** they receive "Too many failed attempts. Please try again in 15 minutes."
4. **Given** a user successfully logs in, **When** they make requests to protected resources, **Then** their access token in the `Authorization: Bearer` header is validated on every request.

---

### User Story 3 — Token Refresh & Session Management (Priority: P1)

As a logged-in user, I want my session to persist without re-entering credentials and I want to manage my active sessions across devices.

**Why this priority**: Without token refresh, users would be logged out every 15 minutes. Session management is critical for security (revoking compromised sessions).

**Independent Test**: Can be tested by letting an access token expire, verifying auto-refresh works, listing sessions, and revoking one.

**Acceptance Scenarios**:

1. **Given** a user's access token has expired, **When** the frontend sends the refresh token, **Then** a new access token is issued if the refresh token and session are valid.
2. **Given** a user wants to see their active sessions, **When** they request the session list, **Then** they see all sessions with device info, IP, creation date, and a "current session" indicator.
3. **Given** a user wants to revoke a session (e.g., lost device), **When** they revoke it, **Then** the session is invalidated immediately and the refresh token is rejected on next use.
4. **Given** a user wants to revoke all other sessions, **When** they request it, **Then** all sessions except the current one are invalidated.
5. **Given** a session has been idle for 24 hours, **When** the system performs cleanup, **Then** the session is automatically expired.
6. **Given** a logged-in user wants to log out, **When** they request logout, **Then** the current session is revoked, the refresh token cookie is cleared, and a `UserLoggedOut` event is published.

---

### User Story 4 — Phone OTP Authentication (Priority: P1)

As a registered user with a phone number on file, I want to log in using a one-time SMS code so that I can authenticate without entering my password (passwordless login for existing accounts only — phone OTP does not create new accounts).

**Why this priority**: Passwordless login via OTP is a primary authentication method for the GCC market where phone-based auth is standard practice.

**Independent Test**: Can be tested by requesting an OTP, receiving an SMS, entering the code, and verifying a session is created.

**Acceptance Scenarios**:

1. **Given** a user enters a valid phone number (E.164 format), **When** they request an OTP, **Then** a 6-digit code is generated, stored with a 15-minute expiry, and sent via SMS within 10 seconds.
2. **Given** a user enters the correct OTP code, **When** they submit verification, **Then** a session is created (identical to email+password login).
3. **Given** a user enters an incorrect code, **When** they submit, **Then** the attempt count increments and they see remaining attempts (max 3 per challenge).
4. **Given** a user exceeds 3 failed attempts, **When** they try again, **Then** they receive "Too many attempts. Request a new code."
5. **Given** a user does not verify within 15 minutes, **When** the challenge expires, **Then** they must request a new code.

---

### User Story 5 — Guest Magic Link Access (Priority: P2)

As a room organizer, I want to generate magic links that allow guests to join audio rooms without creating an account, so that I can invite external participants with minimal friction.

**Why this priority**: Guest access is important for usability but not blocking for core platform functionality.

**Independent Test**: Can be tested by generating a link, sharing it, having a guest open it, and verifying listen-only access.

**Acceptance Scenarios**:

1. **Given** a room organizer requests a guest magic link, **When** the link is generated, **Then** a unique URL-safe token is created with a configurable expiry (default 7 days).
2. **Given** a guest clicks the magic link, **When** the token is valid and the room is scheduled or live, **Then** the guest receives a temporary session with listen-only permissions.
3. **Given** a guest attempts to speak or perform restricted actions, **When** they are in a guest session, **Then** they receive "Guest access is listen-only."
4. **Given** an organizer revokes a magic link, **When** a new guest tries to use it, **Then** they receive "This link is no longer valid."
5. **Given** a magic link has expired, **When** a guest tries to use it, **Then** they receive "This link has expired."

---

### User Story 6 — Personal Access Token (PAT) Management (Priority: P2)

As a developer, I want to create scoped Personal Access Tokens so that I can integrate with Muntada APIs programmatically without sharing my credentials.

**Why this priority**: PATs enable the public API story but are not required for core user flows.

**Independent Test**: Can be tested by creating a PAT, using it to authenticate an API request, verifying scope enforcement, and revoking it.

**Acceptance Scenarios**:

1. **Given** a user creates a PAT with a name, scopes, and expiration, **When** the token is generated, **Then** the plaintext token is displayed exactly once with a "save this now" warning, and only the hash is stored.
2. **Given** a developer uses a valid PAT in the `Authorization: Bearer` header, **When** the API validates it, **Then** the request proceeds if the PAT's scopes include the required permission and the token hasn't expired.
3. **Given** a user lists their PATs, **When** the list is returned, **Then** it shows name, scopes, creation date, expiry, and last-used date (never the token itself).
4. **Given** a user revokes a PAT, **When** a subsequent request uses that token, **Then** it is rejected with 401.

---

### User Story 7 — Password Reset Flow (Priority: P2)

As a user who forgot their password, I want to reset it via email so that I can regain access to my account.

**Why this priority**: Password reset is a standard security feature, important but not MVP-blocking.

**Independent Test**: Can be tested by requesting a reset, receiving the email, clicking the link, entering a new password, and logging in.

**Acceptance Scenarios**:

1. **Given** a user submits their email for password reset, **When** the system processes it, **Then** a reset email is sent (without disclosing if the account exists) with a link valid for 1 hour.
2. **Given** a user clicks the reset link, **When** the token is valid, **Then** they can enter a new password (same complexity rules as registration).
3. **Given** the password is successfully reset, **When** the user tries to log in, **Then** the new password works and all other sessions are revoked (security measure).
4. **Given** a reset token has expired (after 1 hour), **When** the user clicks the link, **Then** they receive "Reset link has expired" and can request a new one.
5. **Given** a user requests more than 3 resets per hour, **When** they submit again, **Then** they are rate-limited.

---

### Edge Cases

- **Concurrent Login**: User logs in simultaneously from two devices — two independent sessions are created.
- **Token Expiry Mid-Request**: Access token expires mid-request — request completes; next request triggers refresh.
- **Redis Failure**: Session cache goes down — sessions fall back to SQL Server (slower but functional).
- **SMS Delivery Failure**: Retry up to 3 times with exponential backoff; user can request a new OTP.
- **Password Change**: All other sessions except current are revoked for security.
- **Concurrent OTP Verification**: Serialized via database-level locking to prevent bypass.
- **Magic Link Reuse**: If one-time-use is enabled, second usage is rejected immediately.
- **PAT Expiry During API Call**: Request completes if valid at start; subsequent requests rejected.
- **Email Verification Resend**: Old link is invalidated, new link issued.
- **GCC Compliance**: All auth data stored in GCC region. Audit logs retained for 7 years per Saudi PDPL.

## Requirements *(mandatory)*

### Functional Requirements

**Registration & Onboarding**

- **FR-001**: System MUST support user registration via email and password with complexity validation (minimum 12 characters, 1 uppercase, 1 number, 1 special character).
- **FR-002**: Email addresses MUST be case-insensitive, stored in lowercase, and unique across the system.
- **FR-003**: Passwords MUST be hashed with bcrypt (cost factor 12) and never stored or logged in plaintext.
- **FR-004**: Email verification MUST be mandatory for account activation. Verification tokens expire after 24 hours.

**Authentication Methods**

- **FR-005**: System MUST support two authentication methods: email+password and phone OTP. Registration requires email+password; phone OTP is login-only for existing accounts with a phone number on file. Each method has independent flow and rate limiting.
- **FR-006**: Phone numbers MUST be validated as E.164 format. International country codes MUST be supported.
- **FR-007**: OTP codes MUST be 6 digits, cryptographically random, expire after 15 minutes, with max 3 verification attempts per challenge.
- **FR-008**: SMS delivery MUST be handled by a configured gateway with retry logic (max 3 retries, exponential backoff).

**Token & Session Management**

- **FR-009**: Access tokens MUST be short-lived (15 minutes default). Claims MUST include user ID, tenant ID, scopes, and expiration.
- **FR-010**: Refresh tokens MUST be opaque, stored as HTTP-only cookies with Secure and SameSite=Strict flags.
- **FR-011**: Sessions MUST track user ID, tenant ID, device info (User-Agent, IP), and support immediate revocation.
- **FR-012**: Rate limiting MUST be enforced on login endpoints (max 5 failed attempts per email per 15 minutes).

**Guest Access & Magic Links**

- **FR-013**: Magic links MUST be generated for guest access to specific rooms with configurable expiration (default 7 days).
- **FR-014**: Guest sessions MUST grant listen-only permissions enforced at the authorization layer.

**Personal Access Tokens**

- **FR-015**: PATs MUST be scoped to a user and tenant. Tokens MUST NOT cross tenant boundaries.
- **FR-016**: PAT plaintext MUST be displayed only once at creation. Only the bcrypt hash is stored.
- **FR-017**: PAT usage MUST be logged with last-used timestamp for audit purposes.

**Security & Compliance**

- **FR-018**: All authentication errors MUST use generic messages to prevent user enumeration.
- **FR-019**: Failed authentication attempts MUST be logged with IP, User-Agent, and timestamp.
- **FR-020**: Account lockout MUST occur after configurable failed attempts (default 5) within a time window (default 15 minutes).
- **FR-021**: Password reset tokens MUST expire after 1 hour and be one-time use.
- **FR-022**: All authentication data MUST be stored in the GCC region. Audit logs MUST be retained for 7 years per Saudi PDPL.

**Integration Events**

- **FR-023**: The following events MUST be published: UserRegistered, UserEmailVerified, UserLoggedIn, UserLoggedOut, PasswordChanged, SessionRevoked, PATCreated, PATRevoked.

### Key Entities

- **User**: Central identity entity. Has email, hashed password, status (Unverified/Active/Suspended/Deleted), optional phone number. Owns sessions and PATs.
- **Session**: Authenticated session record. Tracks device info, refresh token, activity timestamps, and expiration. Can be revoked.
- **RefreshToken**: Opaque token bound to a session. Hash-only storage. Supports rotation.
- **OtpChallenge**: Phone OTP verification record. Tracks code, attempts, expiry. Locked after 3 failures.
- **GuestMagicLink**: URL-safe token for guest room access. Hash-only storage. Configurable expiry. Listen-only permissions.
- **PersonalAccessToken**: Scoped API token for developer integrations. Hash-only storage. Tenant-bound. Tracks last usage.
- **PasswordResetToken**: One-time-use token for password recovery. Hash-only storage. 1-hour expiry.
- **EmailVerificationToken**: One-time-use token for email confirmation. Hash-only storage. 24-hour expiry.

## Success Criteria *(mandatory)*

### Measurable Outcomes

- **SC-001**: Users can register, verify email, and log in within 2 minutes end-to-end.
- **SC-002**: OTP SMS is delivered within 10 seconds of challenge creation.
- **SC-003**: Password reset emails are delivered within 30 seconds.
- **SC-004**: Sessions persist across browser restarts and can be listed/revoked by the user.
- **SC-005**: Magic links work seamlessly for guest access with correct permission enforcement.
- **SC-006**: PATs can be created, used for API authentication, and revoked without credential leakage.
- **SC-007**: Rate limiting prevents brute force attacks — no more than 5 failed login attempts per 15 minutes per account.
- **SC-008**: All authentication events are audit-logged for compliance (retained 7 years).
- **SC-009**: No credentials (passwords, tokens, OTP codes) appear in logs or error messages.
- **SC-010**: Authentication endpoints respond in under 500ms for 95% of requests under normal load.

## Assumptions

- **Shared Kernel**: Epic 0 (Foundation) is complete, providing base entity types, opaque ID generation, integration event publishing, and error handling middleware.
- **Infrastructure**: SQL Server, Redis, and RabbitMQ are available (provisioned by Aspire in dev, Helm in staging/prod).
- **Email Service**: An SMTP server or email API (SendGrid, AWS SES) is configured and operational.
- **SMS Provider**: A configured SMS gateway (Twilio, AWS SNS, or local GCC provider) is available for OTP delivery.
- **TLS**: All authentication endpoints are served over HTTPS. TLS termination is handled at the ingress layer.
- **Multi-tenancy**: Tenant context is included in JWT claims and enforced at the application layer (Tenancy module built in Epic 2).
- **Clock Synchronization**: Server clocks are NTP-synchronized for consistent token expiration validation.
- **Compliance**: Saudi PDPL requires 7-year retention of authentication audit events. Data residency is enforced by infrastructure.
- **Out of Scope**: Social login (Google, Apple) is explicitly out of scope for this epic.
- **Out of Scope**: Two-factor authentication (2FA/MFA) beyond phone OTP is out of scope for this epic.
