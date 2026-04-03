namespace Muntada.SharedKernel.Domain.Exceptions;

/// <summary>
/// Thrown when an operation is not permitted for the current user.
/// Maps to HTTP 403 Forbidden.
/// </summary>
public sealed class UnauthorizedException : DomainException
{
    /// <summary>
    /// Gets the reason the operation was denied.
    /// </summary>
    public string Reason { get; }

    /// <summary>
    /// Gets the permission that was required but not held. Null if not applicable.
    /// </summary>
    public string? RequiredPermission { get; }

    /// <summary>
    /// Initializes a new instance of <see cref="UnauthorizedException"/>.
    /// </summary>
    /// <param name="reason">A description of why access was denied.</param>
    /// <param name="requiredPermission">The permission needed, if applicable.</param>
    public UnauthorizedException(string reason, string? requiredPermission = null)
        : base($"Access denied: {reason}")
    {
        Reason = reason;
        RequiredPermission = requiredPermission;
    }
}
