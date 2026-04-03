# Tasks: Identity & Access Management

**Input**: Design documents from `/specs/001-identity-access/`
**Prerequisites**: plan.md, spec.md (post-clarification), research.md
**Version**: 1.0
**Last Updated**: 2026-04-03

**Tests**: Unit tests are MANDATORY per Constitution XI. TDD for auth policy evaluation and state transitions per Constitution IV.

**Organization**: Tasks grouped by user story for independent implementation and testing.

## Format: `[ID] [P?] [Story] Description`

- **[P]**: Can run in parallel (different files, no dependencies)
- **[Story]**: Which user story (US1-US7) this task belongs to
- Include exact file paths in descriptions

---

## Phase 1: Module Setup

**Purpose**: Identity module project structure, DbContext, Aspire registration

- [x] T001 Create `backend/src/Modules/Identity/Identity.csproj` with references to SharedKernel, EF Core, BCrypt.Net-Next, MediatR, FluentValidation
- [x] T002 [P] Create directory structure: `Domain/{User,Session,Otp,GuestLink,Pat,PasswordReset,EmailVerification,Events}`, `Application/{Commands,Queries,Validators,Services,BackgroundJobs}`, `Infrastructure/{Services,Repositories,RateLimiting}`, `Api/{Controllers,Dtos}`
- [x] T003 Create `backend/src/Modules/Identity/Infrastructure/IdentityDbContext.cs` with `[identity]` schema configuration
- [x] T004 [P] Create `backend/tests/Modules/Identity.Tests/Identity.Tests.csproj` with xUnit, FluentAssertions, Moq references to Identity project
- [x] T005 Register Identity module in Aspire AppHost (`aspire/Muntada.AppHost/AppHost.cs`) and add Identity.csproj to solution
- [x] T006 Add Identity project reference to `backend/src/Muntada.Api/Muntada.Api.csproj` and register services in `Program.cs`

**Checkpoint**: Identity module compiles, DbContext configured, registered in Aspire.

---

## Phase 2: Domain Layer — Core Entities (Blocking)

**Purpose**: All domain entities and value objects needed across user stories. MUST complete before any story work.

### Value Objects

- [x] T007 [P] Implement `Email` value object in `backend/src/Modules/Identity/Domain/User/Email.cs` — lowercase normalization, RFC 5322 validation, XML docs
- [x] T008 [P] Implement `PasswordHash` value object in `backend/src/Modules/Identity/Domain/User/PasswordHash.cs` — bcrypt cost 12, Create/Verify methods, XML docs
- [x] T009 [P] Implement `PhoneNumber` value object in `backend/src/Modules/Identity/Domain/User/PhoneNumber.cs` — E.164 validation, XML docs
- [x] T010 [P] Implement `DeviceInfo` value object in `backend/src/Modules/Identity/Domain/Session/DeviceInfo.cs` — UserAgent, IpAddress, Country, XML docs

### Aggregates & Entities

