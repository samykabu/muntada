# Epic 1: Identity & Access Module - Task Breakdown

**Version:** 1.0
**Epic Owner:** Platform / Security
**Last Updated:** 2026-04-03

---

## Execution Overview

This epic implements comprehensive authentication and authorization. All tasks depend on shared kernel (Epic 0) and follow modular structure. Tasks are organized into 4 phases: infrastructure, core auth, advanced features, and integration.

---

## Phase 1: Module Infrastructure

### T101: Identity Module Structure & Database Setup [P]
**User Story:** US-1.1, US-1.2
**Priority:** P1
**Effort:** 5 pts
**Dependencies:** Epic 0 complete

Create Identity module project structure and database schema.

**Deliverables:**
- `backend/src/Modules/Identity/Identity.csproj`
- `Domain/`, `Application/`, `Infrastructure/`, `Api/` folders
- SQL Server schema: `[identity]`
- EF Core DbContext for Identity entities
- Initial migration script

**File Locations:**
- `backend/src/Modules/Identity/Identity.csproj`
- `backend/src/Modules/Identity/Infrastructure/IdentityDbContext.cs`
- `backend/src/Modules/Identity/Infrastructure/Migrations/`

**Acceptance:**
- Project compiles
- Database schema created
- EF Core migrations run successfully
- Module can be referenced by other modules

---

### T102: User Entity & Value Objects [P]
**User Story:** US-1.1
**Priority:** P1
**Effort:** 5 pts
**Dependencies:** T101

Implement User aggregate root and supporting value objects.

**Deliverables:**
- `User` aggregate root (Id, Email, PasswordHash, Status, CreatedAt, UpdatedAt)
- `Email` value object (lowercase, validation)
- `PasswordHash` value object (bcrypt)
- `PhoneNumber` value object (E.164 format)
- `UserStatus` enum (Unverified, Active, Suspended, Deleted)

**File Locations:**
- `backend/src/Modules/Identity/Domain/User/User.cs`
- `backend/src/Modules/Identity/Domain/User/Email.cs`
- `backend/src/Modules/Identity/Domain/User/PasswordHash.cs`
- `backend/src/Modules/Identity/Domain/User/PhoneNumber.cs`

**Acceptance:**
- Classes compile
- Value objects enforce invariants
- Unit tests pass (validation, equality)

---

### T103: Session & RefreshToken Entities [P]
**User Story:** US-1.2, US-1.3
**Priority:** P1
**Effort:** 5 pts
**Dependencies:** T102

Implement Session and RefreshToken entities.

**Deliverables:**
- `Session` aggregate root (UserId, Status, DeviceInfo, ExpiresAt)
- `DeviceInfo` value object (UserAgent, IpAddress, Country)
- `RefreshToken` entity (SessionId, TokenHash, Status, ExpiresAt)
- `SessionStatus`, `RefreshTokenStatus` enums

**File Locations:**
- `backend/src/Modules/Identity/Domain/Session/Session.cs`
- `backend/src/Modules/Identity/Domain/Session/RefreshToken.cs`
- `backend/src/Modules/Identity/Domain/Session/DeviceInfo.cs`

**Acceptance:**
- Entities compile
- Relationships correctly modeled
- Database constraints enforceable

---

### T104: OTP Challenge Entity [P]
**User Story:** US-1.4
**Priority:** P1
**Effort:** 3 pts
**Dependencies:** T101

Implement OTP challenge for phone authentication.

**Deliverables:**
- `OtpChallenge` aggregate (PhoneNumber, Code, Status, Attempts, ExpiresAt)
- `OtpStatus` enum (Pending, Verified, Expired)
- Methods: `IsValid()`, `IncrementFailedAttempts()`

**File Locations:**
- `backend/src/Modules/Identity/Domain/Otp/OtpChallenge.cs`

**Acceptance:**
- Aggregate compiles
- Status validation correct
- Unit tests for expiry and attempt logic

---

### T105: Guest Magic Link Entity [P]
**User Story:** US-1.5
**Priority:** P2
**Effort:** 3 pts
**Dependencies:** T101

Implement guest magic link for room access.

**Deliverables:**
- `GuestMagicLink` aggregate (RoomOccurrenceId, Token, TokenHash, Status, ExpiresAt)
- `GuestMagicLinkStatus` enum (Active, Revoked, Expired)
- Methods: `IsValid()`

