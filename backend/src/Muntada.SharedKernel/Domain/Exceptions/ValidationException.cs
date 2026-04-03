namespace Muntada.SharedKernel.Domain.Exceptions;

/// <summary>
/// Thrown when input validation fails. Maps to HTTP 400 Bad Request.
/// Contains a list of <see cref="ValidationError"/> describing each failure.
/// </summary>
public sealed class ValidationException : DomainException
{
    /// <summary>
    /// Gets the collection of validation errors.
    /// </summary>
    public IReadOnlyList<ValidationError> Errors { get; }

    /// <summary>
    /// Initializes a new instance of <see cref="ValidationException"/> with a list of errors.
    /// </summary>
    /// <param name="errors">The validation errors.</param>
    public ValidationException(IEnumerable<ValidationError> errors)
        : base("One or more validation errors occurred.")
    {
        Errors = errors.ToList().AsReadOnly();
    }

    /// <summary>
    /// Initializes a new instance of <see cref="ValidationException"/> with a single error.
    /// </summary>
    /// <param name="propertyName">The name of the invalid property.</param>
    /// <param name="errorMessage">The error message.</param>
    public ValidationException(string propertyName, string errorMessage)
        : this(new[] { new ValidationError(propertyName, errorMessage) }) { }
}

/// <summary>
/// Describes a single validation failure for a specific property.
/// </summary>
/// <param name="PropertyName">The name of the property that failed validation.</param>
/// <param name="ErrorMessage">A human-readable description of the failure.</param>
/// <param name="ErrorCode">An optional machine-readable error code.</param>
public sealed record ValidationError(
    string PropertyName,
    string ErrorMessage,
    string? ErrorCode = null);
