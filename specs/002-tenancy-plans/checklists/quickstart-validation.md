# Quickstart Validation Checklist — Tenancy Module

**Date:** 2026-04-03
**Module:** 002-tenancy-plans

Verifies that all quickstart steps work correctly for developers setting up the Tenancy module.

---

## Build & Compile

- [ ] `dotnet build backend/src/Modules/Tenancy/Tenancy.csproj` compiles without errors
- [ ] `dotnet build backend/tests/Modules/Tenancy.Tests/Tenancy.Tests.csproj` compiles without errors
- [ ] No XML documentation warnings (CS1591) in build output

## Tests

- [ ] `dotnet test backend/tests/Modules/Tenancy.Tests/` — all unit tests pass
- [ ] `TenantTests` — tenant lifecycle, slug, branding tests pass
- [ ] `TenantMembershipTests` — membership CRUD tests pass
- [ ] `TenantIsolationTests` — multi-tenant data isolation tests pass

## API Endpoints

Verify the following endpoints are registered and return expected status codes:

### Tenants
- [ ] `POST /api/v1/tenants` — 201 Created (create tenant)
- [ ] `GET /api/v1/tenants/{tenantId}` — 200 OK (get tenant)
- [ ] `PATCH /api/v1/tenants/{tenantId}/branding` — 200 OK (update branding)

### Members
- [ ] `GET /api/v1/tenants/{tenantId}/members` — 200 OK (list members)
- [ ] `POST /api/v1/tenants/{tenantId}/members/invite` — 201 Created (invite member)
- [ ] `POST /api/v1/tenants/{tenantId}/members/accept` — 200 OK (accept invite)
- [ ] `PATCH /api/v1/tenants/{tenantId}/members/{memberId}/role` — 204 No Content (update role)
- [ ] `DELETE /api/v1/tenants/{tenantId}/members/{memberId}` — 204 No Content (remove member)

### Plans
- [ ] `GET /api/v1/tenants/{tenantId}/plan` — 200 OK (get current plan)
- [ ] `GET /api/v1/plans/available` — 200 OK (list available plans)
- [ ] `POST /api/v1/tenants/{tenantId}/plan/upgrade` — 200 OK (upgrade plan)
- [ ] `POST /api/v1/tenants/{tenantId}/plan/downgrade` — 200 OK (downgrade plan)

### Retention
- [ ] `GET /api/v1/tenants/{tenantId}/retention-policies` — 200 OK (get policy)
- [ ] `PATCH /api/v1/tenants/{tenantId}/retention-policies` — 200 OK (update policy)

### Usage
- [ ] `GET /api/v1/tenants/{tenantId}/usage` — 200 OK (get current usage)
- [ ] `GET /api/v1/tenants/{tenantId}/usage/history` — 200 OK (get usage history)

### Feature Toggles
- [ ] `POST /api/v1/feature-toggles` — 201 Created (create toggle)
- [ ] `PATCH /api/v1/feature-toggles/{toggleId}` — 200 OK (update toggle)
- [ ] `GET /api/v1/tenants/{tenantId}/features` — 200 OK (get enabled features)

## Seed Data

- [ ] 5 plan definitions seeded (Free, Trial, Starter, Professional, Enterprise)
- [ ] Plan limits correctly configured for each tier
- [ ] Seed data applied via EF Core `HasData()` in `TenancyDbContext.OnModelCreating`

## Aspire Registration

- [ ] Tenancy module registered in `Muntada.AppHost` project
- [ ] SQL Server resource configured for Tenancy schema
- [ ] Redis resource available for caching
- [ ] RabbitMQ resource available for integration events

## Infrastructure Services

- [ ] `TenancyDbContext` registered in DI container
- [ ] `IFeatureToggleService` registered as scoped service
- [ ] `IPlanLimitService` registered as scoped service
- [ ] `IBrandingService` registered as scoped service
- [ ] `IAlertService` registered as scoped service
- [ ] `TenancyAuditService` registered as scoped service
- [ ] `TenantContextMiddleware` added to request pipeline