**File Locations:**
- `backend/src/Modules/Identity/Domain/GuestLink/GuestMagicLink.cs`

**Acceptance:**
- Aggregate compiles
- Token hashing implemented (SHA256)
- Status logic correct

---

### T106: Personal Access Token Entity [P]
**User Story:** US-1.6
**Priority:** P2
**Effort:** 3 pts
**Dependencies:** T101

Implement Personal Access Token (PAT) for API access.

**Deliverables:**
- `PersonalAccessToken` aggregate (UserId, TenantId, Name, TokenHash, Scopes, Status, ExpiresAt)
- `PatStatus` enum (Active, Revoked)
- Methods: `IsValid()`

**File Locations:**
- `backend/src/Modules/Identity/Domain/Pat/PersonalAccessToken.cs`

**Acceptance:**
- Aggregate compiles
- Tenant scoping enforced
- Scope list modeled correctly

---

### T107: Password Reset Token Entity [P]
**User Story:** US-1.7
**Priority:** P2
**Effort:** 2 pts
**Dependencies:** T101

Implement password reset token.

**Deliverables:**
- `PasswordResetToken` entity (UserId, TokenHash, Status, ExpiresAt)
- `PasswordResetTokenStatus` enum (Pending, Used, Expired)
- Methods: `IsValid()`

**File Locations:**
- `backend/src/Modules/Identity/Domain/PasswordReset/PasswordResetToken.cs`

**Acceptance:**
- Entity compiles
- One-time use enforced
- Expiry logic correct

---

## Phase 2: Core Authentication

### T108: User Registration Command & Handler [P]
**User Story:** US-1.1
**Priority:** P1
**Effort:** 8 pts
**Dependencies:** T102

Implement user registration command and handler.

**Deliverables:**
- `RegisterUserCommand` DTO
- `RegisterUserCommandHandler` validates and creates user
- Email uniqueness check
- Password validation (12+ chars, complexity)
- Publish `UserRegistered` integration event
- Unit tests for validation rules

**File Locations:**
- `backend/src/Modules/Identity/Application/Commands/RegisterUserCommand.cs`
- `backend/src/Modules/Identity/Application/Commands/RegisterUserCommandHandler.cs`
- `backend/src/Modules/Identity/Application/Validators/RegisterUserValidator.cs`
- `backend/src/Modules/Identity/Domain/Events/UserRegisteredEvent.cs`

**Acceptance:**
- Valid registration succeeds
- Duplicate email rejected
- Password complexity enforced
- Bcrypt hash generated (cost factor 12)
- Event published to RabbitMQ

---

### T109: Email Verification Flow [P]
**User Story:** US-1.1
**Priority:** P1
**Effort:** 8 pts
**Dependencies:** T108

Implement email verification token generation, sending, and validation.

**Deliverables:**
- `EmailVerificationToken` entity (UserId, TokenHash, ExpiresAt)
- `GenerateEmailVerificationTokenCommand` & handler
- `VerifyEmailCommand` & handler
- Email service integration (SMTP)
- Token expiry: 24 hours
- Resend logic (invalidate old tokens)

**File Locations:**
- `backend/src/Modules/Identity/Domain/EmailVerification/EmailVerificationToken.cs`
- `backend/src/Modules/Identity/Application/Commands/GenerateEmailVerificationTokenCommand.cs`
- `backend/src/Modules/Identity/Application/Commands/VerifyEmailCommand.cs`
- `backend/src/Modules/Identity/Application/Services/EmailService.cs`

**Acceptance:**
- Token generated and stored
- Email sent within 30 seconds
- Verification link works
- User status transitions to Active
- Account lockout after 5 failed attempts

---

### T110: Login Command & Handler [P]
**User Story:** US-1.2
**Priority:** P1
**Effort:** 10 pts
**Dependencies:** T108, T103

Implement login command with rate limiting and session creation.

**Deliverables:**
- `LoginCommand` DTO (email, password)
- `LoginCommandHandler` validates credentials
- `LoginResponse` with accessToken, refreshToken
- Rate limiting: 5 failed attempts per 15 minutes
- Session creation with DeviceInfo
- JWT token generation (HS256 or RS256)
- Refresh token generation (opaque string)
- Publish `UserLoggedIn` event

