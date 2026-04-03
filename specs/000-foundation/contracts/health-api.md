# API Contract: Health Check Endpoints

**Feature**: 000-foundation
**Date**: 2026-04-03
**Base Path**: `/health`

---

## GET /health

**Purpose**: Overall system health status. Used by load balancers and monitoring.

**Response 200 OK**:
```json
{
  "status": "Healthy",
  "timestamp": "2026-04-03T12:00:00Z",
  "version": "1.0.0",
  "uptime": "00:15:32"
}
```

**Response 503 Service Unavailable**:
```json
{
  "status": "Unhealthy",
  "timestamp": "2026-04-03T12:00:00Z",
  "version": "1.0.0",
  "checks": [
    {
      "name": "sql-server",
      "status": "Unhealthy",
      "description": "Connection refused"
    }
  ]
}
```

---

## GET /health/ready

**Purpose**: Kubernetes readiness probe. Returns 200 when the application is ready to accept traffic (all dependencies connected).

**Response 200 OK**:
```json
{
  "status": "Ready",
  "checks": [
    { "name": "sql-server", "status": "Healthy" },
    { "name": "redis", "status": "Healthy" },
    { "name": "rabbitmq", "status": "Healthy" }
  ]
}
```

**Response 503 Not Ready**:
```json
{
  "status": "NotReady",
  "checks": [
    { "name": "sql-server", "status": "Healthy" },
    { "name": "redis", "status": "Unhealthy", "description": "Connection timeout" },
    { "name": "rabbitmq", "status": "Healthy" }
  ]
}
```

---

## GET /health/live

**Purpose**: Kubernetes liveness probe. Returns 200 if the process is alive and not deadlocked. Lightweight — no dependency checks.

**Response 200 OK**:
```json
{
  "status": "Alive"
}
```

**Response 503 Dead**:
```json
{
  "status": "Dead"
}
```

---

## Error Response Format (RFC 9457 Problem Details)

All API error responses follow this format:

```json
{
  "type": "https://tools.ietf.org/html/rfc9457",
  "title": "Validation Error",
  "status": 400,
  "detail": "One or more validation errors occurred.",
  "instance": "/api/v1/rooms",
  "traceId": "00-abc123-def456-01",
  "errors": [
    {
      "propertyName": "name",
      "errorMessage": "Name must not be empty.",
      "errorCode": "NotEmptyValidator"
    }
  ]
}
```

### HTTP Status Code Mapping

| Exception Type | HTTP Status | Title |
|---------------|-------------|-------|
| ValidationException | 400 Bad Request | Validation Error |
| UnauthorizedException | 403 Forbidden | Access Denied |
| EntityNotFoundException | 404 Not Found | Resource Not Found |
| DomainException | 422 Unprocessable Entity | Domain Error |
| Unhandled Exception | 500 Internal Server Error | Internal Server Error |
