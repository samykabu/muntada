# Quickstart: Tenancy & Plans Module

**Branch**: `002-tenancy-plans` | **Date**: 2026-04-03

## Prerequisites

- .NET 10 SDK installed
- Node.js 20+ and npm
- Docker Desktop running (for Aspire containers)
- Identity module (Epic 1) complete and working
- Git on branch `002-tenancy-plans`

## Local Development Setup

### 1. Start the full stack via Aspire

```bash
dotnet run --project aspire/Muntada.AppHost
```

This provisions SQL Server, Redis, RabbitMQ, MinIO, and the backend API automatically. The Aspire dashboard is at `https://localhost:18888`.

### 2. Verify infrastructure

After Aspire starts, confirm these services are healthy in the dashboard:
- **SQL Server**: `muntadadb` database with `[identity]` schema (existing) and `[tenancy]` schema (new)
- **Redis**: Available for caching and feature toggle cache
- **RabbitMQ**: Management at `http://localhost:15672` (guest/guest)
- **MinIO**: Console at `http://localhost:9001` (minioadmin/minioadmin)

### 3. Run database migrations

```bash
# From the backend directory
cd backend/src/Modules/Tenancy
dotnet ef migrations add InitialTenancy --startup-project ../../Muntada.Api
dotnet ef database update --startup-project ../../Muntada.Api
```

> **IMPORTANT**: Never generate migrations with AI tools. Use `dotnet ef migrations add` CLI only (Constitution Principle X).

### 4. Seed plan definitions

Plan definitions are seeded automatically when the Tenancy module starts. Verify via:
```bash
# Check plans exist
curl -s http://localhost:5000/api/v1/plans/available | jq
```

### 5. Start frontend

```bash
cd frontend
npm install
npm run dev
```

Frontend is at `http://localhost:3000`.

## Development Workflow

### Adding a new command/handler

1. Create command class in `Application/Commands/`
2. Create FluentValidation validator in `Application/Validators/`
3. Create handler implementing `IRequestHandler<TCommand, TResponse>`
4. Write unit tests FIRST (TDD for critical paths — Constitution Principle IV)
5. Run tests: `dotnet test`
6. Create API endpoint in appropriate controller
7. Commit per task (Constitution: Git & PR Discipline)

### Testing

```bash
# Run all Tenancy unit tests
dotnet test backend/tests/Modules/Tenancy.Tests --filter "Category=Unit"

# Run integration tests
dotnet test backend/tests/Modules/Tenancy.Tests --filter "Category=Integration"

# Run frontend tests
cd frontend && npm run test:unit

# Run E2E tests (requires Aspire running)
cd frontend && npm run test:e2e
```

### Key patterns to follow (from Identity module)

| Pattern | Example in Identity | Apply to Tenancy |
|---------|-------------------|-----------------|
| Aggregate root | `User : AggregateRoot<Guid>` | `Tenant : AggregateRoot<Guid>` |
| Value object | `Email : ValueObject` | `TenantSlug : ValueObject`, `PlanLimits : ValueObject` |
| Command + handler | `RegisterUserCommand` | `CreateTenantCommand` |
| FluentValidation | `RegisterUserValidator` | `CreateTenantValidator` |
| Integration event | `IIntegrationEvent` | `TenantCreatedEvent`, `PlanChangedEvent` |
| Opaque IDs | `usr_`, `pat_` | `tnt_`, `mbr_`, `pln_` |
| DB schema | `[identity]` | `[tenancy]` |
| MediatR pipeline | `ValidationBehavior` | Same (shared from SharedKernel) |

### Registering in the API gateway

Add to `backend/src/Muntada.Api/Program.cs`:
```csharp
// Register Tenancy module
builder.Services.AddDbContext<TenancyDbContext>(options =>
    options.UseSqlServer(builder.Configuration.GetConnectionString("muntadadb")));

builder.Services.AddMediatR(cfg =>
    cfg.RegisterServicesFromAssembly(typeof(TenancyDbContext).Assembly));
```

### Registering in Aspire AppHost

The Tenancy module uses the same backend API project — no separate AppHost entry needed. Verify the API project references the Tenancy module in its `.csproj`.

## Key Files Reference

| Purpose | Path |
|---------|------|
| Source spec (detailed) | `specs/002-tenancy-plans/spec-source.md` |
| Source tasks (detailed) | `specs/002-tenancy-plans/tasks-source.md` |
| Data model | `specs/002-tenancy-plans/data-model.md` |
| API contracts | `specs/002-tenancy-plans/contracts/` |
| SharedKernel base classes | `backend/src/Muntada.SharedKernel/Domain/` |
| Identity module (reference) | `backend/src/Modules/Identity/` |
| Aspire AppHost | `aspire/Muntada.AppHost/AppHost.cs` |
