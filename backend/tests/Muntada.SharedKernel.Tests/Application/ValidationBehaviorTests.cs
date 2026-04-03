using FluentValidation;
using FluentValidation.Results;
using MediatR;
using Moq;
using Muntada.SharedKernel.Application.Behaviors;

namespace Muntada.SharedKernel.Tests.Application;

public class ValidationBehaviorTests
{
    private record TestRequest(string Name) : IRequest<string>;

    private class TestRequestValidator : AbstractValidator<TestRequest>
    {
        public TestRequestValidator()
        {
            RuleFor(x => x.Name).NotEmpty().WithMessage("Name is required");
        }
    }

    [Fact]
    public async Task Should_pass_when_no_validators_registered()
    {
        var behavior = new ValidationBehavior<TestRequest, string>(
            Enumerable.Empty<IValidator<TestRequest>>());

        var result = await behavior.Handle(
            new TestRequest(""),
            () => Task.FromResult("OK"),
            CancellationToken.None);

        result.Should().Be("OK");
    }

    [Fact]
    public async Task Should_pass_when_validation_succeeds()
    {
        var behavior = new ValidationBehavior<TestRequest, string>(
            new[] { new TestRequestValidator() });

        var result = await behavior.Handle(
            new TestRequest("Valid Name"),
            () => Task.FromResult("OK"),
            CancellationToken.None);

        result.Should().Be("OK");
    }

    [Fact]
    public async Task Should_throw_ValidationException_when_validation_fails()
    {
        var behavior = new ValidationBehavior<TestRequest, string>(
            new[] { new TestRequestValidator() });

        var act = () => behavior.Handle(
            new TestRequest(""),
            () => Task.FromResult("OK"),
            CancellationToken.None);

        await act.Should().ThrowAsync<Muntada.SharedKernel.Domain.Exceptions.ValidationException>()
            .Where(e => e.Errors.Any(err => err.PropertyName == "Name"));
    }

    private class NameValidator : AbstractValidator<TestRequest>
    {
        public NameValidator()
        {
            RuleFor(x => x.Name).NotEmpty().WithMessage("Name error");
        }
    }

    private class LengthValidator : AbstractValidator<TestRequest>
    {
        public LengthValidator()
        {
            RuleFor(x => x.Name).MinimumLength(10).WithMessage("Too short");
        }
    }

    [Fact]
    public async Task Should_collect_errors_from_multiple_validators()
    {
        var behavior = new ValidationBehavior<TestRequest, string>(
            new IValidator<TestRequest>[] { new NameValidator(), new LengthValidator() });

        var act = () => behavior.Handle(
            new TestRequest(""),
            () => Task.FromResult("OK"),
            CancellationToken.None);

        var exception = await act.Should()
            .ThrowAsync<Muntada.SharedKernel.Domain.Exceptions.ValidationException>();
        exception.Which.Errors.Should().HaveCountGreaterOrEqualTo(2);
    }
}
