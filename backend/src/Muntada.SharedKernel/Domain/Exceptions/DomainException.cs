namespace Muntada.SharedKernel.Domain.Exceptions;

/// <summary>
/// Base exception for all domain-layer errors in the Muntada platform.
/// Subclasses map to specific HTTP status codes via ErrorHandlingMiddleware.
/// </summary>
public abstract class DomainException : Exception
{
    /// <summary>
    /// Initializes a new instance of <see cref="DomainException"/>.
    /// </summary>
    /// <param name="message">The error message describing what went wrong.</param>
    protected DomainException(string message) : base(message) { }

    /// <summary>
    /// Initializes a new instance of <see cref="DomainException"/> with an inner exception.
    /// </summary>
    /// <param name="message">The error message.</param>
    /// <param name="innerException">The exception that caused this exception.</param>
    protected DomainException(string message, Exception innerException)
        : base(message, innerException) { }
}