- [x] T011 Implement `User` aggregate root in `backend/src/Modules/Identity/Domain/User/User.cs` — Id (usr_), Email, PasswordHash, PhoneNumber, Status enum (Unverified/Active/Suspended/Deleted), FullName, timestamps, XML docs
- [x] T012 [P] Implement `Session` aggregate root in `backend/src/Modules/Identity/Domain/Session/Session.cs` — Id (ses_), UserId, TenantId, Status enum (Active/Revoked/Expired), DeviceInfo, RefreshTokenId, timestamps, Revoke() method, XML docs
- [x] T013 [P] Implement `RefreshToken` entity in `backend/src/Modules/Identity/Domain/Session/RefreshToken.cs` — Id, SessionId, TokenHash, Status enum, IsValid() method, XML docs
- [x] T014 [P] Implement `OtpChallenge` aggregate in `backend/src/Modules/Identity/Domain/Otp/OtpChallenge.cs` — Id (otp_), PhoneNumber, CodeHash, Status enum (Pending/Verified/Expired), FailedAttempts, IsValid(), IncrementFailedAttempts(), XML docs
- [x] T015 [P] Implement `GuestMagicLink` aggregate in `backend/src/Modules/Identity/Domain/GuestLink/GuestMagicLink.cs` — Id (lnk_), RoomOccurrenceId, CreatedBy, TokenHash (SHA256), Status enum (Active/Revoked/Expired), ExpiresAt, UsageCount, IsValid(), XML docs
- [x] T016 [P] Implement `PersonalAccessToken` aggregate in `backend/src/Modules/Identity/Domain/Pat/PersonalAccessToken.cs` — Id (pat_), UserId, TenantId, Name, TokenHash (bcrypt), Scopes list, Status enum (Active/Revoked), ExpiresAt, LastUsedAt, IsValid(), XML docs
- [x] T017 [P] Implement `PasswordResetToken` entity in `backend/src/Modules/Identity/Domain/PasswordReset/PasswordResetToken.cs` — Id (prt_), UserId, TokenHash (SHA256), Status enum (Pending/Used/Expired), ExpiresAt, IsValid(), XML docs
- [x] T018 [P] Implement `EmailVerificationToken` entity in `backend/src/Modules/Identity/Domain/EmailVerification/EmailVerificationToken.cs` — Id (evt_), UserId, TokenHash (SHA256), Status enum (Pending/Used/Expired), ExpiresAt, IsValid(), XML docs

### Integration Events

- [x] T019 [P] Create integration events in `backend/src/Modules/Identity/Domain/Events/` — UserRegisteredEvent, UserEmailVerifiedEvent, UserLoggedInEvent, UserLoggedOutEvent, PasswordChangedEvent, SessionRevokedEvent, PATCreatedEvent, PATRevokedEvent — all implementing IIntegrationEvent, XML docs

### Unit Tests for Domain

- [x] T020 [P] Write unit tests for `Email` value object in `backend/tests/Modules/Identity.Tests/Domain/EmailTests.cs`
- [x] T021 [P] Write unit tests for `PasswordHash` value object in `backend/tests/Modules/Identity.Tests/Domain/PasswordHashTests.cs`
- [x] T022 [P] Write unit tests for `PhoneNumber` value object in `backend/tests/Modules/Identity.Tests/Domain/PhoneNumberTests.cs`
- [x] T023 [P] Write unit tests for `User` aggregate (state transitions, validation) in `backend/tests/Modules/Identity.Tests/Domain/UserTests.cs`
- [x] T024 [P] Write unit tests for `Session` aggregate (revoke, expiry) in `backend/tests/Modules/Identity.Tests/Domain/SessionTests.cs`
- [x] T025 [P] Write unit tests for `OtpChallenge` (attempts, expiry, locking) in `backend/tests/Modules/Identity.Tests/Domain/OtpChallengeTests.cs`
- [x] T026 Verify all domain unit tests pass: `dotnet test backend/tests/Modules/Identity.Tests/`

**Checkpoint**: All domain entities compile and tested. All state machines verified.

---

## Phase 3: Application Layer — Service Interfaces & Infrastructure

**Purpose**: Service interfaces, JWT token service, email/SMS abstractions, rate limiting, EF Core configuration

### Service Interfaces

- [x] T027 [P] Create `ITokenService` interface in `backend/src/Modules/Identity/Application/Services/ITokenService.cs` — GenerateAccessToken, GenerateRefreshToken, ValidateAccessToken, XML docs
- [x] T028 [P] Create `IEmailService` interface in `backend/src/Modules/Identity/Application/Services/IEmailService.cs` — SendVerificationEmail, SendPasswordResetEmail, XML docs
- [x] T029 [P] Create `ISmsService` interface in `backend/src/Modules/Identity/Application/Services/ISmsService.cs` — SendOtpCode, XML docs

### Infrastructure Implementations