**File Locations:**
- `backend/src/Modules/Identity/Application/Commands/LoginCommand.cs`
- `backend/src/Modules/Identity/Application/Commands/LoginCommandHandler.cs`
- `backend/src/Modules/Identity/Application/Services/TokenService.cs`
- `backend/src/Modules/Identity/Infrastructure/Services/JwtTokenService.cs`

**Acceptance:**
- Valid credentials accepted
- Invalid credentials rejected (generic error)
- Rate limit enforced (Redis)
- JWT contains correct claims (sub, aud, scope, iat, exp)
- Refresh token secure HTTP-only cookie
- Session persisted to database

---

### T111: Token Refresh & Session Management [P]
**User Story:** US-1.3
**Priority:** P1
**Effort:** 10 pts
**Dependencies:** T110

Implement token refresh and session management endpoints.

**Deliverables:**
- `RefreshTokenCommand` & handler
- `ListSessionsQuery` & handler
- `RevokeSessionCommand` & handler
- `RevokeAllOtherSessionsCommand` & handler
- Idle session cleanup job (24 hour timeout)
- IP address validation on refresh
- Token rotation support

**File Locations:**
- `backend/src/Modules/Identity/Application/Commands/RefreshTokenCommand.cs`
- `backend/src/Modules/Identity/Application/Queries/ListSessionsQuery.cs`
- `backend/src/Modules/Identity/Application/Commands/RevokeSessionCommand.cs`
- `backend/src/Modules/Identity/Application/BackgroundJobs/IdleSessionCleanupJob.cs`

**Acceptance:**
- Refresh endpoint returns new access token
- Token hash matches stored value (bcrypt)
- Session expiry checked
- IP address validated or within geofence
- Sessions listed with device info
- Revocation prevents future refreshes
- Idle sessions cleaned up automatically

---

### T112: OTP Challenge & Verification [P]
**User Story:** US-1.4
**Priority:** P1
**Effort:** 12 pts
**Dependencies:** T104

Implement OTP challenge generation and verification.

**Deliverables:**
- `GenerateOtpChallengeCommand` & handler
- `VerifyOtpCommand` & handler
- SMS integration (Twilio, AWS SNS, or custom)
- 6-digit code generation (cryptographically secure)
- Attempt limiting (max 3 failed)
- Expiry enforcement (15 minutes)
- Challenge lockout after failed attempts
- Session creation on successful verification
- Cleanup job for expired challenges

**File Locations:**
- `backend/src/Modules/Identity/Application/Commands/GenerateOtpChallengeCommand.cs`
- `backend/src/Modules/Identity/Application/Commands/VerifyOtpCommand.cs`
- `backend/src/Modules/Identity/Application/Services/SmsService.cs`
- `backend/src/Modules/Identity/Infrastructure/Services/TwilioSmsService.cs` (or custom)
- `backend/src/Modules/Identity/Application/BackgroundJobs/ExpiredOtpCleanupJob.cs`

**Acceptance:**
- OTP code generated and stored
- SMS sent within 10 seconds
- Verification accepts correct code
- Attempts tracked
- Lockout enforced after 3 failures
- Session created on success
- Expired challenges cleaned up

---

## Phase 3: Advanced Features

### T113: Guest Magic Link Generation & Validation [P]
**User Story:** US-1.5
**Priority:** P2
**Effort:** 8 pts
**Dependencies:** T105

Implement guest magic link creation and validation.

**Deliverables:**
- `GenerateGuestMagicLinkCommand` & handler
- `ValidateGuestMagicLinkQuery` & handler
- Token generation (cryptographically secure, 32 bytes)
- Expiry: 7 days (configurable)
- Rate limiting: max 10 per room per day
- Revocation endpoint
- Publish `GuestLinkGenerated` event

**File Locations:**
- `backend/src/Modules/Identity/Application/Commands/GenerateGuestMagicLinkCommand.cs`
- `backend/src/Modules/Identity/Application/Queries/ValidateGuestMagicLinkQuery.cs`
- `backend/src/Modules/Identity/Domain/Events/GuestLinkGeneratedEvent.cs`

**Acceptance:**
- Link generated with unique token
- Validation checks signature and expiry
- Room context validated
- Rate limit enforced
- Can be revoked

---

### T114: Personal Access Token Management [P]
**User Story:** US-1.6
**Priority:** P2
**Effort:** 10 pts
**Dependencies:** T106

