# Muntada — Spec Kit Index

**Project**: Muntada — Multi-tenant SaaS for moderated audio rooms
**Phase**: Phase 1 (Web)
**Timeline**: 12 weeks (aggressive)
**Team**: 3-4 developers + AI-assisted development
**Region**: GCC only

## Architecture

React SPA (TypeScript) + ASP.NET Core 8+ modular monolith (C#) + LiveKit OSS
SQL Server | Redis | RabbitMQ | MinIO | Self-hosted Kubernetes

## Master Plan

- [Implementation Plan](./plan.md) — Full epic breakdown, dependency graph, team allocation, risk analysis

## Constitution

- [Project Constitution](../.specify/memory/constitution.md) — 7 core principles, technology constraints, governance

## Epic Specifications & Tasks

### Critical Path (Sequential)

| # | Epic | Spec | Tasks | Week | Dependencies |
|---|------|------|-------|------|--------------|
| 0 | Foundation & Infrastructure | [spec](./000-foundation/spec.md) | [tasks](./000-foundation/tasks.md) | 1-2 | None |
| 1 | Identity & Access | [spec](./001-identity-access/spec.md) | [tasks](./001-identity-access/tasks.md) | 2-3 | Epic 0 |
| 2 | Tenancy & Plans | [spec](./002-tenancy-plans/spec.md) | [tasks](./002-tenancy-plans/tasks.md) | 3-4 | Epic 0, 1 |
| 3 | Rooms & Scheduling | [spec](./003-rooms-scheduling/spec.md) | [tasks](./003-rooms-scheduling/tasks.md) | 4-5 | Epic 1, 2 |
| 4 | Realtime Orchestration | [spec](./004-realtime-orchestration/spec.md) | [tasks](./004-realtime-orchestration/tasks.md) | 5-6 | Epic 0, 1, 2, 3 |

### Parallelizable (after Epic 4)

| # | Epic | Spec | Tasks | Week | Dependencies |
|---|------|------|-------|------|--------------|
| 5 | Moderation | [spec](./005-moderation/spec.md) | [tasks](./005-moderation/tasks.md) | 6-7 | Epic 3, 4 |
| 6 | Chat & Messaging | [spec](./006-chat-messaging/spec.md) | [tasks](./006-chat-messaging/tasks.md) | 7-8 | Epic 3, 4 |
| 7 | Recording & Consent | [spec](./007-recording-consent/spec.md) | [tasks](./007-recording-consent/tasks.md) | 8-9 | Epic 3, 4, 5 |
| 8 | Files & Artifacts | [spec](./008-files-artifacts/spec.md) | [tasks](./008-files-artifacts/tasks.md) | 9-10 | Epic 2, 3 |

### Late-Stage (after core modules stable)

| # | Epic | Spec | Tasks | Week | Dependencies |
|---|------|------|-------|------|--------------|
| 9 | Billing & Metering | [spec](./009-billing-metering/spec.md) | [tasks](./009-billing-metering/tasks.md) | 10-11 | Epic 2, 3 |
| 10 | Reporting & Audit | [spec](./010-reporting-audit/spec.md) | [tasks](./010-reporting-audit/tasks.md) | 11 | Epic 2, all event producers |
| 11 | Admin & Support | [spec](./011-admin-support/spec.md) | [tasks](./011-admin-support/tasks.md) | 11-12 | Epic 1, 2, 3, 10 |
| 12 | Public API & Webhooks | [spec](./012-public-api-webhooks/spec.md) | [tasks](./012-public-api-webhooks/tasks.md) | 12 | Epic 1, 2, 3+ |

## Dependency Graph

```
Epic 0 (Foundation)
  └─→ Epic 1 (Identity)
       └─→ Epic 2 (Tenancy)
            └─→ Epic 3 (Rooms)
                 └─→ Epic 4 (Realtime)
                      ├─→ Epic 5 (Moderation) ──→ Epic 7 (Recording)
                      ├─→ Epic 6 (Chat)
                      └─→ Epic 8 (Files)
            ├─→ Epic 9 (Billing)
            └─→ Epic 10 (Reporting) ──→ Epic 11 (Admin)
       └─→ Epic 12 (Public API)
```

## Parallelism Opportunities

After Epic 4 completes (Week 6), the following can run concurrently across team members:

- **Dev A (Backend)**: Epic 5 (Moderation) → Epic 7 (Recording)
- **Dev B (Backend)**: Epic 6 (Chat) → Epic 8 (Files)
- **Dev C (Full-stack)**: Epic 9 (Billing) + Epic 10 (Reporting)
- **Dev D / AI**: Epic 11 (Admin) + Epic 12 (Public API)

## Spec Kit Commands

Once working in a Claude Code session within this project:

```
/speckit-constitution    # View project principles
/speckit-specify         # Create new feature spec
/speckit-plan            # Generate implementation plan
/speckit-tasks           # Break down into tasks
/speckit-implement       # Execute implementation
/speckit-clarify         # De-risk ambiguous areas
/speckit-analyze         # Cross-artifact consistency check
/speckit-checklist       # Quality validation checklist
```

## Key Decisions Captured

- Auth: Email+password AND phone OTP both supported
- Encryption: Transport-only Phase 1, E2EE-ready architecture
- Payments: HyperPay/Moyasar (GCC local methods)
- AI features: Strictly deferred, only schemas reserved
- Public site: Static marketing/landing page
- Infrastructure: All self-hosted on Kubernetes (SQL Server, Redis, RabbitMQ, MinIO)
- Scope: Full Phase 1 (all 8 slices) — aggressive 3-month timeline