- [x] T030 Implement `JwtTokenService` in `backend/src/Modules/Identity/Infrastructure/Services/JwtTokenService.cs` — RS256 signing with kid, configurable expiry, claim mapping (sub, aud, scope), XML docs
- [x] T031 [P] Implement `SmtpEmailService` in `backend/src/Modules/Identity/Infrastructure/Services/SmtpEmailService.cs` — template-based email sending, XML docs
- [x] T032 [P] Implement `SmsGatewayService` in `backend/src/Modules/Identity/Infrastructure/Services/SmsGatewayService.cs` — abstract SMS sending with retry (3x exponential backoff), XML docs
- [x] T033 Implement `RateLimitingMiddleware` in `backend/src/Modules/Identity/Infrastructure/RateLimiting/RateLimitingMiddleware.cs` — Redis sliding window, configurable per-endpoint limits, XML docs
- [x] T034 [P] Implement `RateLimitPolicies` in `backend/src/Modules/Identity/Infrastructure/RateLimiting/RateLimitPolicies.cs` — login (5/15min), OTP (3/challenge), reset (3/hr/email), magic-link (10/day/room)

### EF Core Configuration

- [x] T035 Configure EF Core entity mappings in `backend/src/Modules/Identity/Infrastructure/IdentityDbContext.cs` — all entities with [identity] schema, indexes on Email (unique), PhoneNumber, TokenHash columns

### Unit Tests for Infrastructure

- [x] T036 [P] Write unit tests for `JwtTokenService` in `backend/tests/Modules/Identity.Tests/Infrastructure/JwtTokenServiceTests.cs`
- [x] T037 [P] Write unit tests for `RateLimitingMiddleware` in `backend/tests/Modules/Identity.Tests/Infrastructure/RateLimitingTests.cs`
- [x] T038 Verify all tests pass: `dotnet test backend/tests/Modules/Identity.Tests/`

**Checkpoint**: All service interfaces defined, JWT/rate-limiting implemented and tested.

---

## Phase 4: User Story 1 — Registration & Email Verification (P1) 🎯 MVP

**Goal**: Users can register with email+password and verify their email.
**Independent Test**: Register → receive verification email → click link → account Active.

- [ ] T039 [P] [US1] Create `RegisterUserCommand` and `RegisterUserValidator` in `backend/src/Modules/Identity/Application/Commands/RegisterUserCommand.cs` — email, password, confirmPassword validation (12+ chars, complexity)
- [ ] T040 [US1] Implement `RegisterUserCommandHandler` in `backend/src/Modules/Identity/Application/Commands/RegisterUserCommandHandler.cs` — validate, check duplicate email (generic error), bcrypt hash, create User (Unverified), publish UserRegisteredEvent
- [ ] T041 [P] [US1] Create `EmailVerificationToken` command and handler in `backend/src/Modules/Identity/Application/Commands/GenerateEmailVerificationCommand.cs` — generate token, hash (SHA256), send email, 24hr expiry
- [ ] T042 [US1] Create `VerifyEmailCommand` and handler in `backend/src/Modules/Identity/Application/Commands/VerifyEmailCommand.cs` — validate token, transition User to Active, publish UserEmailVerifiedEvent
- [ ] T043 [P] [US1] Create `ResendVerificationCommand` in `backend/src/Modules/Identity/Application/Commands/ResendVerificationCommand.cs` — invalidate old token, generate new
- [ ] T044 [US1] Create `AuthController` with POST `/api/v1/identity/auth/register`, POST `/api/v1/identity/auth/verify-email`, POST `/api/v1/identity/auth/resend-verification` in `backend/src/Modules/Identity/Api/Controllers/AuthController.cs`
- [ ] T045 [P] [US1] Create request/response DTOs in `backend/src/Modules/Identity/Api/Dtos/` — RegisterRequest, VerifyEmailRequest, ResendVerificationRequest
- [ ] T046 [P] [US1] Write unit tests for RegisterUserCommandHandler in `backend/tests/Modules/Identity.Tests/Application/RegisterUserTests.cs`
- [ ] T047 [US1] Verify all US1 tests pass

**Checkpoint**: Registration + email verification working end-to-end.

---

## Phase 5: User Story 2 — Login with Email & Password (P1)

**Goal**: Users can log in and receive JWT + refresh tokens with session tracking.
**Independent Test**: Login with valid credentials → JWT issued → session created.

