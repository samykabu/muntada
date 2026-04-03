# Data Model: Foundation & Infrastructure

**Feature**: 000-foundation
**Date**: 2026-04-03

---

## SharedKernel Domain Entities

### Entity\<TId\>

Base class for all domain entities.

| Field | Type | Description |
|-------|------|-------------|
| Id | TId | Primary identifier (generic type) |

**Behavior**:
- Equality based on Id (not reference equality)
- Override `Equals()`, `GetHashCode()`, `==`, `!=`

---

### AggregateRoot\<TId\> : Entity\<TId\>

Base class for aggregate roots with domain event tracking and optimistic concurrency.

| Field | Type | Description |
|-------|------|-------------|
| Id | TId | Primary identifier (inherited) |
| Version | int | Optimistic concurrency token, auto-incremented on save |
| CreatedAt | DateTimeOffset | UTC timestamp of creation |
| UpdatedAt | DateTimeOffset | UTC timestamp of last modification |
| _domainEvents | List\<IDomainEvent\> | Internal collection of pending domain events |

**Behavior**:
- `AddDomainEvent(IDomainEvent @event)` — Adds event to pending collection
- `ClearDomainEvents()` — Clears pending events (called after publish)
- `DomainEvents` — Read-only view of pending events
- Version increments on every successful persist

---

### AuditedEntity\<TId\> : AggregateRoot\<TId\>

Extends AggregateRoot with audit tracking fields.

| Field | Type | Description |
|-------|------|-------------|
| CreatedBy | string | User ID of creator (opaque ID) |
| UpdatedBy | string? | User ID of last modifier |
| IsDeleted | bool | Soft delete flag (default false) |
| DeletedAt | DateTimeOffset? | UTC timestamp of soft delete |
| DeletedBy | string? | User ID who deleted |

---

### ValueObject

Abstract base class for value objects (immutable, equality by value).

**Behavior**:
- `GetEqualityComponents()` — Returns fields used for equality
- Override `Equals()`, `GetHashCode()` based on components
- Immutable by convention (all properties `init` or constructor-only)

---

### OpaqueId : ValueObject

Strongly-typed opaque identifier.

| Field | Type | Description |
|-------|------|-------------|
| Value | string | Full opaque ID string (e.g., `usr_a7k2jZ9xQpR4b1m`) |
| Prefix | string | Type prefix (e.g., `usr`, `org`, `room`) |
| EncodedPart | string | The encoded portion after the underscore |

**Behavior**:
- `OpaqueIdGenerator.Generate(string prefix)` → `OpaqueId`
- `OpaqueId.TryParse(string input, out OpaqueId result)` → bool
- `ToString()` → `"{Prefix}_{EncodedPart}"`
- Validation: prefix must be 2-8 lowercase alpha characters

---

### IDomainEvent

Marker interface for domain events.

| Field | Type | Description |
|-------|------|-------------|
| EventId | Guid | Unique event identifier |
| OccurredAt | DateTimeOffset | UTC timestamp when event occurred |

---

### IntegrationEvent : IDomainEvent

Base class for cross-module integration events published to RabbitMQ.

| Field | Type | Description |
|-------|------|-------------|
| EventId | Guid | Unique event identifier (inherited) |
| OccurredAt | DateTimeOffset | UTC timestamp (inherited) |
| AggregateId | string | Opaque ID of the source aggregate |
| AggregateType | string | Type name of the source aggregate |
| Version | int | Event schema version for backward compatibility |

---

### AuditLog

Stores change tracking for audited entities.

| Field | Type | Description |
|-------|------|-------------|
| Id | Guid | Primary key |
| EntityType | string | Full type name of the audited entity |
| EntityId | string | Opaque ID of the audited entity |
| Action | AuditAction | Enum: Created, Updated, Deleted |
| ChangedBy | string | User ID who made the change |
| ChangedAt | DateTimeOffset | UTC timestamp of the change |
| Changes | string | JSON serialized before/after values |
| CorrelationId | string? | Request correlation ID for tracing |

**AuditAction Enum**: `Created = 0`, `Updated = 1`, `Deleted = 2`

---

## Domain Exception Hierarchy

```
Exception
└── DomainException (abstract)
    ├── ValidationException
    │   └── Errors: IReadOnlyList<ValidationError>
    ├── EntityNotFoundException
    │   └── EntityType: string, EntityId: string
    └── UnauthorizedException
        └── Reason: string, RequiredPermission: string?
```

### ValidationError (Value Object)

| Field | Type | Description |
|-------|------|-------------|
| PropertyName | string | Name of the invalid property |
| ErrorMessage | string | Human-readable error message |
| ErrorCode | string | Machine-readable error code |

---

## Relationships

```
AggregateRoot<TId> ──inherits──> Entity<TId>
AuditedEntity<TId> ──inherits──> AggregateRoot<TId>
AggregateRoot<TId> ──contains──> IDomainEvent (0..*)
IntegrationEvent ──implements──> IDomainEvent
AuditLog ──references──> AuditedEntity (via EntityType + EntityId)
OpaqueId ──inherits──> ValueObject
```

---

## Database Schema (SharedKernel)

The SharedKernel does not own a SQL schema directly. Base class configurations are applied via EF Core `IEntityTypeConfiguration<T>` in each module that inherits from SharedKernel types.

**AuditLog table** (owned by each module's schema):

```sql
CREATE TABLE [{module_schema}].[AuditLogs] (
    Id              UNIQUEIDENTIFIER    NOT NULL PRIMARY KEY DEFAULT NEWSEQUENTIALID(),
    EntityType      NVARCHAR(256)       NOT NULL,
    EntityId        NVARCHAR(128)       NOT NULL,
    Action          INT                 NOT NULL,
    ChangedBy       NVARCHAR(128)       NOT NULL,
    ChangedAt       DATETIMEOFFSET      NOT NULL,
    Changes         NVARCHAR(MAX)       NULL,
    CorrelationId   NVARCHAR(128)       NULL,
    INDEX IX_AuditLog_EntityType_EntityId (EntityType, EntityId),
    INDEX IX_AuditLog_ChangedBy (ChangedBy),
    INDEX IX_AuditLog_ChangedAt (ChangedAt)
);
```
