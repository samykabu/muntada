# Implementation Plan: Identity & Access Management

**Branch**: `001-identity-access` | **Date**: 2026-04-03 | **Spec**: [spec.md](spec.md)
**Input**: Feature specification from `/specs/001-identity-access/spec.md` (post-clarification)

## Summary

This plan implements the Identity & Access Management module for Muntada: user registration with email verification, email+password login with JWT/refresh tokens, session management with device tracking, phone OTP passwordless login, guest magic links for listen-only room access, Personal Access Tokens (PATs) for API integration, and password reset flow. The module owns the `[identity]` SQL Server schema and publishes integration events for downstream modules.

## Technical Context

**Language/Version**: C# / .NET 10 (backend), TypeScript 5.x / React 19 (frontend)
**Primary Dependencies**: ASP.NET Core 10, Entity Framework Core, MediatR 14, FluentValidation 12, MassTransit 9.1 (RabbitMQ), BCrypt.Net-Next, System.IdentityModel.Tokens.Jwt, Serilog (backend); React 19, Vite, Redux Toolkit + RTK Query, React Router, Playwright (frontend)
**Storage**: SQL Server (`[identity]` schema), Redis (session cache, rate limit counters, OTP cache)
**Testing**: xUnit + FluentAssertions + Moq (C# unit), Playwright (E2E/integration)
**Target Platform**: Self-managed Kubernetes, GCC region
**Project Type**: Modular monolith module (Clean Architecture: Domain, Application, Infrastructure, Api layers)
**Performance Goals**: Auth endpoints < 500ms p95, OTP SMS < 10 seconds, email delivery < 30 seconds
**Constraints**: GCC data residency, Saudi PDPL 7-year audit retention, no user enumeration, bcrypt cost 12
**Scale/Scope**: 8 entities, 7 user stories (4x P1, 3x P2), 23 functional requirements, ~18 API endpoints
**Dev Orchestration**: .NET Aspire 13.2 (mandatory), Docker Compose (fallback)

## Constitution Check

*GATE: Must pass before Phase 0 research. Re-check after Phase 1 design.*

| Principle | Status | Notes |
|-----------|--------|-------|
| I. Modular Monolith Discipline | PASS | Identity module in own `[identity]` SQL schema, own NuGet project, integration events for cross-module |
| II. Server-Authoritative State | PASS | User status, session lifecycle, token validity all server-managed with explicit state machines |
| III. API-First | PASS | All 15+ endpoints defined before UI. React SPA is one consumer. |
| IV. Test-First | PASS | TDD for auth policy evaluation, session state transitions, rate limiting |
| V. Invite-Only Security | N/A | Room access handled by Rooms module. Guest magic links bridge Identity to Rooms |
| VI. Observability | PASS | Structured logging for all auth events (FR-019, FR-023), OpenTelemetry via ServiceDefaults |
| VII. Explicit Over Implicit | PASS | Explicit state machines for User, Session, OTP, Token. Opaque IDs |
| VIII. Clean Code & Documentation | PASS | Clean Architecture layers, XML docs on all public APIs |
| IX. Component Reusability | PASS | Auth forms as shared React components |
| X. AI-Safe DB Migrations | PASS | EF Core CLI only for `[identity]` schema migrations |
| XI. Comprehensive Testing | PASS | Unit tests for all domain logic, Playwright for auth flows |
| XII. Aspire-First Local Dev | PASS | Identity module registered in Aspire AppHost |

**GATE RESULT: PASS** — No violations.

## Project Structure

### Documentation (this feature)

```text
specs/001-identity-access/
├── plan.md              # This file
├── research.md          # Phase 0 output
├── data-model.md        # Phase 1 output
├── quickstart.md        # Phase 1 output
├── contracts/           # Phase 1 output (API contracts)
├── checklists/          # Quality validation
└── tasks.md             # Task breakdown
```

### Source Code

```text
backend/src/Modules/Identity/
├── Domain/
│   ├── User/              # User aggregate, Email VO, PasswordHash VO, PhoneNumber VO
│   ├── Session/           # Session aggregate, RefreshToken, DeviceInfo VO
│   ├── Otp/               # OtpChallenge aggregate
│   ├── GuestLink/         # GuestMagicLink aggregate
│   ├── Pat/               # PersonalAccessToken aggregate
│   ├── PasswordReset/     # PasswordResetToken entity
│   ├── EmailVerification/ # EmailVerificationToken entity
│   └── Events/            # Integration events (UserRegistered, etc.)
├── Application/
│   ├── Commands/          # RegisterUser, Login, RefreshToken, VerifyOtp, etc.
│   ├── Queries/           # ListSessions, ListPats, ValidateMagicLink
│   ├── Validators/        # FluentValidation validators
│   ├── Services/          # ITokenService, IEmailService, ISmsService
│   └── BackgroundJobs/    # IdleSessionCleanup, ExpiredOtpCleanup
├── Infrastructure/
│   ├── IdentityDbContext.cs
│   ├── Services/          # JwtTokenService, SmtpEmailService, SmsGatewayService
│   ├── Repositories/      # EF Core repositories
│   └── RateLimiting/      # Redis-based sliding window rate limiter
├── Api/
│   ├── Controllers/       # AuthController, SessionController, OtpController, MagicLinkController, PatController
│   └── Dtos/              # Request/Response DTOs
└── Identity.csproj

backend/tests/Modules/Identity.Tests/
├── Domain/                # Unit tests for aggregates, value objects
├── Application/           # Unit tests for command handlers, validators
├── Infrastructure/        # Integration tests for DB, Redis, JWT
└── Identity.Tests.csproj

frontend/src/features/auth/
├── pages/                 # RegisterPage, LoginPage, OtpLoginPage, ForgotPasswordPage, ResetPasswordPage
├── components/            # RegisterForm, LoginForm, OtpForm (reusable per Constitution IX)
├── hooks/                 # useAuth
├── api/                   # RTK Query auth API slices
└── context/               # AuthContext (token refresh interceptor)
```

**Structure Decision**: Identity module follows Clean Architecture within `backend/src/Modules/Identity/`. Own SQL schema `[identity]`. Frontend auth features in `frontend/src/features/auth/`. All forms extracted as reusable components (Constitution IX).

## Complexity Tracking

> No violations to justify. Standard patterns for ASP.NET Core identity module.