- [ ] T048 [US2] Create `LoginCommand` and `LoginValidator` in `backend/src/Modules/Identity/Application/Commands/LoginCommand.cs`
- [ ] T049 [US2] Implement `LoginCommandHandler` in `backend/src/Modules/Identity/Application/Commands/LoginCommandHandler.cs` — rate limit check, validate credentials (generic error on failure), create Session, generate JWT + refresh token, publish UserLoggedInEvent
- [ ] T050 [US2] Add POST `/api/v1/identity/auth/login` to `AuthController` — return LoginResponse with accessToken, set refresh token as HTTP-only cookie (Secure, SameSite=Strict)
- [ ] T051 [P] [US2] Create DTOs: `LoginRequest`, `LoginResponse` in `backend/src/Modules/Identity/Api/Dtos/`
- [ ] T052 [P] [US2] Write unit tests for LoginCommandHandler in `backend/tests/Modules/Identity.Tests/Application/LoginTests.cs` — valid login, invalid password, rate limiting, unverified account
- [ ] T053 [US2] Verify all US2 tests pass

**Checkpoint**: Login working, JWT issued, session persisted.

---

## Phase 6: User Story 3 — Token Refresh & Session Management (P1)

**Goal**: Refresh tokens, list/revoke sessions, idle cleanup.
**Independent Test**: Token expires → refresh → new token issued. List sessions. Revoke one.

- [ ] T054 [US3] Create `RefreshTokenCommand` and handler in `backend/src/Modules/Identity/Application/Commands/RefreshTokenCommand.cs` — validate refresh token hash, check session, issue new JWT
- [ ] T055 [US3] Create `ListSessionsQuery` and handler in `backend/src/Modules/Identity/Application/Queries/ListSessionsQuery.cs` — return sessions with device info, current session flag
- [ ] T056 [US3] Create `RevokeSessionCommand` and handler in `backend/src/Modules/Identity/Application/Commands/RevokeSessionCommand.cs` — mark session Revoked, invalidate refresh token, publish SessionRevokedEvent
- [ ] T057 [P] [US3] Create `RevokeAllOtherSessionsCommand` in `backend/src/Modules/Identity/Application/Commands/RevokeAllOtherSessionsCommand.cs`
- [ ] T058 [P] [US3] Create `IdleSessionCleanupJob` in `backend/src/Modules/Identity/Application/BackgroundJobs/IdleSessionCleanupJob.cs` — expire sessions idle > 24 hours
- [ ] T058b [US3] Create `LogoutCommand` and handler in `backend/src/Modules/Identity/Application/Commands/LogoutCommand.cs` — revoke current session, clear refresh token cookie, publish UserLoggedOutEvent
- [ ] T059 [US3] Create `SessionController` with POST `/api/v1/identity/auth/refresh`, POST `/api/v1/identity/auth/logout`, GET `/api/v1/identity/sessions`, DELETE `/api/v1/identity/sessions/{id}`, DELETE `/api/v1/identity/sessions?exceptCurrent=true` in `backend/src/Modules/Identity/Api/Controllers/SessionController.cs`
- [ ] T060 [P] [US3] Create DTOs: `SessionDto`, `RefreshTokenResponse` in `backend/src/Modules/Identity/Api/Dtos/`
- [ ] T061 [P] [US3] Write unit tests for RefreshTokenCommandHandler, RevokeSessionCommandHandler in `backend/tests/Modules/Identity.Tests/Application/SessionTests.cs`
- [ ] T062 [US3] Verify all US3 tests pass

**Checkpoint**: Token refresh, session list/revoke all working.

---

## Phase 7: User Story 4 — Phone OTP Authentication (P1)

**Goal**: Login via phone OTP for existing users with phone on file.
**Independent Test**: Request OTP → SMS received → enter code → session created.