Implement PAT creation, listing, and revocation.

**Deliverables:**
- `CreatePersonalAccessTokenCommand` & handler
- `ListPersonalAccessTokensQuery` & handler
- `RevokePersonalAccessTokenCommand` & handler
- Token hashing (bcrypt)
- Plaintext display once (warning)
- Scope validation
- Last used tracking
- Publish `PATCreated`, `PATRevoked` events

**File Locations:**
- `backend/src/Modules/Identity/Application/Commands/CreatePersonalAccessTokenCommand.cs`
- `backend/src/Modules/Identity/Application/Queries/ListPersonalAccessTokensQuery.cs`
- `backend/src/Modules/Identity/Application/Commands/RevokePersonalAccessTokenCommand.cs`

**Acceptance:**
- Token created with bcrypt hash
- Plaintext displayed once only
- Scopes enforced in later requests
- List shows all PATs with metadata
- Revocation prevents future use

---

### T115: Password Reset Flow [P]
**User Story:** US-1.7
**Priority:** P2
**Effort:** 8 pts
**Dependencies:** T107, T109

Implement password reset token and reset endpoint.

**Deliverables:**
- `ForgotPasswordCommand` & handler (generic error message)
- `ResetPasswordCommand` & handler
- Token generation (cryptographically secure)
- Token expiry: 1 hour
- Email sending
- One-time use enforcement
- Rate limiting: max 3 requests per email per hour
- Password validation (same rules as registration)

**File Locations:**
- `backend/src/Modules/Identity/Application/Commands/ForgotPasswordCommand.cs`
- `backend/src/Modules/Identity/Application/Commands/ResetPasswordCommand.cs`

**Acceptance:**
- Forgot password generates token
- Email sent within 30 seconds
- Reset validates token and new password
- Token invalidated after use
- Generic error message (no user enumeration)
- Rate limit enforced

---

### T116: Rate Limiting Middleware [P]
**User Story:** US-1.2, US-1.4
**Priority:** P1
**Effort:** 5 pts
**Dependencies:** Epic 0 (Redis)

Implement rate limiting using Redis.

**Deliverables:**
- `RateLimitingMiddleware` class
- Sliding window algorithm
- Endpoint-specific limits
- Key format: `rate-limit:{endpoint}:{identifier}:{window}`
- TTL: automatic expiry

**File Locations:**
- `backend/src/SharedKernel/Infrastructure/Middleware/RateLimitingMiddleware.cs`
- `backend/src/Modules/Identity/Infrastructure/RateLimitPolicies.cs`

**Acceptance:**
- Requests within limit accepted
- Requests exceeding limit rejected (429)
- Sliding window correct
- Redis keys expire automatically

---

## Phase 4: API Endpoints & Integration

### T117: Identity API Controller - Registration & Login [P]
**User Story:** US-1.1, US-1.2
**Priority:** P1
**Effort:** 8 pts
**Dependencies:** T108, T110

Create REST API endpoints for registration and login.

**Deliverables:**
- `POST /api/identity/auth/register` - registration
- `POST /api/identity/auth/login` - login
- `POST /api/identity/auth/verify-email` - email verification
- DTOs: `RegisterRequest`, `LoginRequest`, `LoginResponse`
- Status codes: 201 (register), 200 (login), 400 (validation), 429 (rate limit)
- Swagger/OpenAPI documentation

**File Locations:**
- `backend/src/Modules/Identity/Api/Controllers/AuthController.cs`
- `backend/src/Modules/Identity/Api/Dtos/RegisterRequest.cs`
- `backend/src/Modules/Identity/Api/Dtos/LoginRequest.cs`
- `backend/src/Modules/Identity/Api/Dtos/LoginResponse.cs`

**Acceptance:**
- Endpoints return correct status codes
- Responses match OpenAPI spec
- Validation errors described
- Swagger docs complete

---

### T118: Identity API Controller - Token & Sessions [P]
**User Story:** US-1.3
**Priority:** P1
**Effort:** 8 pts
**Dependencies:** T111

Create REST API endpoints for token refresh and session management.

**Deliverables:**
- `POST /api/identity/auth/refresh` - refresh token
- `GET /api/identity/sessions` - list sessions
- `DELETE /api/identity/sessions/{sessionId}` - revoke session
- `DELETE /api/identity/sessions?exceptCurrent=true` - revoke all others
- DTOs: `SessionDto`, `RefreshTokenRequest`, `RefreshTokenResponse`

