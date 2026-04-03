# Research Decisions: Identity & Access Management

**Epic**: 001-identity-access
**Date**: 2026-04-03
**Status**: Approved

---

## R1: Password Hashing Strategy

**Decision**: BCrypt.Net-Next with cost factor 12.

**Rationale**: BCrypt is purpose-built for password hashing with a tunable work factor that resists brute-force attacks as hardware improves. Cost factor 12 provides approximately 250ms hash time on modern hardware, balancing security with acceptable login latency. The BCrypt.Net-Next NuGet package is the actively maintained .NET implementation with no known vulnerabilities. BCrypt's salt is embedded in the output string, simplifying storage (single `PasswordHash` column). Cost factor 12 meets OWASP 2024 recommendations for bcrypt.

**Alternatives Considered**:
- **Argon2id** (via Konscious.Security.Cryptography): Superior memory-hard algorithm and winner of the Password Hashing Competition. However, .NET ecosystem support is less mature, tuning memory/parallelism parameters adds operational complexity, and the marginal security benefit over bcrypt at cost 12 does not justify the adoption risk for this phase.
- **PBKDF2** (built-in .NET `Rfc2898DeriveBytes`): Available out of the box in .NET, but requires higher iteration counts (600k+ per OWASP) to match bcrypt's resistance, resulting in longer hash times and more CPU consumption. Less future-proof than bcrypt's adaptive work factor.
- **scrypt**: Good memory-hard properties but limited .NET library ecosystem and less community adoption compared to bcrypt. Tuning N/r/p parameters is error-prone.

---

## R2: JWT Signing Algorithm

**Decision**: RS256 (RSA-SHA256, asymmetric) with `kid` (Key ID) header for key rotation.

**Rationale**: RS256 uses an RSA private key to sign tokens and a public key to verify them. This asymmetric approach allows any service to verify tokens using only the public key (published via JWKS endpoint) without access to the signing secret. The `kid` header enables zero-downtime key rotation by allowing multiple valid keys during transition periods. RS256 is the industry standard for multi-service architectures and is natively supported by ASP.NET Core's `Microsoft.AspNetCore.Authentication.JwtBearer`.

**Alternatives Considered**:
- **HS256** (HMAC-SHA256, symmetric): Simpler setup with a shared secret. However, every service that validates tokens must possess the secret, creating a wider attack surface. Key rotation requires coordinated secret distribution. Unsuitable for architectures where token verification is distributed across services.
- **ES256** (ECDSA P-256): Smaller key sizes and faster verification than RS256. However, .NET's ECDSA support has historically had cross-platform inconsistencies, and RS256 is more universally supported by third-party integrations and API gateways. ES256 can be adopted in a future phase if performance profiling warrants it.
- **EdDSA (Ed25519)**: Modern and efficient, but not yet supported by ASP.NET Core's built-in JWT middleware without third-party libraries. Premature for production use in this stack.

---

## R3: Refresh Token Storage

**Decision**: Opaque random string (32 bytes, Base64URL-encoded). Stored as bcrypt hash in SQL Server. Transmitted as HTTP-only, Secure, SameSite=Strict cookie.

**Rationale**: Opaque tokens (as opposed to JWTs) cannot be decoded or inspected by clients, reducing information leakage. Bcrypt hashing of the stored token ensures that a database breach does not directly expose valid refresh tokens. HTTP-only cookies prevent JavaScript access (XSS mitigation). Secure flag ensures HTTPS-only transmission. SameSite=Strict prevents CSRF attacks. The 30-day lifetime balances user convenience with security. Token rotation (issuing a new refresh token on each use) limits the window of a stolen token.

**Alternatives Considered**:
- **JWT refresh tokens**: Would allow stateless validation but defeat the purpose of server-side revocation. Refresh tokens must be revocable immediately (e.g., when a user revokes a session), which requires server-side state regardless.
- **SHA256 hash storage**: Faster than bcrypt for hashing, but since refresh tokens are high-entropy random strings (not user-chosen passwords), SHA256 is actually sufficient. However, bcrypt was chosen for defense-in-depth consistency across all token types and to avoid accidental downgrade if token generation entropy is ever reduced.
- **Redis-only storage**: Fast lookups but volatile. Session data must survive Redis restarts. SQL Server provides durability; Redis provides the cache-aside layer for performance.