- [ ] T063 [US4] Create `GenerateOtpChallengeCommand` and handler in `backend/src/Modules/Identity/Application/Commands/GenerateOtpChallengeCommand.cs` — validate phone (E.164), generate 6-digit code (cryptographically secure), store with 15min expiry, send SMS
- [ ] T064 [US4] Create `VerifyOtpCommand` and handler in `backend/src/Modules/Identity/Application/Commands/VerifyOtpCommand.cs` — validate code, check attempts (max 3), create Session on success
- [ ] T065 [P] [US4] Create `ExpiredOtpCleanupJob` in `backend/src/Modules/Identity/Application/BackgroundJobs/ExpiredOtpCleanupJob.cs`
- [ ] T066 [US4] Create `OtpController` with POST `/api/v1/identity/auth/otp/challenge`, POST `/api/v1/identity/auth/otp/verify` in `backend/src/Modules/Identity/Api/Controllers/OtpController.cs`
- [ ] T067 [P] [US4] Create DTOs: `OtpChallengeRequest`, `OtpChallengeResponse`, `OtpVerifyRequest` in `backend/src/Modules/Identity/Api/Dtos/`
- [ ] T068 [P] [US4] Write unit tests for OTP flow in `backend/tests/Modules/Identity.Tests/Application/OtpTests.cs` — valid code, invalid code, max attempts, expired challenge
- [ ] T069 [US4] Verify all US4 tests pass

**Checkpoint**: OTP login working end-to-end for existing users.

---

## Phase 8: User Story 5 — Guest Magic Links (P2)

**Goal**: Organizers generate magic links for guest listen-only room access.
**Independent Test**: Generate link → guest opens → temporary listen-only session.

- [ ] T070 [US5] Create `GenerateGuestMagicLinkCommand` and handler in `backend/src/Modules/Identity/Application/Commands/GenerateGuestMagicLinkCommand.cs` — generate 32-byte token, SHA256 hash, configurable expiry (default 7 days), rate limit (10/room/day)
- [ ] T071 [US5] Create `ValidateGuestMagicLinkQuery` and handler in `backend/src/Modules/Identity/Application/Queries/ValidateGuestMagicLinkQuery.cs` — validate token hash, check expiry/status, create guest session (listen-only)
- [ ] T072 [P] [US5] Create `RevokeMagicLinkCommand` in `backend/src/Modules/Identity/Application/Commands/RevokeMagicLinkCommand.cs`
- [ ] T073 [US5] Create `MagicLinkController` with POST `/api/v1/identity/magic-links`, GET `/api/v1/identity/magic-links/validate`, DELETE `/api/v1/identity/magic-links/{id}` in `backend/src/Modules/Identity/Api/Controllers/MagicLinkController.cs`
- [ ] T074 [P] [US5] Create DTOs in `backend/src/Modules/Identity/Api/Dtos/` — CreateMagicLinkRequest, MagicLinkDto, GuestSessionDto
- [ ] T075 [P] [US5] Write unit tests in `backend/tests/Modules/Identity.Tests/Application/MagicLinkTests.cs`
- [ ] T076 [US5] Verify all US5 tests pass

**Checkpoint**: Magic links working for guest access.

---

## Phase 9: User Story 6 — Personal Access Tokens (P2)

**Goal**: Users create/list/revoke scoped PATs for API integration.
**Independent Test**: Create PAT → use in API request → scope enforced → revoke.

- [ ] T077 [US6] Create `CreatePatCommand` and handler in `backend/src/Modules/Identity/Application/Commands/CreatePatCommand.cs` — generate token, bcrypt hash, display plaintext once, validate scopes, publish PATCreatedEvent
- [ ] T078 [US6] Create `ListPatsQuery` and handler in `backend/src/Modules/Identity/Application/Queries/ListPatsQuery.cs` — return PATs with metadata (never token)
- [ ] T079 [P] [US6] Create `RevokePatCommand` and handler in `backend/src/Modules/Identity/Application/Commands/RevokePatCommand.cs` — mark Revoked, publish PATRevokedEvent
- [ ] T080 [US6] Create `PatController` with POST `/api/v1/identity/pats`, GET `/api/v1/identity/pats`, DELETE `/api/v1/identity/pats/{id}` in `backend/src/Modules/Identity/Api/Controllers/PatController.cs`
- [ ] T081 [P] [US6] Create DTOs in `backend/src/Modules/Identity/Api/Dtos/` — CreatePatRequest, PatCreatedResponse, PatDto
- [ ] T082 [P] [US6] Write unit tests in `backend/tests/Modules/Identity.Tests/Application/PatTests.cs`
- [ ] T083 [US6] Verify all US6 tests pass