**File Locations:**
- `backend/src/Modules/Identity/Api/Controllers/SessionController.cs`
- `backend/src/Modules/Identity/Api/Dtos/SessionDto.cs`

**Acceptance:**
- Refresh endpoint returns new token
- Session list shows all sessions with device info
- Revocation successful
- Unauthorized requests rejected (401/403)

---

### T119: Identity API Controller - OTP & Magic Links [P]
**User Story:** US-1.4, US-1.5
**Priority:** P2
**Effort:** 8 pts
**Dependencies:** T112, T113

Create REST API endpoints for OTP and guest magic links.

**Deliverables:**
- `POST /api/identity/auth/otp/challenge` - request OTP
- `POST /api/identity/auth/otp/verify` - verify OTP
- `POST /api/identity/magic-links` - generate guest link
- `GET /api/identity/magic-links/validate` - validate guest link
- `DELETE /api/identity/magic-links/{linkId}` - revoke guest link
- DTOs and Swagger docs

**File Locations:**
- `backend/src/Modules/Identity/Api/Controllers/OtpController.cs`
- `backend/src/Modules/Identity/Api/Controllers/MagicLinkController.cs`

**Acceptance:**
- OTP endpoints functional
- Magic link generation works
- Validation checks expiry and status

---

### T120: Identity API Controller - PAT & Password Reset [P]
**User Story:** US-1.6, US-1.7
**Priority:** P2
**Effort:** 8 pts
**Dependencies:** T114, T115

Create REST API endpoints for PAT and password reset.

**Deliverables:**
- `POST /api/identity/pats` - create PAT
- `GET /api/identity/pats` - list PATs
- `DELETE /api/identity/pats/{patId}` - revoke PAT
- `POST /api/identity/auth/forgot-password` - request reset
- `POST /api/identity/auth/reset-password` - reset password
- DTOs and Swagger docs

**File Locations:**
- `backend/src/Modules/Identity/Api/Controllers/PatController.cs`
- `backend/src/Modules/Identity/Api/Controllers/PasswordResetController.cs`

**Acceptance:**
- All endpoints functional
- Responses match spec
- Swagger docs complete

---

### T121: Frontend: Registration Page Component [P]
**User Story:** US-1.1
**Priority:** P2
**Effort:** 8 pts
**Dependencies:** T117

Create React registration page component.

**Deliverables:**
- `frontend/src/features/auth/pages/RegisterPage.tsx`
- Form with email, password, confirm password fields
- Client-side validation
- Error handling and display
- Loading state
- Link to login page
- Redirect on success

**File Locations:**
- `frontend/src/features/auth/pages/RegisterPage.tsx`
- `frontend/src/features/auth/components/RegisterForm.tsx`
- `frontend/src/features/auth/api/registerApi.ts`

**Acceptance:**
- Form renders correctly
- Validation works client-side
- Calls backend API
- Success redirects to verification or login
- Error messages displayed

---

### T122: Frontend: Login Page Component [P]
**User Story:** US-1.2
**Priority:** P2
**Effort:** 8 pts
**Dependencies:** T117

Create React login page component.

**Deliverables:**
- `frontend/src/features/auth/pages/LoginPage.tsx`
- Form with email and password fields
- "Forgot password?" link
- Error handling
- Loading state
- Redirect on success to dashboard
- Remember me (optional, Phase 2)

**File Locations:**
- `frontend/src/features/auth/pages/LoginPage.tsx`
- `frontend/src/features/auth/components/LoginForm.tsx`
- `frontend/src/features/auth/api/loginApi.ts`

**Acceptance:**
- Form renders and validates
- API calls work
- Success redirects to dashboard
- Error messages clear

---

### T123: Frontend: Session Management & Token Refresh [P]
**User Story:** US-1.3
**Priority:** P2
**Effort:** 8 pts
**Dependencies:** T122

Implement token refresh and session management in frontend.

**Deliverables:**
- `frontend/src/features/auth/hooks/useAuth.ts` - auth context hook
- HTTP interceptor for token refresh
- Redirect to login on 401
- Session list view (optional phase 2)
- Redux store for auth state (or context)

