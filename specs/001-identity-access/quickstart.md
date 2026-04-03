# Quickstart: Identity & Access Management

**Epic**: 001-identity-access
**Date**: 2026-04-03

---

## Prerequisites

1. **Epic 0 (Foundation) must be complete.** The shared kernel, Aspire AppHost, database infrastructure, and CI/CD pipeline must be operational.
2. **.NET 8 SDK** installed (verify: `dotnet --version`).
3. **Node.js 20+** installed (verify: `node --version`).
4. **Docker Desktop** running (required by Aspire for SQL Server, Redis, RabbitMQ, MailDev containers).

---

## Running the Identity Module Locally

### 1. Start the Aspire AppHost

The Aspire AppHost orchestrates all infrastructure dependencies (SQL Server, Redis, RabbitMQ, MailDev) and the backend API.

```bash
dotnet run --project aspire/Muntada.AppHost
```

This will:
- Provision SQL Server with the `[identity]` schema
- Start Redis for session cache and rate limiting
- Start RabbitMQ for integration events
- Start MailDev for capturing outbound emails (http://localhost:1080)
- Start the backend API (http://localhost:5000)

### 2. Verify services are healthy

Open the Aspire Dashboard (URL printed in console output, typically http://localhost:15888) and confirm all resources show a green "Running" status.

### 3. Start the frontend (if applicable)

```bash
cd frontend
npm install
npm run dev
```

Frontend will be available at http://localhost:3000.

---

## Testing Registration & Login Manually

### Register a new user

```bash
curl -X POST http://localhost:5000/api/v1/identity/auth/register \
  -H "Content-Type: application/json" \
  -d '{
    "email": "test@example.com",
    "password": "TestP@ssw0rd123",
    "confirmPassword": "TestP@ssw0rd123",
    "fullName": "Test User"
  }'
```

Expected: `201 Created` with user DTO (status: "Unverified").

### Verify email

1. Open MailDev at http://localhost:1080
2. Find the verification email sent to `test@example.com`
3. Copy the verification token from the email link
4. Call the verify endpoint:

```bash
curl -X POST http://localhost:5000/api/v1/identity/auth/verify-email \
  -H "Content-Type: application/json" \
  -d '{"token": "<token-from-email>"}'
```

Expected: `200 OK` with success message.

### Log in

```bash
curl -X POST http://localhost:5000/api/v1/identity/auth/login \
  -H "Content-Type: application/json" \
  -c cookies.txt \
  -d '{
    "email": "test@example.com",
    "password": "TestP@ssw0rd123"
  }'
```

Expected: `200 OK` with `accessToken` in body and `refresh_token` cookie saved to `cookies.txt`.

### Access a protected endpoint

```bash
curl http://localhost:5000/api/v1/identity/sessions \
  -H "Authorization: Bearer <accessToken>"
```

Expected: `200 OK` with session list including the current session.

### Refresh the access token

```bash
curl -X POST http://localhost:5000/api/v1/identity/auth/refresh \
  -b cookies.txt \
  -c cookies.txt
```

Expected: `200 OK` with a new `accessToken` and updated refresh token cookie.

---

## Running Unit Tests

```bash
dotnet test backend/tests/Modules/Identity.Tests/
```

This executes all xUnit tests for the Identity module, including:
- Domain entity state machine tests
- Application service tests (registration, login, token management)
- FluentValidation request validator tests
- Rate limiting logic tests

To run with detailed output:

```bash
dotnet test backend/tests/Modules/Identity.Tests/ --verbosity normal --logger "console;verbosity=detailed"
```

---

## Running Playwright E2E Auth Tests

### Prerequisites

Install Playwright browsers (first time only):

```bash
cd frontend
npx playwright install
```

### Run E2E tests

```bash
cd frontend
npx playwright test --project=auth
```

This runs end-to-end tests covering:
- Full registration flow (form submission, email verification)
- Login flow (valid/invalid credentials, error messages)
- Session management (list sessions, revoke session)
- Token refresh (automatic re-authentication)
- Password reset flow
- Rate limiting behavior (lockout after failed attempts)

To run in headed mode for debugging:

```bash
npx playwright test --project=auth --headed
```

To view the test report:

```bash
npx playwright show-report
```

---

## API Endpoint Reference

| Method | Endpoint | Auth | Description |
|---|---|---|---|
| POST | `/api/v1/identity/auth/register` | No | Register new user |
| POST | `/api/v1/identity/auth/login` | No | Login with email/password |
| POST | `/api/v1/identity/auth/verify-email` | No | Verify email address |
| POST | `/api/v1/identity/auth/resend-verification` | No | Resend verification email |
| POST | `/api/v1/identity/auth/refresh` | Cookie | Refresh access token |
| GET | `/api/v1/identity/sessions` | Bearer | List user sessions |
| DELETE | `/api/v1/identity/sessions/{sessionId}` | Bearer | Revoke a session |
| DELETE | `/api/v1/identity/sessions?exceptCurrent=true` | Bearer | Revoke all other sessions |
| POST | `/api/v1/identity/auth/otp/challenge` | No | Request OTP code |
| POST | `/api/v1/identity/auth/otp/verify` | No | Verify OTP code |
| POST | `/api/v1/identity/magic-links` | Bearer | Create guest magic link |
| GET | `/api/v1/identity/magic-links/validate?token=...` | No | Validate magic link |
| DELETE | `/api/v1/identity/magic-links/{linkId}` | Bearer | Revoke magic link |
| POST | `/api/v1/identity/pats` | Bearer | Create PAT |
| GET | `/api/v1/identity/pats` | Bearer | List PATs |
| DELETE | `/api/v1/identity/pats/{patId}` | Bearer | Revoke PAT |
| POST | `/api/v1/identity/auth/forgot-password` | No | Request password reset |
| POST | `/api/v1/identity/auth/reset-password` | No | Reset password |

---

## Database Migrations

**CRITICAL (Constitution Principle X)**: Database migrations MUST NEVER be generated by AI tooling. All migrations MUST be created using the EF Core CLI.

### Creating a new migration

```bash
cd backend/src/Modules/Identity
dotnet ef migrations add <MigrationName> \
  --project Muntada.Identity.Infrastructure \
  --startup-project ../../../Muntada.Api \
  --context IdentityDbContext
```

### Applying migrations

Migrations are applied automatically on Aspire startup in the development environment. For manual application:

```bash
dotnet ef database update \
  --project backend/src/Modules/Identity/Muntada.Identity.Infrastructure \
  --startup-project backend/src/Muntada.Api \
  --context IdentityDbContext
```

### Reverting a migration

```bash
dotnet ef database update <PreviousMigrationName> \
  --project backend/src/Modules/Identity/Muntada.Identity.Infrastructure \
  --startup-project backend/src/Muntada.Api \
  --context IdentityDbContext
```

### Removing the last migration (if not applied)

```bash
dotnet ef migrations remove \
  --project backend/src/Modules/Identity/Muntada.Identity.Infrastructure \
  --startup-project backend/src/Muntada.Api \
  --context IdentityDbContext
```

---

## Troubleshooting

### "Connection refused" on API calls

- Verify Aspire AppHost is running: check the Aspire Dashboard for service status.
- Confirm the API is listening on the expected port (default: 5000).
- Check that Docker Desktop is running (SQL Server, Redis, RabbitMQ run in containers).

### "Invalid email or password" when credentials are correct

- Check that the user's email is verified (status must be "Active", not "Unverified").
- Unverified accounts return 403, not 401. Check the response status code.
- Verify the user was created against the correct database (check connection string in Aspire config).

### Verification email not appearing in MailDev

- Open MailDev UI at http://localhost:1080 and check all messages.
- Verify the SMTP configuration in `appsettings.Development.json` points to the MailDev SMTP port (default: 1025).
- Check Aspire logs for SMTP connection errors.

### Rate limit hit during development

- Rate limits are enforced via Redis. To reset during development, flush the Redis rate limit keys:
  ```bash
  redis-cli KEYS "rate:*" | xargs redis-cli DEL
  ```
- Alternatively, temporarily increase rate limits in `appsettings.Development.json`.

### OTP SMS not delivered in development

- In development, the `ConsoleSmsService` is used instead of Twilio. OTP codes are logged to the console output.
- Check the Aspire Dashboard logs for the API service to find the OTP code.

### Redis connection errors

- Verify Redis is running in Docker: `docker ps | grep redis`.
- Check the Redis connection string in Aspire resource configuration.
- If Redis is down, sessions fall back to SQL Server (slower but functional).

### Migration errors

- Ensure you are running the EF CLI command from the correct directory.
- Verify the `--startup-project` path is correct for your machine.
- If the migration snapshot is out of sync, do NOT manually edit migration files. Remove the last migration and recreate it.
- Remember: AI tools must never generate migration files (Constitution Principle X).

### JWT validation errors

- Verify the RSA key pair is configured in the development environment.
- Check that the `kid` in the JWT header matches an available key in the JWKS endpoint.
- Confirm clock skew settings in `TokenValidationParameters` allow for minor time differences.