**Checkpoint**: PAT CRUD working with scope enforcement.

---

## Phase 10: User Story 7 — Password Reset (P2)

**Goal**: Users can reset forgotten passwords via email.
**Independent Test**: Request reset → email received → click link → new password works.

- [ ] T084 [US7] Create `ForgotPasswordCommand` and handler in `backend/src/Modules/Identity/Application/Commands/ForgotPasswordCommand.cs` — generate token, SHA256 hash, send email (generic response regardless of email existence), rate limit (3/hr/email)
- [ ] T085 [US7] Create `ResetPasswordCommand` and handler in `backend/src/Modules/Identity/Application/Commands/ResetPasswordCommand.cs` — validate token, update password hash, mark token Used, revoke all other sessions, publish PasswordChangedEvent
- [ ] T086 [US7] Add POST `/api/v1/identity/auth/forgot-password` and POST `/api/v1/identity/auth/reset-password` to `AuthController`
- [ ] T087 [P] [US7] Create DTOs: `ForgotPasswordRequest`, `ResetPasswordRequest` in `backend/src/Modules/Identity/Api/Dtos/`
- [ ] T088 [P] [US7] Write unit tests in `backend/tests/Modules/Identity.Tests/Application/PasswordResetTests.cs`
- [ ] T089 [US7] Verify all US7 tests pass

**Checkpoint**: Password reset flow working.

---

## Phase 11: Frontend Auth Pages

**Purpose**: React pages for registration, login, OTP, password reset
**User Stories**: US1, US2, US4, US7

- [ ] T090 [P] Create RTK Query auth API slice in `frontend/src/features/auth/api/authApi.ts` — register, login, verifyEmail, refresh, forgotPassword, resetPassword endpoints
- [ ] T091 [P] Create RTK Query OTP API slice in `frontend/src/features/auth/api/otpApi.ts` — challenge, verify endpoints
- [ ] T092 Create `AuthContext` with token refresh interceptor in `frontend/src/features/auth/context/AuthContext.tsx` — auto-refresh on 401, redirect to login on final failure
- [ ] T093 [P] Create `useAuth` hook in `frontend/src/features/auth/hooks/useAuth.ts` — typed auth state, login/logout/register actions
- [ ] T094 [P] Create `RegisterForm` reusable component in `frontend/src/features/auth/components/RegisterForm.tsx` — email, password, confirmPassword fields, client-side validation
- [ ] T095 [P] Create `LoginForm` reusable component in `frontend/src/features/auth/components/LoginForm.tsx` — email, password fields, forgot password link
- [ ] T096 [P] Create `OtpForm` reusable component in `frontend/src/features/auth/components/OtpForm.tsx` — phone input (E.164), 6-digit code input, countdown timer, resend button
- [ ] T097 Create `RegisterPage` in `frontend/src/features/auth/pages/RegisterPage.tsx`
- [ ] T098 Create `LoginPage` in `frontend/src/features/auth/pages/LoginPage.tsx`
- [ ] T099 Create `OtpLoginPage` in `frontend/src/features/auth/pages/OtpLoginPage.tsx`
- [ ] T100 Create `ForgotPasswordPage` in `frontend/src/features/auth/pages/ForgotPasswordPage.tsx`
- [ ] T101 Create `ResetPasswordPage` in `frontend/src/features/auth/pages/ResetPasswordPage.tsx`
- [ ] T102 Add auth routes to `frontend/src/App.tsx` — /register, /login, /login/otp, /forgot-password, /reset-password
- [ ] T103 Write Playwright E2E test for registration flow in `frontend/tests/e2e/auth/register.spec.ts`
- [ ] T104 [P] Write Playwright E2E test for login flow in `frontend/tests/e2e/auth/login.spec.ts`

**Checkpoint**: All frontend auth pages working with Playwright E2E tests.

---

## Phase 12: Audit Logging & Polish

**Purpose**: Cross-cutting concerns: audit logging, security alerts, documentation

