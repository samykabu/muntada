using Microsoft.Extensions.Logging;

namespace Muntada.Identity.Application.Services;

/// <summary>
/// Structured audit logging service for all authentication events.
/// Logs with correlation IDs for distributed tracing (FR-019, FR-022).
/// CRITICAL: Never logs passwords, tokens, or OTP codes (FR-018, SC-009).
/// Audit logs are retained for 7 years per Saudi PDPL (FR-022).
/// </summary>
public sealed class AuditLogService
{
    private readonly ILogger<AuditLogService> _logger;

    /// <summary>
    /// Initializes a new instance of <see cref="AuditLogService"/>.
    /// </summary>
    public AuditLogService(ILogger<AuditLogService> logger)
    {
        _logger = logger;
    }

    /// <summary>Logs a user registration event.</summary>
    public void LogUserRegistered(string userId, string email, string ipAddress)
    {
        _logger.LogInformation("Auth:UserRegistered UserId={UserId} Email={Email} IP={IpAddress}",
            userId, email, ipAddress);
    }

    /// <summary>Logs a successful login.</summary>
    public void LogLoginSuccess(string userId, string email, string ipAddress, string userAgent)
    {
        _logger.LogInformation("Auth:LoginSuccess UserId={UserId} Email={Email} IP={IpAddress} UA={UserAgent}",
            userId, email, ipAddress, userAgent);
    }

    /// <summary>Logs a failed login attempt.</summary>
    public void LogLoginFailed(string email, string ipAddress, string userAgent, string reason)
    {
        _logger.LogWarning("Auth:LoginFailed Email={Email} IP={IpAddress} UA={UserAgent} Reason={Reason}",
            email, ipAddress, userAgent, reason);
    }

    /// <summary>Logs a session revocation.</summary>
    public void LogSessionRevoked(string userId, string sessionId, string reason)
    {
        _logger.LogInformation("Auth:SessionRevoked UserId={UserId} SessionId={SessionId} Reason={Reason}",
            userId, sessionId, reason);
    }

    /// <summary>Logs a password change.</summary>
    public void LogPasswordChanged(string userId, string ipAddress)
    {
        _logger.LogInformation("Auth:PasswordChanged UserId={UserId} IP={IpAddress}",
            userId, ipAddress);
    }

    /// <summary>Logs PAT creation.</summary>
    public void LogPatCreated(string userId, string patId, IEnumerable<string> scopes)
    {
        _logger.LogInformation("Auth:PATCreated UserId={UserId} PatId={PatId} Scopes={Scopes}",
            userId, patId, string.Join(",", scopes));
    }

    /// <summary>Logs PAT revocation.</summary>
    public void LogPatRevoked(string userId, string patId)
    {
        _logger.LogInformation("Auth:PATRevoked UserId={UserId} PatId={PatId}",
            userId, patId);
    }

    /// <summary>Logs OTP challenge request.</summary>
    public void LogOtpRequested(string phoneNumber, string ipAddress)
    {
        // SECURITY: Do NOT log the OTP code
        _logger.LogInformation("Auth:OTPRequested Phone={PhoneNumber} IP={IpAddress}",
            phoneNumber, ipAddress);
    }
}
