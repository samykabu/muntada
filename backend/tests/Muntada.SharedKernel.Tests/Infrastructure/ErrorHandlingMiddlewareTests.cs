using System.Text.Json;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using Moq;
using Muntada.SharedKernel.Domain.Exceptions;
using Muntada.SharedKernel.Infrastructure.Middleware;

namespace Muntada.SharedKernel.Tests.Infrastructure;

public class ErrorHandlingMiddlewareTests
{
    private readonly Mock<ILogger<ErrorHandlingMiddleware>> _logger = new();

    private ErrorHandlingMiddleware CreateMiddleware(RequestDelegate next)
    {
        return new ErrorHandlingMiddleware(next, _logger.Object);
    }

    private static DefaultHttpContext CreateHttpContext()
    {
        var context = new DefaultHttpContext();
        context.Response.Body = new MemoryStream();
        return context;
    }

    private static async Task<JsonDocument> ReadResponseBody(HttpContext context)
    {
        context.Response.Body.Seek(0, SeekOrigin.Begin);
        return await JsonDocument.ParseAsync(context.Response.Body);
    }

    [Fact]
    public async Task Should_return_400_for_ValidationException()
    {
        var middleware = CreateMiddleware(_ =>
            throw new ValidationException("Name", "Name is required"));
        var context = CreateHttpContext();

        await middleware.InvokeAsync(context);

        context.Response.StatusCode.Should().Be(400);
        var doc = await ReadResponseBody(context);
        doc.RootElement.GetProperty("title").GetString().Should().Be("Validation Error");
    }

    [Fact]
    public async Task Should_return_404_for_EntityNotFoundException()
    {
        var middleware = CreateMiddleware(_ =>
            throw new EntityNotFoundException("User", "usr_123"));
        var context = CreateHttpContext();

        await middleware.InvokeAsync(context);

        context.Response.StatusCode.Should().Be(404);
        var doc = await ReadResponseBody(context);
        doc.RootElement.GetProperty("title").GetString().Should().Be("Resource Not Found");
    }

    [Fact]
    public async Task Should_return_403_for_UnauthorizedException()
    {
        var middleware = CreateMiddleware(_ =>
            throw new UnauthorizedException("Not allowed"));
        var context = CreateHttpContext();

        await middleware.InvokeAsync(context);

        context.Response.StatusCode.Should().Be(403);
        var doc = await ReadResponseBody(context);
        doc.RootElement.GetProperty("title").GetString().Should().Be("Access Denied");
    }

    [Fact]
    public async Task Should_return_500_for_unhandled_exception()
    {
        var middleware = CreateMiddleware(_ =>
            throw new InvalidOperationException("Something broke"));
        var context = CreateHttpContext();

        await middleware.InvokeAsync(context);

        context.Response.StatusCode.Should().Be(500);
        var doc = await ReadResponseBody(context);
        doc.RootElement.GetProperty("title").GetString().Should().Be("Internal Server Error");
    }

    [Fact]
    public async Task Should_include_traceId_in_response()
    {
        var middleware = CreateMiddleware(_ =>
            throw new EntityNotFoundException("User", "usr_123"));
        var context = CreateHttpContext();

        await middleware.InvokeAsync(context);

        var doc = await ReadResponseBody(context);
        doc.RootElement.GetProperty("traceId").GetString().Should().NotBeNullOrEmpty();
    }

    [Fact]
    public async Task Should_pass_through_when_no_exception()
    {
        var middleware = CreateMiddleware(ctx =>
        {
            ctx.Response.StatusCode = 200;
            return Task.CompletedTask;
        });
        var context = CreateHttpContext();

        await middleware.InvokeAsync(context);

        context.Response.StatusCode.Should().Be(200);
    }
}