**File Locations:**
- `frontend/src/features/auth/context/AuthContext.tsx`
- `frontend/src/features/auth/hooks/useAuth.ts`
- `frontend/src/api/interceptors/tokenRefreshInterceptor.ts`

**Acceptance:**
- Token refresh automatic on 401
- No user redirected except on final failure
- Auth state persists across page reload (localStorage)

---

### T124: Frontend: OTP Login Page [P]
**User Story:** US-1.4
**Priority:** P2
**Effort:** 8 pts
**Dependencies:** T119

Create React OTP login page.

**Deliverables:**
- `frontend/src/features/auth/pages/OtpLoginPage.tsx`
- Phone number input
- OTP code input (6 digits)
- SMS resend functionality
- Time-based countdown
- Error messages

**File Locations:**
- `frontend/src/features/auth/pages/OtpLoginPage.tsx`
- `frontend/src/features/auth/components/OtpForm.tsx`
- `frontend/src/features/auth/api/otpApi.ts`

**Acceptance:**
- Phone input validates E.164
- OTP sent and received
- Verification works
- Countdown displayed
- Resend button functional

---

### T125: Frontend: Password Reset Flow [P]
**User Story:** US-1.7
**Priority:** P2
**Effort:** 6 pts
**Dependencies:** T120

Create React password reset pages.

**Deliverables:**
- `frontend/src/features/auth/pages/ForgotPasswordPage.tsx` - request reset
- `frontend/src/features/auth/pages/ResetPasswordPage.tsx` - enter new password
- Links in login page
- Email validation
- Password validation
- Success message

**File Locations:**
- `frontend/src/features/auth/pages/ForgotPasswordPage.tsx`
- `frontend/src/features/auth/pages/ResetPasswordPage.tsx`
- `frontend/src/features/auth/api/passwordResetApi.ts`

**Acceptance:**
- Forgot password form works
- Email validated
- Reset page token validation
- New password set successfully
- Redirect to login

---

## Phase 5: Integration Tests & Monitoring

### T126: Identity Integration Tests [P]
**User Story:** All
**Priority:** P1
**Effort:** 13 pts
**Dependencies:** All implementation tasks

Write comprehensive integration tests for Identity module.

**Deliverables:**
- Tests for registration flow (valid, duplicate email, weak password)
- Tests for login (valid, invalid, rate limiting)
- Tests for token refresh
- Tests for OTP flow
- Tests for guest magic links
- Tests for PAT management
- Tests for password reset
- Database test fixtures

**File Locations:**
- `backend/src/Modules/Identity.Tests/Integration/`

**Acceptance:**
- All user story acceptance criteria tested
- Edge cases covered
- Tests pass consistently
- Code coverage > 80%

---

### T127: Audit Logging for Authentication Events [P]
**User Story:** US-1.1 - US-1.7
**Priority:** P2
**Effort:** 5 pts
**Dependencies:** T108 - T115

Implement audit logging for all authentication events.

**Deliverables:**
- Log: user registration, email verification, login (success/failure), logout, password change, PAT creation/revocation
- Structured logs with: timestamp, userId, action, ipAddress, userAgent, outcome
- Serilog integration
- Retention per PDPL (7 years for audit logs)

**File Locations:**
- `backend/src/Modules/Identity/Application/Services/AuditLogService.cs`

**Acceptance:**
- All auth events logged
- Logs searchable and structured
- Sensitive data not logged (passwords, tokens)
- Retention policy enforced

---

### T128: Security Event Alerting [P]
**User Story:** US-1.2
**Priority:** P2
**Effort:** 5 pts
**Dependencies:** T127

Set up alerts for security events.

**Deliverables:**
- Alert on repeated failed login attempts
- Alert on PAT revocation
- Alert on password reset
- Email notifications to admin
- Dashboard integration (future)

**File Locations:**
- `backend/src/Modules/Identity/Application/Services/SecurityAlertService.cs`

**Acceptance:**
- Alerts sent for configured events
- Email delivery within 1 minute
- Configurable thresholds

---

## Success Metrics

- User can register, verify email, and login within 2 minutes
- JWT tokens issued and validated on every protected request
- OTP delivered within 10 seconds
- Magic links work seamlessly for guest access
- All auth events audit-logged for 7 years
- Rate limiting prevents brute force (alerts monitored)
- Performance: auth endpoints < 200ms p95
- 100% code coverage for security-critical paths
