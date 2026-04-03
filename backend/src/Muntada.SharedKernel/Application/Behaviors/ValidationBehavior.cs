using FluentValidation;
using MediatR;
using Muntada.SharedKernel.Domain.Exceptions;
using ValidationException = Muntada.SharedKernel.Domain.Exceptions.ValidationException;

namespace Muntada.SharedKernel.Application.Behaviors;

/// <summary>
/// MediatR pipeline behavior that runs FluentValidation validators
/// before the request handler executes. If validation fails, a
/// <see cref="ValidationException"/> is thrown with all errors.
/// </summary>
/// <typeparam name="TRequest">The MediatR request type.</typeparam>
/// <typeparam name="TResponse">The MediatR response type.</typeparam>
public sealed class ValidationBehavior<TRequest, TResponse>
    : IPipelineBehavior<TRequest, TResponse>
    where TRequest : IRequest<TResponse>
{
    private readonly IEnumerable<IValidator<TRequest>> _validators;

    /// <summary>
    /// Initializes a new instance of <see cref="ValidationBehavior{TRequest,TResponse}"/>.
    /// </summary>
    /// <param name="validators">All registered validators for the request type.</param>
    public ValidationBehavior(IEnumerable<IValidator<TRequest>> validators)
    {
        _validators = validators;
    }

    /// <summary>
    /// Validates the request before passing it to the next handler in the pipeline.
    /// </summary>
    /// <param name="request">The incoming request.</param>
    /// <param name="next">The next handler delegate.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>The response from the next handler.</returns>
    /// <exception cref="ValidationException">Thrown when validation fails.</exception>
    public async Task<TResponse> Handle(
        TRequest request,
        RequestHandlerDelegate<TResponse> next,
        CancellationToken cancellationToken)
    {
        if (!_validators.Any())
        {
            return await next();
        }

        var context = new FluentValidation.ValidationContext<TRequest>(request);

        var validationResults = await Task.WhenAll(
            _validators.Select(v => v.ValidateAsync(context, cancellationToken)));

        var errors = validationResults
            .SelectMany(r => r.Errors)
            .Where(f => f is not null)
            .Select(f => new ValidationError(
                f.PropertyName,
                f.ErrorMessage,
                f.ErrorCode))
            .ToList();

        if (errors.Count > 0)
        {
            throw new ValidationException(errors);
        }

        return await next();
    }
}
