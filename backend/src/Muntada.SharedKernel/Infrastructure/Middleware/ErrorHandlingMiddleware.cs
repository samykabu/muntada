using System.Diagnostics;
using System.Text.Json;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using Muntada.SharedKernel.Domain.Exceptions;

namespace Muntada.SharedKernel.Infrastructure.Middleware;

/// <summary>
/// ASP.NET Core middleware that catches domain exceptions and translates them
/// to RFC 9457 Problem Details JSON responses with appropriate HTTP status codes.
/// Includes correlation ID tracking for distributed tracing.
/// </summary>
public sealed class ErrorHandlingMiddleware
{
    private readonly RequestDelegate _next;
    private readonly ILogger<ErrorHandlingMiddleware> _logger;

    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        WriteIndented = false
    };

    /// <summary>
    /// Initializes a new instance of <see cref="ErrorHandlingMiddleware"/>.
    /// </summary>
    /// <param name="next">The next middleware in the pipeline.</param>
    /// <param name="logger">The logger instance.</param>
    public ErrorHandlingMiddleware(RequestDelegate next, ILogger<ErrorHandlingMiddleware> logger)
    {
        _next = next ?? throw new ArgumentNullException(nameof(next));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    /// <summary>
    /// Invokes the middleware, catching exceptions and converting them to Problem Details responses.
    /// </summary>
    /// <param name="context">The HTTP context.</param>
    public async Task InvokeAsync(HttpContext context)
    {
        try
        {
            await _next(context);
        }
        catch (Exception ex)
        {
            await HandleExceptionAsync(context, ex);
        }
    }

    private async Task HandleExceptionAsync(HttpContext context, Exception exception)
    {
        if (context.Response.HasStarted)
        {
            _logger.LogWarning(
                "Response already started, cannot write error response. Exception: {ExceptionType}",
                exception.GetType().Name);
            throw exception;
        }

        var traceId = Activity.Current?.Id ?? context.TraceIdentifier;

        var problemDetails = exception switch
        {
            ValidationException validationEx => CreateValidationProblemDetails(validationEx, traceId, context),
            EntityNotFoundException notFoundEx => CreateProblemDetails(
                "Resource Not Found", notFoundEx.Message, StatusCodes.Status404NotFound, traceId, context),
            UnauthorizedException unauthorizedEx => CreateProblemDetails(
                "Access Denied", unauthorizedEx.Message, StatusCodes.Status403Forbidden, traceId, context),
            DomainException domainEx => CreateProblemDetails(
                "Domain Error", domainEx.Message, StatusCodes.Status422UnprocessableEntity, traceId, context),
            _ => CreateProblemDetails(
                "Internal Server Error", "An unexpected error occurred.", StatusCodes.Status500InternalServerError, traceId, context)
        };

        LogException(exception, traceId);

        context.Response.StatusCode = problemDetails.Status ?? StatusCodes.Status500InternalServerError;
        context.Response.ContentType = "application/problem+json";

        await context.Response.WriteAsync(
            JsonSerializer.Serialize(problemDetails, JsonOptions));
    }

    private static ProblemDetails CreateProblemDetails(
        string title, string detail, int statusCode, string traceId, HttpContext context)
    {
        return new ProblemDetails
        {
            Type = "https://tools.ietf.org/html/rfc9457",
            Title = title,
            Status = statusCode,
            Detail = detail,
            Instance = context.Request.Path,
            Extensions = { ["traceId"] = traceId }
        };
    }

    private static ProblemDetails CreateValidationProblemDetails(
        ValidationException exception, string traceId, HttpContext context)
    {
        var problemDetails = CreateProblemDetails(
            "Validation Error",
            "One or more validation errors occurred.",
            StatusCodes.Status400BadRequest,
            traceId,
            context);

        problemDetails.Extensions["errors"] = exception.Errors.Select(e => new
        {
            e.PropertyName,
            e.ErrorMessage,
            e.ErrorCode
        });

        return problemDetails;
    }

    private void LogException(Exception exception, string traceId)
    {
        if (exception is DomainException)
        {
            _logger.LogWarning(exception,
                "Domain exception caught. TraceId: {TraceId}, Type: {ExceptionType}",
                traceId, exception.GetType().Name);
        }
        else
        {
            _logger.LogError(exception,
                "Unhandled exception caught. TraceId: {TraceId}, Type: {ExceptionType}",
                traceId, exception.GetType().Name);
        }
    }
}
