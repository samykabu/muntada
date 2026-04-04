# Security Review Checklist — Tenancy Module

**Date:** 2026-04-03
**Module:** 002-tenancy-plans
**Reviewer:** TODO

Items marked `[ ]` require manual verification. Mark `[x]` when reviewed and confirmed.

---

## Controller Authorization

### TenantsController (`/api/v1/tenants`)

- [ ] `POST /tenants` — Authorization attribute present (authenticated users only)
- [ ] `GET /tenants/{tenantId}` — Tenant membership verified before returning data
- [ ] `PATCH /tenants/{tenantId}/branding` — Only Owner/Admin can modify branding
- [ ] CreatedBy extracted from authenticated user claims (currently TODO placeholder)

### MembersController (`/api/v1/tenants/{tenantId}/members`)

- [ ] `GET /members` — Caller must be a member of the tenant
- [ ] `POST /members/invite` — Only Owner/Admin can invite
- [ ] `POST /members/invite` — Admin cannot invite with Owner role
- [ ] `POST /members/accept` — UserId extracted from authenticated user claims (currently TODO placeholder)
- [ ] `PATCH /members/{memberId}/role` — Only Owner can change roles
- [ ] `DELETE /members/{memberId}` — Admin cannot remove Owner
- [ ] `DELETE /members/{memberId}` — Last Owner cannot be removed
- [ ] InvitedBy/RequestedBy extracted from authenticated user claims (currently TODO placeholder)

### PlansController (`/api/v1`)

- [ ] `GET /tenants/{tenantId}/plan` — Caller must be a member of the tenant
- [ ] `GET /plans/available` — Public or requires authentication
- [ ] `POST /tenants/{tenantId}/plan/upgrade` — Only Owner/Admin can upgrade
- [ ] `POST /tenants/{tenantId}/plan/downgrade` — Only Owner/Admin can downgrade
- [ ] RequestedBy extracted from authenticated user claims (currently TODO placeholder)

### RetentionController (`/api/v1/tenants/{tenantId}/retention-policies`)

- [ ] `GET /retention-policies` — Caller must be a member of the tenant
- [ ] `PATCH /retention-policies` — Only Owner/Admin can update retention policy

### UsageController (`/api/v1/tenants/{tenantId}/usage`)

- [ ] `GET /usage` — Caller must be a member of the tenant
- [ ] `GET /usage/history` — Caller must be a member of the tenant

### FeatureTogglesController

- [ ] `POST /feature-toggles` — Platform admin only (system-level operation)
- [ ] `PATCH /feature-toggles/{toggleId}` — Platform admin only
- [ ] `GET /tenants/{tenantId}/features` — Caller must be a member of the tenant

---

## Tenant Context Enforcement

- [ ] `TenantContextMiddleware` extracts tenant context from request (header/route)
- [ ] All tenant-scoped queries filter by tenant ID from context (not user input)
- [ ] Tenant ID from URL route is validated against authenticated user's memberships

---

## Cross-Tenant Data Leakage

- [ ] `GetTenantQuery` — Returns null for non-existent tenant IDs
- [ ] `GetTenantMembersQuery` — Filters memberships by TenantId
- [ ] `GetTenantPlanQuery` — Scoped to requesting tenant
- [ ] `CheckPlanLimitsQuery` — Scoped to requesting tenant
- [ ] `GetRetentionPolicyQuery` — Scoped to requesting tenant
- [ ] `GetTenantUsageQuery` — Scoped to requesting tenant
- [ ] `GetUsageHistoryQuery` — Scoped to requesting tenant
- [ ] Feature toggle overrides queried by tenant ID only
- [ ] No endpoint returns data for multiple tenants in a single response

---

## Suspension Enforcement

- [ ] Suspended tenants cannot create rooms or access features
- [ ] Suspended tenants can still view their data (read-only)
- [ ] `FeatureToggleMiddleware` respects tenant suspension status
- [ ] API responses include appropriate 403 for suspended tenants

---

## Role-Based Access Control

- [ ] `TenantRole` enum: Owner > Admin > Member hierarchy enforced
- [ ] Owner role cannot be assigned via `InviteMember` by Admin
- [ ] Owner role change requires existing Owner authorization
- [ ] Last Owner cannot be removed or downgraded
- [ ] Admin cannot modify Owner memberships

---

## Invite Token Security

- [ ] Invite tokens are cryptographically random (sufficient entropy)
- [ ] Invite tokens have expiration (7-day default)
- [ ] Expired tokens are rejected during acceptance
- [ ] Used tokens are invalidated after acceptance
- [ ] Token uniqueness enforced at database level (unique index)
- [ ] Tokens are not logged in plain text

---

## Data Protection

- [ ] Sensitive fields not exposed in API responses (e.g., invite tokens in list endpoints)
- [ ] Pagination prevents unbounded result sets
- [ ] Input validation present on all command handlers (FluentValidation)
- [ ] SQL injection prevented by parameterized queries (EF Core)

---

## CSRF Protection

- [ ] Anti-forgery tokens required for state-changing operations (when using cookie auth)
- [ ] Note: Currently API-only with bearer tokens; CSRF not applicable for token-based auth
- [ ] Review needed when cookie-based session auth is added

---

## Logging & Audit

- [ ] Sensitive data not logged (invite tokens, user emails in error messages)
- [ ] Structured logging includes tenant context for all operations
- [ ] `TenancyAuditService` captures state changes for compliance

---

## Summary

| Category                    | Items | Status   |
|-----------------------------|-------|----------|
| Controller Authorization    | 19    | TODO     |
| Tenant Context Enforcement  | 3     | TODO     |
| Cross-Tenant Data Leakage   | 9     | TODO     |
| Suspension Enforcement      | 4     | TODO     |
| Role-Based Access Control   | 5     | TODO     |
| Invite Token Security       | 6     | TODO     |
| Data Protection             | 4     | TODO     |
| CSRF Protection             | 3     | TODO     |
| Logging & Audit             | 3     | TODO     |
| **Total**                   | **56**| **TODO** |