- [ ] T105 Implement audit logging service in `backend/src/Modules/Identity/Application/Services/AuditLogService.cs` — structured Serilog logging for all auth events (FR-019, FR-022), no credentials in logs (FR-018)
- [ ] T105b [P] Write unit test validating no credentials (passwords, tokens, OTP codes) leak into logs in `backend/tests/Modules/Identity.Tests/Application/AuditLogSanitizationTests.cs` — covers SC-009
- [ ] T106 [P] Add OpenAPI/Swagger documentation annotations to all Identity API controllers
- [ ] T107 [P] Create database migration instructions in `docs/runbooks/identity-migration.md` — NEVER AI-generated (Constitution X)
- [ ] T107b Configure audit log retention policy (7-year retention per Saudi PDPL FR-022) — Serilog sink configuration or separate audit database with retention rules
- [ ] T107c [P] Add performance benchmark for auth endpoints in `backend/tests/Modules/Identity.Tests/Infrastructure/AuthPerformanceTests.cs` — verify < 500ms p95 for register, login, refresh (SC-010)
- [ ] T108 Run full test suite: `dotnet test` (backend) + Playwright E2E — all must pass
- [ ] T109 Final validation: all 18 API endpoints responding correctly (including logout), audit logs flowing, no credential leakage

**Checkpoint**: Identity module complete. All tests pass. Audit logging operational. Performance validated.

---

## Dependencies & Execution Order

### Phase Dependencies

```
Phase 1 (Setup) → Phase 2 (Domain) → Phase 3 (Infra/Services) → Phase 4-7 (P1 Stories, sequential)
                                                                → Phase 8-10 (P2 Stories, can parallel after Phase 7)
Phase 4-10 → Phase 11 (Frontend)
Phase 11 → Phase 12 (Polish)
```

### User Story Dependencies

- **US1 (Registration)**: Depends on Phase 2 (domain) + Phase 3 (infra). No story dependencies.
- **US2 (Login)**: Depends on US1 (needs registered users to test).
- **US3 (Sessions)**: Depends on US2 (needs active sessions).
- **US4 (OTP)**: Depends on US2 (needs session infrastructure). Phone OTP is login-only per clarification.
- **US5 (Magic Links)**: Independent of US1-4 (can start after Phase 3).
- **US6 (PATs)**: Independent of US1-4 (can start after Phase 3).
- **US7 (Password Reset)**: Depends on US1 (needs email infrastructure from verification flow).

### Parallel Opportunities

- Phase 2: All value objects (T007-T010), all aggregates (T011-T018), all events (T019), all tests (T020-T025) marked [P]
- Phase 3: Service interfaces (T027-T029) in parallel. Implementations (T030-T034) partially parallel.
- Phase 4-7: Sequential per story, but within each phase many tasks are [P]
- Phase 8-10: Can run in parallel with each other (all depend on Phase 3, not on each other)
- Phase 11: API slices and components (T090-T096) in parallel

---

## Implementation Strategy

### MVP First (Phases 1-5)

1. Phase 1: Module setup
2. Phase 2: Domain entities + tests
3. Phase 3: Infrastructure (JWT, rate limiting)
4. Phase 4: US1 — Registration + email verification
5. Phase 5: US2 — Login
6. **STOP and VALIDATE**: Users can register and login.

### Incremental Delivery

7. Phase 6: US3 — Sessions (refresh, list, revoke)
8. Phase 7: US4 — OTP login
9. Phase 8-10: US5/US6/US7 (magic links, PATs, password reset) — can be parallelized
10. Phase 11: Frontend auth pages
11. Phase 12: Polish, audit, documentation

---

## Git & PR Workflow (per Constitution)

- **GitHub Issues**: Create a GitHub issue for each task before implementation begins. Close it upon completion.
- **Commit after each task** — one Git commit per completed task, not batched.
- **All unit tests MUST pass** before each commit.
- **PR per Phase**: Create a Pull Request at the end of each phase with a detailed summary of all changes.
- **Code Review**: Run code review before submitting any PR. Fix all findings first.
- **Phase Summary**: Include a detailed summary of all implemented tasks when the phase is completed.
- **Database Migrations**: NEVER generate migrations via AI — use `dotnet ef migrations add` only.
- **Aspire AppHost**: Identity module MUST register itself in the Aspire AppHost project.
