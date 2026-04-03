# Muntada: Master Implementation Plan
**Phase 1 — Complete Moderated Audio Room Platform**

**Version:** 1.0
**Date:** 2026-04-03
**Timeline:** 3 months (12 weeks) aggressive
**Team:** 3-4 developers + AI-assisted development
**Status:** Planning phase

---

## Table of Contents

1. [Project Overview](#project-overview)
2. [Technical Context](#technical-context)
3. [Project Structure](#project-structure)
4. [Epic Breakdown (13 Epics)](#epic-breakdown)
5. [Dependency Graph](#dependency-graph)
6. [Team Allocation Strategy](#team-allocation-strategy)
7. [Critical Path Analysis](#critical-path-analysis)
8. [Risk Assessment](#risk-assessment)
9. [Success Criteria](#success-criteria)

---

## Project Overview

**Muntada** is a multi-tenant SaaS platform for moderated audio rooms with scheduling, real-time orchestration, and consent-first recording. The platform targets GCC organizations requiring Saudi PDPL compliance.

### Phase 1 Scope

- Multi-tenant architecture with branding and subdomain support
- Live audio rooms with scheduling and recurring series
- Speaker management, raise-hand queue, and moderator controls
- Local and server-side recording with consent workflows
- File uploads, malware scanning, and targeted delivery
- Subscription billing with concurrent room metering
- Email + phone OTP authentication with guest support
- Transport-level encryption; E2EE-ready architecture
- Admin dashboard and tenant analytics
- Public API with PAT authentication and webhooks

### Non-Scope (Phase 2+)

- End-to-end encryption (E2EE) implementation
- AI-powered transcription and analysis
- Advanced analytics and ML features
- Mobile native apps (web-first only)

---

## Technical Context

### Tech Stack

| Layer | Technology | Notes |
|-------|-----------|-------|
| **Frontend** | React 18 (SPA) + TypeScript | Vite bundler, responsive design |
| **Backend** | ASP.NET Core 8 (modular monolith) | C#, modular structure, vertical slices |
| **Real-time** | LiveKit OSS (self-hosted) | WebRTC signaling, room orchestration |
| **Signaling** | ASP.NET Core SignalR | Fan-out for presence, state sync |
| **Database** | SQL Server (self-hosted) | ACID transactions, audit trails |
| **Cache** | Redis (self-hosted) | Session, room state, presence |
| **Message Queue** | RabbitMQ (self-hosted) | Async workflows, event sourcing |
| **Storage** | MinIO (self-hosted) | S3-compatible, recordings, uploads |
| **Container** | Kubernetes (self-hosted) | Orchestration, scaling, observability |
| **Observability** | Prometheus + Grafana + ELK | Metrics, logs, traces |
| **Auth** | JWT + Email + Phone OTP | Custom identity provider |
| **Payments** | HyperPay + Moyasar | GCC local providers |

### Compliance & Regions

- **Region:** GCC only (Saudi Arabia primary)
- **Data Residency:** On-premise, self-hosted infrastructure
- **Compliance:** Saudi PDPL (Personal Data Protection Law)
- **Encryption:** TLS 1.3 for transport; E2EE deferred to Phase 2

### Key Constraints

- Self-hosted infrastructure (no third-party SaaS dependencies)
- Limited DevOps team; infrastructure as code required
- 3-4 developers; AI-assisted development critical
- Aggressive 3-month timeline; parallelism essential
- Modular monolith; future microservice-ready design

---

## Project Structure

```
Muntada/
├── backend/                          # ASP.NET Core modular monolith
│   ├── src/
│   │   ├── Muntada.Core/            # Shared kernel, domain models
│   │   ├── Muntada.Identity/        # Auth, credentials, JWT, OTP
│   │   ├── Muntada.Tenancy/         # Multi-tenancy, branding, plans
│   │   ├── Muntada.Rooms/           # Rooms, templates, scheduling
│   │   ├── Muntada.Realtime/        # LiveKit integration, SignalR
│   │   ├── Muntada.Moderation/      # Raise hand, speaker queue, roster
│   │   ├── Muntada.Chat/            # Room chat, moderator DM
│   │   ├── Muntada.Recording/       # Local/server recording, manifests
│   │   ├── Muntada.Storage/         # File uploads, malware scanning
│   │   ├── Muntada.Billing/         # Metering, subscription state
│   │   ├── Muntada.Reporting/       # Analytics, audit trails
│   │   ├── Muntada.Admin/           # Tenant mgmt, interventions
│   │   ├── Muntada.Api/             # Public API, webhooks, PAT
│   │   └── Muntada.Web/             # ASP.NET Core host, middleware
│   ├── tests/
│   │   ├── Muntada.UnitTests/
│   │   ├── Muntada.IntegrationTests/
│   │   └── Muntada.E2ETests/
│   ├── Muntada.sln
│   └── docker/
│       ├── Dockerfile
│       ├── docker-compose.yml       # Local dev environment
│       └── k8s/                     # Kubernetes manifests
│
├── frontend/                         # React + TypeScript SPA
│   ├── src/
│   │   ├── components/
│   │   │   ├── Auth/                # Login, OTP, registration
│   │   │   ├── Rooms/               # Room creation, join, schedule
│   │   │   ├── Realtime/            # Audio controls, participant list
│   │   │   ├── Moderation/          # Raise hand, speaker controls
│   │   │   ├── Chat/                # Room chat, DM
│   │   │   ├── Recording/           # Recording indicator, consent
│   │   │   ├── Admin/               # Tenant dashboard, analytics
│   │   │   └── Shared/              # UI lib, buttons, modals
│   │   ├── pages/                   # Route-based pages
│   │   ├── hooks/                   # Custom React hooks
│   │   ├── services/                # API clients, WebSocket
│   │   ├── store/                   # State management (Zustand/Redux)
│   │   ├── types/                   # TypeScript interfaces
│   │   ├── styles/                  # Tailwind CSS, theme
│   │   ├── utils/                   # Helpers, validators
│   │   ├── App.tsx
│   │   └── main.tsx
│   ├── public/
│   │   ├── index.html
│   │   └── assets/
│   ├── tests/
│   │   ├── unit/
│   │   ├── integration/
│   │   └── e2e/                     # Playwright tests
│   ├── vite.config.ts
│   ├── tsconfig.json
│   └── package.json
│
├── infra/                            # Infrastructure as Code
│   ├── kubernetes/
│   │   ├── base/
│   │   │   ├── backend-deployment.yaml
│   │   │   ├── frontend-deployment.yaml
│   │   │   ├── livekit-deployment.yaml
│   │   │   ├── postgres-statefulset.yaml
│   │   │   ├── redis-deployment.yaml
│   │   │   ├── rabbitmq-deployment.yaml
│   │   │   ├── minio-deployment.yaml
│   │   │   ├── prometheus-deployment.yaml
│   │   │   ├── grafana-deployment.yaml
│   │   │   └── elasticsearch-deployment.yaml
│   │   ├── overlays/
│   │   │   ├── dev/
│   │   │   ├── staging/
│   │   │   └── production/
│   │   ├── kustomization.yaml
│   │   └── README.md
│   ├── terraform/                   # Alternative to Kustomize (optional)
│   ├── helm/                        # Helm charts for services
│   ├── scripts/
│   │   ├── setup-cluster.sh
│   │   ├── deploy.sh
│   │   ├── backup.sh
│   │   └── monitoring.sh
│   └── README.md
│
├── docs/                             # Documentation
│   ├── ARCHITECTURE.md              # System design, modules
│   ├── API.md                        # API reference
│   ├── DEPLOYMENT.md                # Setup, CI/CD, ops runbooks
│   ├── DEVELOPMENT.md               # Dev environment, contributing
│   ├── COMPLIANCE.md                # PDPL, privacy, security
│   ├── SCHEMAS.md                   # Database, event, message schemas
│   └── RUNBOOKS/
│       ├── emergency-revoke.md
│       ├── incident-response.md
│       ├── backup-restore.md
│       └── scaling-guide.md
│
├── .github/
│   ├── workflows/
│   │   ├── ci-backend.yml
│   │   ├── ci-frontend.yml
│   │   ├── deploy-staging.yml
│   │   └── deploy-production.yml
│   └── CODEOWNERS
│
├── docker-compose.dev.yml           # Local development environment
├── .env.example
├── README.md
└── CHANGELOG.md
```

---

## Epic Breakdown

### Epic 0: Foundation (Weeks 1–2)

**Summary**
Establish repository structure, CI/CD pipelines, local development environment, Kubernetes cluster provisioning, base observability stack, and shared kernel domain models. This epic ensures the team can develop, test, and deploy code reliably from day one.

**Dependencies**
None (critical path start)

**Modules Involved**

- `.github/workflows/` — GitHub Actions for CI/CD
- `infra/kubernetes/` — K8s manifests, helm charts
- `backend/src/Muntada.Core/` — Shared domain models, DTOs, exceptions
- `docker-compose.dev.yml` — Local development stack
- Observability — Prometheus, Grafana, ELK stack setup

**Key Deliverables**

1. GitHub repository with branch protection and CODEOWNERS
2. Working CI/CD pipeline (lint, build, test, security scan)
3. Kubernetes cluster provisioned and verified
4. Docker Compose local dev environment (all services)
5. Base observability: Prometheus scraping K8s metrics, Grafana dashboards, ELK for logs
6. Shared kernel: Base entity, aggregate root, result types, domain events
7. Environment variable management (dev, staging, production)
8. Database schema (initial tables: tenants, users, audit logs)

**Week Target**
Week 1–2

**Parallelism Notes**

- K8s provisioning and docker-compose setup can run in parallel
- CI/CD pipeline setup independent from backend/frontend scaffolding
- Shared kernel design should be finalized before Epic 1 starts
- Observability stack can be enhanced incrementally

**Success Metrics**

- All developers can clone repo and run `docker-compose up` (all services healthy)
- GitHub Actions pipeline green for every commit
- Kubernetes cluster passes health checks; basic application deployed
- Grafana accessible with system metrics visible
- Database migrations run automatically on startup

---

### Epic 1: Identity & Access (Weeks 2–3)

**Summary**
Implement authentication and credential management: email/password accounts, phone OTP, JWT token issuance/refresh, session management, guest magic links, and organization switching. This is foundational for all user-facing features.

**Dependencies**

- Epic 0 (shared kernel, database foundation)

**Modules Involved**

- `Muntada.Identity/` — Core auth logic
- `Muntada.Core/` — User, credential aggregates
- Frontend: `components/Auth/`, `services/api/auth`
- Database: Users, Credentials, Sessions, OTP, MagicLinks tables

**Key Deliverables**

1. User registration (email + password validation)
2. Email/password login with JWT + refresh token flow
3. Phone OTP registration and login flow
4. Guest magic link generation and validation
5. Session management (Redis-backed)
6. Token refresh endpoint with sliding window
7. Logout and token revocation
8. Organization (tenant) switching by authenticated user
9. Password reset flow (email-based)
10. Frontend login/register pages, OTP input component
11. Auth guard middleware (backend); protected routes (frontend)
12. Unit tests (90%+ coverage), integration tests for auth flows

**Week Target**
Week 2–3

**Parallelism Notes**

- Email/password and OTP flows can be developed in parallel
- Frontend login components independent from backend token logic
- Magic link feature can be deferred to week 3 if needed
- Password reset can be implemented concurrently with login

**Success Metrics**

- Successful registration and login with email/password
- OTP delivery via SMS works; users can verify and authenticate
- Magic links valid for 24 hours; cannot be reused
- Refresh tokens extend session; old tokens revoke cleanly
- Logout clears Redis session; subsequent requests 401
- Organization switch updates current tenant context
- Auth middleware blocks unauthenticated requests (401)

---

### Epic 2: Tenancy & Plans (Weeks 3–4)

**Summary**
Implement multi-tenancy infrastructure: tenant creation, custom branding, subdomain routing, subscription plans, feature toggles, and data retention policies. This enables the SaaS business model.

**Dependencies**

- Epic 0 (database, shared kernel)
- Epic 1 (user accounts, organization concept)

**Modules Involved**

- `Muntada.Tenancy/` — Core tenancy logic
- `Muntada.Core/` — Tenant, Plan aggregates
- Frontend: `components/Tenancy/`, tenant switcher
- Database: Tenants, Plans, Features, BrandingConfigs tables

**Key Deliverables**

1. Tenant creation (admin API endpoint)
2. Subdomain provisioning and routing (e.g., acme.muntada.com → tenant resolver middleware)
3. Custom branding: logo, colors, name per tenant
4. Subscription plans (Starter, Professional, Enterprise)
5. Feature toggles per plan (e.g., concurrent rooms, storage limits, API access)
6. Data retention policies (recordings, chat, audit logs TTL)
7. Tenant billing status tracking (active, suspended, cancelled)
8. Tenant isolation middleware (row-level security patterns)
9. Admin UI for tenant creation and management
10. Feature flag evaluation service (runtime feature checking)

**Week Target**
Week 3–4

**Parallelism Notes**

- Subdomain routing and branding can be done in parallel
- Plans and feature toggles independent tracks
- Data retention policies can be deferred to Epic 10 if needed
- Frontend tenant switcher can be refined in Epic 11

**Success Metrics**

- New tenant created with unique subdomain; accessible immediately
- Branding (logo, colors) applied consistently across all pages
- Feature toggles correctly restrict operations per plan
- Tenant data isolated (queries only return tenant's own data)
- Subscription status blocks/allows operations correctly
- Data retention policies execute on schedule

---

### Epic 3: Rooms & Scheduling (Weeks 4–5)

**Summary**
Implement room lifecycle management: creation, templates, scheduling with recurrence, one-off occurrences, invites, membership management, and state machine transitions (draft → scheduled → in-progress → finished).

**Dependencies**

- Epic 0 (database, shared kernel)
- Epic 1 (users, authentication)
- Epic 2 (tenants, plans)

**Modules Involved**

- `Muntada.Rooms/` — Core room logic
- `Muntada.Core/` — Room, Occurrence, Invite aggregates
- Frontend: `components/Rooms/`, calendar, room creation wizard
- Database: Rooms, Occurrences, Invites, RoomMembers, RoomTemplates tables

**Key Deliverables**

1. Room creation (one-off or template-based)
2. Room templates with common settings (e.g., "Company All-Hands")
3. Recurring room series (daily, weekly, monthly patterns)
4. Occurrence generation (weekly cron task for recurring series)
5. Room state machine: Draft → Scheduled → InProgress → Finished → Archived
6. Room membership: owner, moderators, speakers, observers
7. Invite generation (email invites to join room)
8. RSVP tracking and attendee list
9. Calendar UI showing scheduled rooms and occurrences
10. Room settings: description, duration, password protection
11. Conflict detection (overlapping rooms for same participant)
12. Integration with Epic 1 (only authenticated users can join private rooms)

**Week Target**
Week 4–5

**Parallelism Notes**

- Room CRUD and templates can be developed independently
- Recurrence logic and occurrence generation separate tracks
- Frontend calendar view can be implemented in parallel
- Invite system can follow core room implementation

**Success Metrics**

- Room created in draft state; can edit settings until scheduled
- Recurring room with weekly pattern generates occurrences for 12 weeks
- Email invites sent; tracking RSVP status
- Room state transitions enforced (e.g., can't start in Draft)
- Membership roles correctly control room permissions
- Calendar displays all upcoming occurrences for user

---

### Epic 4: Realtime Orchestration (Weeks 5–6)

**Summary**
Integrate LiveKit OSS for WebRTC signaling and audio streaming. Implement token issuance, webhook handling, room state caching, SignalR for client synchronization, and active participant tracking.

**Dependencies**

- Epic 0 (foundation, infrastructure)
- Epic 1 (authentication)
- Epic 2 (tenancy, feature limits)
- Epic 3 (rooms, room state)

**Modules Involved**

- `Muntada.Realtime/` — LiveKit integration, token issuance, webhooks
- `Muntada.Recording/` — Participant tracking (preview for Epic 7)
- Frontend: `components/Realtime/`, audio controls, participant list, volume meter
- Dependencies: LiveKit SDK, SignalR client
- Database: RoomSessions, ParticipantStates tables

**Key Deliverables**

1. LiveKit room provisioning on-demand (create room in LiveKit when Muntada room starts)
2. JWT token issuance for LiveKit access (scoped to room, user identity)
3. LiveKit webhook receivers (participant_joined, participant_left, recording_started, etc.)
4. Room session tracking (who joined, when, connection quality metrics)
5. Participant state cache (Redis): identity, role, joined_at, is_speaking
6. SignalR hub for pushing participant updates to all clients in room
7. Active participant counter (for concurrent room limiting in Epic 9)
8. Connection quality monitoring (publish network stats)
9. Graceful disconnect handling (auto-cleanup stale sessions)
10. Frontend: Real-time participant list, own audio/video controls, presence indicator
11. Audio codec selection (Opus preferred for GCC compliance)

**Week Target**
Week 5–6

**Parallelism Notes**

- LiveKit token issuance and webhook handling independent
- Frontend audio controls can develop in parallel with backend
- SignalR integration can follow core token logic
- Recording state tracking (participant_left detection) feeds into Epic 7

**Success Metrics**

- User joins room; LiveKit token issued, connects to WebRTC session
- Participant list updates in real-time across all clients (SignalR)
- Webhooks processed: participant_joined increments counter, participant_left decrements
- Connection stats published; Grafana dashboard shows concurrent participants
- Disconnect cleans up session state within 5 seconds
- Audio quality meters visible in UI

---

### Epic 5: Moderation (Weeks 6–7)

**Summary**
Implement speaker management: raise-hand queue, moderator grant/deny, speaker roster states, moderator handover, participant lock/remove actions. This enforces moderated discussion patterns.

**Dependencies**

- Epic 3 (room membership, roles)
- Epic 4 (realtime orchestration, participant state)

**Modules Involved**

- `Muntada.Moderation/` — Core moderation logic
- `Muntada.Core/` — RaiseHand, SpeakerQueue, RosterState aggregates
- Frontend: `components/Moderation/`, raise-hand button, queue UI, moderator panel
- Database: RaiseHands, SpeakerQueues, RosterStates tables

**Key Deliverables**

1. Raise-hand mechanism (observer/speaker can raise hand)
2. Speaker queue (FIFO; moderator sees queue and grants/denies)
3. Speaker state transitions: Listener → Speaker → Listener
4. Moderator actions: grant, deny, remove from room, mute (LiveKit integration)
5. Moderator handover (pass control to another speaker)
6. Lock room (prevent new participants from joining)
7. Unlock room
8. Roster view (who's speaking, who's listening, queue count)
9. Real-time queue updates via SignalR
10. Notifications when hand is granted/denied
11. Speaker timeout (auto-demote if inactive for X minutes)
12. Moderation log (audit trail of actions)

**Week Target**
Week 6–7

**Parallelism Notes**

- Queue logic and moderator actions independent tracks
- Frontend raise-hand UI and moderator panel can develop in parallel
- Speaker timeout can be deferred to week 7 if needed
- Moderation log can be integrated with Epic 10

**Success Metrics**

- Participant raises hand; appears in moderator queue
- Moderator grants; participant becomes speaker (audio unmuted in LiveKit)
- Queue updates in real-time across all clients
- Remove participant immediately disconnects from room
- Moderator can hand over control; new moderator inherits permissions
- Lock/unlock enforced (new joins rejected when locked)
- Timeout demotes inactive speakers automatically

---

### Epic 6: Chat & Messaging (Weeks 7–8)

**Summary**
Implement real-time room chat and direct messaging between participants. Includes message persistence, delivery status tracking, unread indicators, and read receipts.

**Dependencies**

- Epic 1 (authentication, users)
- Epic 4 (realtime orchestration, SignalR)

**Modules Involved**

- `Muntada.Chat/` — Core chat logic
- `Muntada.Core/` — Message, DirectMessage aggregates
- Frontend: `components/Chat/`, message input, message list, unread badges
- Database: RoomMessages, DirectMessages, ReadReceipts tables

**Key Deliverables**

1. Room-wide messages (all participants see)
2. Moderator-only DM (observer can DM moderator privately)
3. Message persistence (stored for audit, retention per tenant policy)
4. Message delivery status: pending, delivered, failed
5. Read receipts (track who read each message)
6. Unread message count per room/DM
7. Message search within room
8. Message editing (update timestamp, history kept)
9. Message deletion (soft delete with audit trail)
10. Typing indicators (who's typing now)
11. Rich text support (mentions, links, basic formatting)
12. Markdown rendering
13. Frontend: Scrollable message list, input box, typing indicator

**Week Target**
Week 7–8

**Parallelism Notes**

- Room chat and DM can be developed in parallel
- Message persistence independent from UI
- Typing indicators and read receipts can follow core messaging
- Search can be deferred to Phase 2 if needed

**Success Metrics**

- Message sent; immediately visible to all room participants
- DM sent; only moderator and sender see
- Message marked delivered within 100ms
- Read receipts show who's read the message
- Unread count decrements when user opens chat
- Typing indicator appears for 2+ seconds of inactivity
- Message deleted removed from view; audit log retained

---

### Epic 7: Recording & Consent (Weeks 8–9)

**Summary**
Implement local and server-side recording with manifest generation, consent tracking, download rules, and consent announcements. Ensures PDPL-compliant recording practices.

**Dependencies**

- Epic 3 (room lifecycle)
- Epic 4 (realtime orchestration, participant tracking)
- Epic 5 (moderation, speaker identification)

**Modules Involved**

- `Muntada.Recording/` — Core recording logic, manifest generation
- `Muntada.Storage/` — Preview for Epic 8
- Frontend: `components/Recording/`, recording indicator, consent popup, download UI
- Database: Recordings, RecordingParticipants, ConsentEvents, RecordingManifests tables

**Key Deliverables**

1. Server-side recording request (LiveKit webhook integration)
2. Local recording (browser-based, participant downloads own audio)
3. Recording participant manifest (who spoke, when, duration, audio track)
4. Consent tracking: opt-in consent events (participant grants permission to record)
5. Consent announcement (room announcement: "This call is being recorded")
6. Recording indicator (visible to all: "Recording in progress")
7. Download rules:
   - Speaker can always download own audio track
   - Moderator can download full recording if all speakers consented
   - Tenant admin can audit recordings via dashboard
8. Recording expiry (soft delete after retention period; audit log kept)
9. Recording metadata: room, participants, start/end time, duration, codec
10. Integration with file storage (Epic 8) for manifest files
11. Frontend: Consent popup at room start, recording indicator, download button

**Week Target**
Week 8–9

**Parallelism Notes**

- Server recording and local recording logic independent
- Consent tracking separate from manifest generation
- Frontend consent UI can develop in parallel with backend

**Success Metrics**

- Room starts; "Recording" indicator visible to all
- Consent announcement plays at start
- Participants see consent popup; can download own audio track
- Manifest generated with participant participation data
- Moderator can download full recording only if all speakers consented
- Recording deleted after retention period; audit log preserved

---

### Epic 8: Files & Artifacts (Weeks 9–10)

**Summary**
Implement file upload infrastructure: storage in MinIO, malware scanning workflow, targeted delivery (participant- or role-based), expiry management, and download tracking.

**Dependencies**

- Epic 0 (S3-compatible storage, infrastructure)
- Epic 2 (tenancy, storage limits per plan)
- Epic 7 (recording manifests, artifacts)

**Modules Involved**

- `Muntada.Storage/` — Core storage logic, upload orchestration, malware scanning
- MinIO (object storage backend)
- Frontend: `components/Storage/`, file upload UI, progress bar, download list
- Database: Files, FileAccess, MalwareScanResults tables

**Key Deliverables**

1. File upload endpoint (multipart/form-data)
2. File type validation (whitelist: PDF, DOCX, images, audio, video)
3. File size validation (per tenant plan limits)
4. Virus/malware scanning (ClamAV integration or third-party API)
5. Quarantine infected files (admin can review and delete)
6. Storage in MinIO with tenant-based path isolation
7. File metadata: name, size, type, upload_at, uploaded_by, scan_status
8. Access control: upload-by role, download-by role or explicit list
9. Expiry management (delete after retention period)
10. Download tracking (audit log: who downloaded, when)
11. Direct download links (signed, time-limited)
12. Thumbnail generation for images
13. Frontend: Drag-and-drop upload, progress bar, file list, download button

**Week Target**
Week 9–10

**Parallelism Notes**

- Upload validation and malware scanning can run in parallel
- Storage backend independent from access control logic
- Frontend upload UI and download tracking separate tracks

**Success Metrics**

- File uploaded; scanned within 30 seconds
- Clean files immediately available for download
- Infected files quarantined; not accessible to users
- Download link valid for 1 hour; cannot be reused
- Expired files deleted automatically; audit log retained
- Storage consumption tracked against tenant plan limits

---

### Epic 9: Billing & Metering (Weeks 10–11)

**Summary**
Implement subscription management, metering (concurrent rooms, storage, OTP usage), HyperPay/Moyasar payment integration, and billing state tracking.

**Dependencies**

- Epic 0 (database, foundation)
- Epic 2 (tenants, plans)
- Epic 4 (realtime orchestration, participant counting)
- Epic 8 (storage tracking)

**Modules Involved**

- `Muntada.Billing/` — Core billing logic, metering, payment integration
- HyperPay/Moyasar APIs
- Frontend: `components/Billing/`, billing dashboard, payment form
- Database: Subscriptions, MeterReadings, Invoices, Payments tables

**Key Deliverables**

1. Subscription creation (user selects plan, becomes subscriber)
2. Subscription state: active, suspended, cancelled, expired
3. Auto-renewal (monthly or annual, 7-day reminder before renewal)
4. Concurrent room counter (meter: max concurrent rooms per plan)
5. Storage consumption meter (track uploads + recordings)
6. OTP usage meter (track SMS sends per month)
7. Overage charges (if metering exceeds plan limit)
8. HyperPay payment integration (charge card on subscription)
9. Moyasar payment fallback
10. Invoice generation (monthly/annual, line items for base + overage)
11. Dunning workflow (retry failed payments, notify user)
12. Billing status indicator (active, past_due, suspended)
13. Payment method management (add, update, delete card)
14. Tax calculation (VAT per GCC region)
15. Admin billing dashboard (revenue, churn, MRR)
16. Billing email notifications (invoice, payment confirmation, suspension warning)

**Week Target**
Week 10–11

**Parallelism Notes**

- Meter implementation (rooms, storage, OTP) can run in parallel
- Payment gateway integration and subscription logic separate tracks
- Admin billing dashboard independent from user-facing billing UI
- Tax and invoice generation can follow payment integration

**Success Metrics**

- User subscribes to plan; payment processed successfully
- Concurrent room limit enforced (reject room start if at limit)
- Overage charges calculated and added to next invoice
- Auto-renewal succeeds; user receives invoice
- Failed payment triggers retry within 5 days
- Billing dashboard shows real-time MRR, churn, subscription status

---

### Epic 10: Reporting & Audit (Weeks 11)

**Summary**
Implement operational dashboards for tenants (analytics, usage), admin dashboards (system health, revenue), and audit export (compliance, forensics).

**Dependencies**

- Epic 0 (observability stack, logging)
- Epic 2 (tenants, feature limits)
- Epic 3 (rooms, occurrences)
- Epic 4 (realtime, participant data)
- Epic 9 (billing, meters)

**Modules Involved**

- `Muntada.Reporting/` — Analytics, audit, dashboard logic
- Prometheus, Grafana, ELK
- Frontend: `components/Reporting/`, dashboard, chart components
- Database: AuditLogs, AnalyticsSnapshots tables

**Key Deliverables**

1. Operational dashboards:
   - System health: uptime, error rates, latency percentiles
   - Concurrent participants, active rooms, peak hours
   - Feature usage: recordings, chats, file uploads
2. Tenant analytics dashboard:
   - Room utilization (attendance, duration)
   - Participant engagement (speak time, raise hands)
   - Chat activity (message count, unique users)
   - Recording completion rate
3. Billing analytics:
   - MRR, churn rate, subscription distribution
   - Overage revenue
   - Payment success rate
4. Audit log export:
   - All user actions, data changes, access events
   - Filterby date range, user, action type, resource
   - Export format: CSV, JSON
5. Compliance report:
   - Data retention compliance (check old records cleaned up)
   - User consent tracking
   - Payment audit trail
6. Grafana dashboards (auto-provisioned):
   - System metrics (CPU, memory, disk, network)
   - Database performance (queries, connections)
   - LiveKit metrics (room count, bitrate, packet loss)
7. Log aggregation (ELK stack):
   - Structured logs from all services
   - Searchable audit events
   - Error tracking and alerting

**Week Target**
Week 11

**Parallelism Notes**

- Operational and tenant dashboards can develop independently
- Grafana setup independent from custom dashboard implementation
- Audit log export can follow core dashboard work
- Compliance reports can be deferred if needed

**Success Metrics**

- Dashboard accessible to authenticated tenant admins
- Real-time metrics (< 5 min latency)
- Audit log export includes all user actions for past 12 months
- Grafana dashboards monitor system health automatically
- Compliance report confirms data retention policies enforced

---

### Epic 11: Admin & Support (Weeks 11–12)

**Summary**
Implement tenant management UI, live intervention tools (impersonation, participant removal), emergency controls (token revoke, room closure), and support workflows.

**Dependencies**

- Epic 1 (authentication, sessions)
- Epic 2 (tenants)
- Epic 3 (rooms)
- Epic 4 (realtime)
- Epic 5 (moderation)

**Modules Involved**

- `Muntada.Admin/` — Core admin logic, interventions
- Frontend: `components/Admin/`, admin panel, tenant mgmt
- Database: AdminActions, InterventionLog tables

**Key Deliverables**

1. Tenant management:
   - View all tenants, edit branding, manage plans
   - Suspend/unsuspend tenant (blocks all access)
   - Delete tenant (soft delete, audit trail)
2. User impersonation (admin assumes user identity, can join rooms)
3. Live room intervention:
   - View active rooms in real-time
   - Force remove participant from room
   - Mute/unmute participant
   - End room (force all participants disconnect)
4. Emergency token revocation:
   - Revoke all JWT tokens for a user
   - Revoke all tokens for a tenant
   - Immediate logout across all devices
5. Session management:
   - View active sessions per user
   - Force logout specific session
6. Support tools:
   - Search user by email, phone, ID
   - View user activity history
   - Access user's rooms, files, chats (audit only)
7. System controls:
   - Maintenance mode (graceful shutdown, redirect users)
   - Rate limit overrides (for testing)
   - Feature toggle management (enable/disable per tenant)
8. Admin audit log (all admin actions logged and searchable)

**Week Target**
Week 11–12

**Parallelism Notes**

- Tenant management UI and user impersonation independent
- Emergency token revocation and session management separate tracks
- Support tools can follow core admin dashboard

**Success Metrics**

- Admin suspends tenant; all users immediately logged out
- Impersonation works; admin can join user's rooms
- Emergency token revoke clears all sessions within 1 second
- Live room intervention removes participant immediately
- Admin audit log captures all actions with timestamp and reason

---

### Epic 12: Public API & Webhooks (Weeks 12)

**Summary**
Expose REST API for external integrations with PAT (Personal Access Token) authentication, request quotas, and signed webhooks for real-time event notifications.

**Dependencies**

- All previous epics (API must expose core functionality)

**Modules Involved**

- `Muntada.Api/` — API controller, PAT auth, webhook signing
- Frontend: `components/Api/`, token management UI
- Database: PersonalAccessTokens, WebhookSubscriptions, WebhookEvents tables

**Key Deliverables**

1. RESTful API endpoints:
   - Rooms: CRUD operations, list occurrences, manage invites
   - Participants: Join room, leave, get roster
   - Recordings: List, download, delete
   - Files: Upload, download, list, delete
   - Users: Profile, update settings
   - Billing: View subscription, usage meters, invoices
2. PAT authentication:
   - Token generation (long-lived, per-user)
   - Token revocation
   - Token scope limiting (e.g., read-only, specific tenant)
   - Rate limiting per token (quotas)
3. Request quotas:
   - 1000 requests/hour per token
   - Burst limit: 100 requests/minute
   - Graceful rejection with Retry-After header
4. Webhook subscriptions:
   - Subscribe to events: room.created, participant.joined, recording.finished, etc.
   - Webhook URL validation
   - Retry logic (exponential backoff, 5 retries)
5. Signed webhooks:
   - HMAC-SHA256 signature in X-Signature header
   - Tenant-specific webhook secret
   - Webhook payload: event type, timestamp, data
6. Webhook management UI:
   - Subscribe/unsubscribe to event types
   - View webhook delivery history
   - Manual retry of failed deliveries
   - Test webhook delivery
7. API documentation (OpenAPI/Swagger):
   - Auto-generated from code
   - Interactive Swagger UI
   - Code examples (cURL, Python, Node.js)
8. API versioning (v1, v2 planned)
9. CORS support for browser-based clients

**Week Target**
Week 12

**Parallelism Notes**

- API endpoint development can parallelize across different resources
- PAT authentication independent from webhook implementation
- API documentation generation can follow endpoint implementation

**Success Metrics**

- External client can authenticate with PAT
- API calls respect rate limits; Retry-After header present
- Webhook subscription works; events delivered to URL within 10 seconds
- Webhook signature validates correctly
- API documentation complete and accurate
- 99.9% webhook delivery success (after retries)

---

## Dependency Graph

```
Week 1-2:  [Epic 0: Foundation]
              |
              v
Week 2-3:  [Epic 1: Identity] ──────┐
              |                       |
              v                       v
Week 3-4:  [Epic 2: Tenancy] ─────┐  (can start after Epic 0)
              |                    |
              v                    v
Week 4-5:  [Epic 3: Rooms] ───┐   |  (depends on Epics 0, 1, 2)
              |               |   |
              v               v   v
Week 5-6:  [Epic 4: Realtime] ─┐  (depends on Epics 0, 1, 2, 3)
              |                |
              v                v
Week 6-7:  [Epic 5: Moderation] (depends on Epics 3, 4)
              |
              v
Week 6-7:  [Epic 6: Chat] ──────────┐  (parallel with Moderation)
              |                      |
              v                      |
Week 8-9:  [Epic 7: Recording] ─┐   |  (depends on Epics 3, 4, 5)
              |                 |   |
              v                 v   v
Week 9-10: [Epic 8: Storage] ──┐  (depends on Epics 0, 2, 7)
              |                |
              v                |
Week 10-11:[Epic 9: Billing] ──┤   (depends on Epics 0, 2, 4, 8)
              |                |
              v                v
Week 11:   [Epic 10: Reporting] (depends on all previous)
              |
              v
Week 11-12:[Epic 11: Admin] ────────(depends on Epics 1, 2, 3, 4, 5)
              |
              v
Week 12:   [Epic 12: API] ──────────(depends on all previous)

Critical Path: Epic 0 → Epic 1 → Epic 2 → Epic 3 → Epic 4 → Epic 5 → Epic 7 → Epic 8 → Epic 9
Expected critical path duration: 11 weeks
Buffer: 1 week for testing, fixes, and contingencies
```

---

## Team Allocation Strategy

### Team Composition

- **Backend Lead (1 dev)** — ASP.NET Core architecture, modular design, data modeling
- **Frontend Lead (1 dev)** — React SPA, UI/UX, state management
- **Full-Stack (1–2 devs)** — Features spanning backend and frontend
- **AI-Assisted Development** — Code generation, test writing, documentation

### Weekly Allocation (4-dev team)

**Weeks 1–2 (Epic 0)**

- **Backend Lead:** Repository, CI/CD, K8s setup, database schema
- **Frontend Lead:** Vite setup, TypeScript config, UI component library
- **Full-Stack 1:** Docker Compose, observability stack (Prometheus, Grafana)
- **Full-Stack 2:** Shared kernel implementation, domain models
- **AI-Assisted:** Documentation, automated tests for infrastructure

**Weeks 2–3 (Epic 1)**

- **Backend Lead:** Email/password auth, JWT, refresh token flow
- **Frontend Lead:** Login/register pages, OTP UI, protected routes
- **Full-Stack 1:** Phone OTP backend, session management (Redis)
- **Full-Stack 2:** Magic link feature, password reset
- **AI-Assisted:** Auth integration tests, OpenAPI docs

**Weeks 3–4 (Epic 2)**

- **Backend Lead:** Tenant service, subdomain routing, plan management
- **Frontend Lead:** Tenant switcher, branding, admin dashboard scaffold
- **Full-Stack 1:** Feature toggles, billing status tracking
- **Full-Stack 2:** Data retention policies, tenant isolation queries
- **AI-Assisted:** Test generation, compliance documentation

**Weeks 4–5 (Epic 3)**

- **Backend Lead:** Room aggregates, state machine, template logic
- **Frontend Lead:** Room creation wizard, calendar view, UI polish
- **Full-Stack 1:** Recurring series, occurrence generation, invites
- **Full-Stack 2:** Membership management, conflict detection
- **AI-Assisted:** Calendar algorithm tests, invite email templates

**Weeks 5–6 (Epic 4)**

- **Backend Lead:** LiveKit integration, token issuance, webhooks
- **Frontend Lead:** Audio controls, participant list, UI responsiveness
- **Full-Stack 1:** SignalR implementation, presence tracking
- **Full-Stack 2:** Connection quality monitoring, metrics publishing
- **AI-Assisted:** WebRTC tests, codec validation, dashboard templates

**Weeks 6–7 (Epic 5)**

- **Backend Lead:** Raise-hand queue, speaker state machine
- **Frontend Lead:** Moderator panel, queue UI, real-time updates
- **Full-Stack 1:** Moderator actions (grant, deny, remove), handover
- **Full-Stack 2:** Lock/unlock room, speaker timeout, moderation log
- **AI-Assisted:** Moderation tests, audit trail validation

**Weeks 7–8 (Epic 6)**

- **Backend Lead:** Room chat persistence, delivery status
- **Frontend Lead:** Message UI, input box, typing indicators
- **Full-Stack 1:** Moderator DM, read receipts, message search
- **Full-Stack 2:** Message editing/deletion, audit trail
- **AI-Assisted:** Chat integration tests, markdown rendering

**Weeks 8–9 (Epic 7)**

- **Backend Lead:** Recording orchestration, manifest generation
- **Frontend Lead:** Consent UI, recording indicator, download UI
- **Full-Stack 1:** Participant tracking, consent events, local recording
- **Full-Stack 2:** Recording expiry, download rules, storage integration
- **AI-Assisted:** Recording validation tests, PDPL compliance checks

**Weeks 9–10 (Epic 8)**

- **Backend Lead:** MinIO integration, malware scanning workflow
- **Frontend Lead:** File upload UI, progress bar, file browser
- **Full-Stack 1:** Access control, expiry management
- **Full-Stack 2:** Download tracking, thumbnail generation, signed links
- **AI-Assisted:** Upload validation tests, security scanning validation

**Weeks 10–11 (Epic 9)**

- **Backend Lead:** Subscription management, metering logic
- **Frontend Lead:** Billing dashboard, payment form
- **Full-Stack 1:** HyperPay integration, invoice generation
- **Full-Stack 2:** Dunning workflow, tax calculation, email notifications
- **AI-Assisted:** Billing calculation tests, payment reconciliation

**Week 11 (Epic 10)**

- **Backend Lead:** Analytics aggregation, audit export
- **Frontend Lead:** Dashboard design, charting library integration
- **Full-Stack 1:** Grafana provisioning, compliance reports
- **Full-Stack 2:** ELK log aggregation, searching
- **AI-Assisted:** Dashboard tests, alert rule templates

**Weeks 11–12 (Epic 11)**

- **Backend Lead:** Admin interventions, emergency controls
- **Frontend Lead:** Admin panel UI, user search, live room view
- **Full-Stack 1:** Impersonation, token revocation
- **Full-Stack 2:** Support tools, audit logging
- **AI-Assisted:** Admin tests, runbook documentation

**Week 12 (Epic 12)**

- **Backend Lead:** API design (OpenAPI), PAT auth, quotas
- **Frontend Lead:** Token management UI, API docs rendering
- **Full-Stack 1:** Webhook subscriptions, signed webhooks
- **Full-Stack 2:** API client SDK generation, versioning
- **AI-Assisted:** API test generation, integration test harness

### AI-Assisted Development Tasks

1. **Code Generation**
   - CRUD endpoints (controller boilerplate)
   - Repository/query patterns
   - React component scaffolding
   - Test stubs (unit, integration)

2. **Testing**
   - Unit test generation (90%+ coverage targets)
   - Integration test suite (happy path + edge cases)
   - Performance test baselines
   - Security test validation

3. **Documentation**
   - API documentation (OpenAPI generation)
   - Architecture decision records (ADRs)
   - Runbooks (deployment, incident response)
   - User guides and FAQ

4. **Code Review & QA**
   - Static analysis (linting, SAST)
   - Code coverage analysis
   - Performance profiling
   - Security audit scripts

---

## Critical Path Analysis

### Timeline Summary

| Phase | Duration | Epics | Notes |
|-------|----------|-------|-------|
| Foundation | Week 1–2 | Epic 0 | Unblocks all downstream work |
| Auth & Tenancy | Week 2–4 | Epic 1–2 | Sequential, minimal parallelism |
| Core Features | Week 4–7 | Epic 3–6 | High parallelism (rooms, realtime, moderation, chat) |
| Recording & Files | Week 8–10 | Epic 7–9 | Sequential (depends on realtime) |
| Admin & APIs | Week 11–12 | Epic 10–12 | Comprehensive, testing-focused |

### Critical Path

The longest dependency chain determines project completion:

```
Epic 0 (2 wks) → Epic 1 (1 wk) → Epic 2 (1 wk) → Epic 3 (1 wk) →
Epic 4 (1 wk) → Epic 5 (1 wk) → Epic 7 (1 wk) → Epic 8 (1 wk) →
Epic 9 (1 wk) → Epic 10 (0.5 wks) → Epic 12 (1 wk)

Total: 11.5 weeks
```

### Parallelism Opportunities

To maximize the 3-month timeline:

- **Weeks 2–3:** Epic 1 (auth) and Epic 2 (tenancy) can start together after Epic 0
- **Weeks 4–7:** Epic 3 (rooms), Epic 5 (moderation), Epic 6 (chat) can develop in parallel
  - Teams: Backend → Rooms, Frontend → Chat/Moderation UI
- **Weeks 8–10:** Epic 7 (recording) and Epic 8 (storage) sequential but early-start Epic 8 features (upload validation) can precede recording
- **Weeks 10–12:** Epic 9 (billing), Epic 11 (admin), Epic 12 (API) independent and can parallelize

### Buffers

- **Week 12 (1 week):** Testing, bug fixes, documentation polish
- **Integration testing:** Weeks 11–12 (cross-epic scenarios)
- **Performance/load testing:** Week 11
- **Security review:** Weeks 1–12 (continuous)

---

## Risk Assessment

### High-Risk Items

#### 1. LiveKit Integration Complexity
- **Risk:** WebRTC signaling, participant state synchronization, webhook reliability
- **Impact:** CRITICAL (blocks all realtime features)
- **Mitigation:**
  - Prototype LiveKit integration in Week 3 (spike task)
  - Dedicated developer for Epic 4
  - Extensive integration testing (Week 5–6)
  - Fallback: Use LiveKit Cloud if OSS deployment unstable

#### 2. 3-Month Aggressive Timeline
- **Risk:** Insufficient time for testing, QA, edge cases
- **Impact:** CRITICAL (quality may suffer)
- **Mitigation:**
  - AI-assisted test generation from day 1
  - Automated regression test suite
  - Priority-based feature list (drop Phase 2 features if needed)
  - Continuous integration (fail fast)

#### 3. Multi-Tenancy Isolation
- **Risk:** Data leakage between tenants, query performance under load
- **Impact:** HIGH (security, performance)
- **Mitigation:**
  - Row-level security patterns (RLS) in SQL Server from Epic 2
  - Query profiling and indexing (Week 4–5)
  - Load testing with realistic data volume (Week 10–11)
  - Code review focus on isolation queries

#### 4. Self-Hosted Infrastructure
- **Risk:** Kubernetes management, database backups, disaster recovery
- **Impact:** HIGH (operations, availability)
- **Mitigation:**
  - Infrastructure as Code (Kustomize/Helm) from Epic 0
  - Automated backups and restore testing (Week 2, 12)
  - Runbooks for common failures (disaster recovery, scaling)
  - DevOps-focused sprints (Weeks 1–2, 11–12)

#### 5. Payment Integration Complexity
- **Risk:** HyperPay/Moyasar API reliability, compliance (PCI, tax)
- **Impact:** HIGH (revenue, compliance)
- **Mitigation:**
  - Early prototyping with payment providers (Week 9)
  - Sandbox environment testing (Week 9–10)
  - Compliance review with legal team (Week 10)
  - Fallback payment method (second provider)

#### 6. GCC Compliance (Saudi PDPL)
- **Risk:** Data residency, retention policies, consent tracking not compliant
- **Impact:** HIGH (legal, regulatory)
- **Mitigation:**
  - Legal review of architecture (Week 1)
  - Compliance checklist per epic (Weeks 2–12)
  - Data retention automation (Epic 2, 7, 8)
  - Audit log and consent tracking (Epics 5, 7, 10)

### Medium-Risk Items

#### 7. Frontend Performance
- **Risk:** Large React SPA, real-time updates via SignalR, inefficient state management
- **Impact:** MEDIUM (user experience)
- **Mitigation:**
  - Code-splitting and lazy loading (Week 3–4)
  - Performance profiling (Week 8–9)
  - Zustand/Redux state management patterns
  - React DevTools monitoring

#### 8. Database Performance
- **Risk:** Query N+1, missing indexes, unoptimized joins
- **Impact:** MEDIUM (latency, scalability)
- **Mitigation:**
  - Database profiling sprints (Weeks 4, 9)
  - Query monitoring (Prometheus, Grafana from Epic 0)
  - Index strategy for high-traffic tables (Rooms, Messages, Recordings)

#### 9. Team Onboarding & Ramp-Up
- **Risk:** New developers slow down progress; unclear architecture
- **Impact:** MEDIUM (velocity)
- **Mitigation:**
  - Comprehensive onboarding docs (Week 1)
  - Architecture decision records (ADRs) updated weekly
  - Pair programming for high-risk components
  - Code review standards enforced

#### 10. Third-Party Dependencies
- **Risk:** Breaking changes in LiveKit, ASP.NET Core, React
- **Impact:** MEDIUM (stability, maintenance)
- **Mitigation:**
  - Pin major versions at start (Week 1)
  - Automated dependency scanning (GitHub Dependabot)
  - Test against upcoming releases (Week 5, 9)

### Low-Risk Items

- Email delivery (OTP, invites): Use managed service (SendGrid, AWS SES)
- DNS/subdomain routing: Simple reverse proxy (Nginx)
- Marketing landing page: Static HTML (Phase 2 feature, not on critical path)

---

## Success Criteria

### Functional Requirements Met

- [ ] All 13 epics delivered (Weeks 1–12)
- [ ] Multi-tenancy working: 3+ concurrent tenants, data isolated
- [ ] Live rooms: Join, leave, real-time participant list, audio quality acceptable (>= 96 kbps)
- [ ] Moderation: Raise hand, grant/deny, speaker queue functional
- [ ] Recording: Server-side and local recording, consent tracking, downloads working
- [ ] Billing: Subscriptions created, payments processed, metering enforced
- [ ] API: 30+ endpoints, PAT auth, quota limiting, webhooks signed
- [ ] Admin: Tenant management, impersonation, emergency controls
- [ ] Audit: All user actions logged, exportable, PDPL-compliant retention

### Quality Requirements Met

- [ ] **Test Coverage:** >= 80% code coverage (unit + integration)
- [ ] **Performance:** P95 latency < 500ms for API endpoints, < 2s for page load
- [ ] **Availability:** 99.5% uptime (no single-point failures)
- [ ] **Security:** SAST/DAST scans clean, no high/critical vulns, PCI compliance
- [ ] **Documentation:** API docs (OpenAPI), architecture docs, runbooks, user guide

### Team & Process

- [ ] **Deployment:** Automated CI/CD pipeline, 1-click staging/prod release
- [ ] **Monitoring:** Grafana dashboards show system health, alerts configured
- [ ] **Incident Response:** Runbooks exist for top 5 failure scenarios
- [ ] **Code Quality:** Static analysis (linting, SAST) enforced in CI

### Post-Launch Readiness

- [ ] **Backup/Restore:** Tested, RPO < 1 hour, RTO < 4 hours
- [ ] **Scaling:** Load test confirms 100+ concurrent users, 50 rooms
- [ ] **Support:** Documentation, FAQ, support ticket system ready
- [ ] **Compliance:** Legal audit complete, PDPL compliance certified

---

## Appendix: Weekly Milestones

### Week 1
- Repo initialized, CI/CD green
- Docker Compose running locally (all services)
- Kubernetes cluster provisioned
- Shared kernel implemented

**Deliverable:** Developers can clone and run full stack locally

### Week 2
- Email/password auth complete
- JWT and refresh token working
- Database schema (users, credentials) finalized
- Observability stack (Prometheus, Grafana) operational

**Deliverable:** Login flow working end-to-end

### Week 3
- Phone OTP registration/login complete
- Tenant creation API working
- Tenancy middleware enforcing isolation
- Magic link feature (optional, deferred if needed)

**Deliverable:** Multi-tenant isolation functional, OTP delivery confirmed

### Week 4
- Room CRUD and templates complete
- Recurring series logic working
- Invite system sending emails
- Calendar UI showing rooms

**Deliverable:** Users can create and schedule rooms

### Week 5
- LiveKit integration (token issuance, webhooks) complete
- SignalR fan-out working
- Participant list real-time
- Connection quality metrics visible

**Deliverable:** Users can join live rooms, hear audio

### Week 6
- Raise-hand queue functional
- Moderator grant/deny working
- Speaker roster state transitions correct
- Moderator handover implemented

**Deliverable:** Moderated speaker management working

### Week 7
- Room chat persistent and real-time
- Moderator DM working
- Read receipts tracking
- Typing indicators visible

**Deliverable:** Chat feature fully functional

### Week 8
- Server-side recording orchestration working
- Participant manifest generation
- Consent tracking and announcements
- Local recording frontend UI

**Deliverable:** Recording with consent working

### Week 9
- File upload and malware scanning working
- Storage limits enforced
- Targeted delivery and access control
- File expiry deletion

**Deliverable:** File artifacts managed securely

### Week 10
- Subscription management working
- HyperPay integration complete
- Metering enforced (concurrent rooms, storage, OTP)
- Invoices and billing dashboard

**Deliverable:** Payment processing functional

### Week 11
- Operational dashboards displaying metrics
- Tenant analytics dashboard
- Audit export working
- Admin dashboard and interventions

**Deliverable:** Visibility and admin control

### Week 12
- REST API endpoints complete
- PAT authentication and quotas working
- Webhook subscriptions and delivery
- API documentation (OpenAPI, Swagger)

**Deliverable:** External integrations possible

---

## Sign-Off

| Role | Name | Date | Signature |
|------|------|------|-----------|
| Project Lead | TBD | 2026-04-03 | ________ |
| Tech Lead (Backend) | TBD | 2026-04-03 | ________ |
| Tech Lead (Frontend) | TBD | 2026-04-03 | ________ |
| DevOps Lead | TBD | 2026-04-03 | ________ |

---

**Document Version:** 1.0
**Last Updated:** 2026-04-03
**Next Review:** 2026-04-17 (after Epic 0 completion)