---

## R4: Rate Limiting

**Decision**: Redis sliding window algorithm via custom ASP.NET Core middleware.

**Rationale**: The sliding window algorithm provides smoother rate limiting than fixed windows (which allow burst at window boundaries) and is more accurate than token bucket for login attempt tracking. Redis provides atomic operations (ZADD + ZREMRANGEBYSCORE + ZCARD) for O(1) rate checking across distributed instances. Custom middleware allows per-endpoint configuration (e.g., 5 failed logins per 15 minutes per email, 3 OTP challenges per phone per hour, 3 password resets per hour per email). The middleware integrates with ASP.NET Core's pipeline and produces structured log entries for rate-limit events.

**Alternatives Considered**:
- **ASP.NET Core built-in rate limiting** (`Microsoft.AspNetCore.RateLimiting`): Available since .NET 7 with fixed window, sliding window, token bucket, and concurrency limiters. However, the built-in middleware uses in-memory storage by default, which does not work across multiple application instances. While `IRateLimiterPartition` can be extended for Redis, the custom implementation provides more control over per-endpoint rules and integration with the identity-specific rate limiting logic (e.g., keying by email+IP combination).
- **Third-party libraries** (AspNetCoreRateLimit): Mature library but adds a dependency for functionality that is straightforward to implement with Redis primitives. The custom approach keeps rate limiting logic co-located with the identity module.
- **API Gateway rate limiting** (nginx, YARP): Operates at the infrastructure layer and lacks application-level context (e.g., per-email limiting). Useful as a complementary defense but insufficient as the primary mechanism.

---

## R5: SMS Gateway

**Decision**: Abstract `ISmsService` interface in the Application layer. Twilio as the default Infrastructure implementation. Swappable via dependency injection.

**Rationale**: Abstracting behind an interface follows Clean Architecture principles and allows swapping SMS providers without modifying business logic. Twilio is chosen as the default for its reliability, global coverage (including GCC), comprehensive .NET SDK, delivery status webhooks, and competitive pricing. The interface contract is simple: `SendSmsAsync(phoneNumber, message)` returning a delivery result. For local development, a `ConsoleSmsService` implementation logs OTP codes to stdout.

**Alternatives Considered**:
- **AWS SNS**: Good global reach but adds AWS dependency to a stack that is otherwise cloud-agnostic (self-hosted K8s). Would require AWS credentials management.
- **Unifonic**: GCC-local provider with strong Saudi/Gulf coverage. Viable as a future secondary provider for better regional delivery rates. Can be added as another `ISmsService` implementation without architectural changes.
- **Azure Communication Services**: Good .NET integration but ties the platform to Azure. Conflicts with the self-hosted deployment model.

---

## R6: Email Service

**Decision**: Abstract `IEmailService` interface in the Application layer. SMTP as the default implementation. SendGrid as an alternative implementation.

**Rationale**: SMTP is universally supported and works with any email provider, making it the safest default for self-hosted deployments. The interface contract includes `SendEmailAsync(to, subject, htmlBody, textBody)`. SendGrid provides a higher-level API with delivery tracking and is suitable for production deployments requiring delivery guarantees. Both implementations are registered via DI configuration. For local development, Aspire provisions a MailDev container that captures all outbound emails.

**Alternatives Considered**:
- **AWS SES**: Cost-effective at scale but adds AWS dependency. Available as a future implementation if needed.
- **Mailgun**: Strong API and deliverability but less common in GCC markets. No significant advantage over SendGrid for this use case.
- **Postmark**: Excellent transactional email service with fast delivery. Viable alternative but SendGrid's broader feature set and GCC presence made it the preferred option.

---

## R7: OTP Code Generation

**Decision**: `System.Security.Cryptography.RandomNumberGenerator` for 6-digit codes.

**Rationale**: `RandomNumberGenerator` is the .NET cryptographic random number generator (CSPRNG). It produces uniformly distributed random bytes suitable for security-sensitive code generation. The 6-digit code is generated by taking a random integer modulo 1,000,000 and zero-padding. This provides 10^6 (1 million) possible codes, which combined with the 3-attempt limit and 15-minute expiry, makes brute-force infeasible (probability of guessing: 3/1,000,000 = 0.0003%). The code is bcrypt-hashed before storage.

**Alternatives Considered**:
- **TOTP (RFC 6238)**: Time-based OTP would enable offline validation but requires the user to have an authenticator app. This is a different feature (2FA/MFA) and is out of scope for this epic. SMS OTP is a server-generated challenge, not TOTP.
- **HOTP (RFC 4226)**: Counter-based OTP. Requires synchronized counters between client and server, which adds complexity for SMS-based delivery with no benefit.
- **`System.Random`**: Not cryptographically secure. Must never be used for security-sensitive code generation. Predictable seed-based sequences could be exploited.

---

## R8: Session Storage

**Decision**: SQL Server for persistence (source of truth) with Redis cache-aside pattern for fast lookups.

**Rationale**: Sessions must survive infrastructure restarts (Redis is volatile by default), so SQL Server is the durable store. However, every authenticated request validates the session, making low-latency lookup critical. The cache-aside pattern stores active sessions in Redis (keyed by session ID) with a TTL matching the session's remaining lifetime. On cache miss, the session is loaded from SQL Server and cached. On session revocation, both the Redis entry and SQL record are updated. This provides sub-millisecond session validation for the common case while maintaining durability.

**Alternatives Considered**:
- **Redis-only**: Fastest option but risks session loss on Redis restart. Redis persistence (RDB/AOF) mitigates this but adds operational complexity and still has a small data loss window.
- **SQL Server-only**: Durable but adds 5-15ms latency per request for session validation. Unacceptable at scale given every authenticated request requires session lookup.
- **Distributed cache (NCache, Hazelcast)**: Over-engineered for current scale. Redis is already in the stack and provides sufficient performance.

---

## R9: Token Blacklisting

**Decision**: Redis set with TTL matching the access token's remaining expiry time for immediate revocation.

**Rationale**: Access tokens are stateless JWTs that cannot be revoked once issued. When immediate revocation is needed (session revoke, password change, PAT revocation), the token's `jti` (JWT ID) is added to a Redis blacklist set. The middleware checks this set on every request. The Redis entry's TTL is set to the token's remaining lifetime, so entries auto-expire when the token would have expired naturally, preventing unbounded growth. This is a targeted approach that only adds overhead when tokens are actively revoked (rare event).

**Alternatives Considered**:
- **Short token lifetime only**: Relying solely on the 15-minute access token lifetime means revocation takes up to 15 minutes to take effect. Unacceptable for security-critical scenarios (compromised session, password change).
- **Database blacklist**: Adds database latency to every request. Redis provides sub-millisecond lookups for this high-frequency check.
- **Versioned tokens**: Including a version number in the JWT and incrementing it on revocation. Requires storing and checking the version on every request, effectively equivalent to a blacklist but with added complexity.

---

## R10: Guest Magic Link Tokens

**Decision**: 32-byte cryptographically random token (Base64URL-encoded), SHA256-hashed for storage.

**Rationale**: 32 bytes of cryptographic randomness provides 256 bits of entropy, making brute-force infeasible. Base64URL encoding produces a URL-safe string suitable for embedding in links. SHA256 is used for storage hashing (instead of bcrypt) because magic link tokens are high-entropy random values, not human-chosen passwords. SHA256 is deterministic and fast, allowing efficient lookup by hash. The token is included in the URL as a query parameter: `/join?token=<base64url>`. The hash is indexed in the database for O(1) lookup.

**Alternatives Considered**:
- **UUID v4**: Only 122 bits of randomness (vs 256 bits). While sufficient for most use cases, the higher entropy of 32 random bytes provides a larger safety margin for tokens that may be shared across untrusted channels.
- **Bcrypt hash storage**: Bcrypt's built-in salt means you cannot look up a token by its hash (you must compare against each stored hash). SHA256 is deterministic, enabling indexed lookup: `WHERE TokenHash = SHA256(incomingToken)`.
- **JWT-based magic links**: Would encode room and permission data in the token itself. However, magic links must be revocable (organizer can cancel them), which requires server-side state regardless. An opaque token with server-side metadata is simpler and more secure.
