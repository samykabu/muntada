using Muntada.SharedKernel.Domain.Exceptions;

namespace Muntada.SharedKernel.Tests.Domain;

public class ExceptionTests
{
    [Fact]
    public void ValidationException_should_contain_errors()
    {
        var errors = new[]
        {
            new ValidationError("Name", "Name is required"),
            new ValidationError("Email", "Email is invalid", "InvalidEmail")
        };

        var exception = new ValidationException(errors);

        exception.Errors.Should().HaveCount(2);
        exception.Errors[0].PropertyName.Should().Be("Name");
        exception.Errors[1].ErrorCode.Should().Be("InvalidEmail");
    }

    [Fact]
    public void ValidationException_single_error_constructor_should_work()
    {
        var exception = new ValidationException("Name", "Name is required");

        exception.Errors.Should().HaveCount(1);
        exception.Errors[0].PropertyName.Should().Be("Name");
    }

    [Fact]
    public void EntityNotFoundException_should_contain_type_and_id()
    {
        var exception = new EntityNotFoundException("User", "usr_abc123");

        exception.EntityType.Should().Be("User");
        exception.EntityId.Should().Be("usr_abc123");
        exception.Message.Should().Contain("User");
        exception.Message.Should().Contain("usr_abc123");
    }

    [Fact]
    public void UnauthorizedException_should_contain_reason()
    {
        var exception = new UnauthorizedException("Insufficient permissions", "rooms:create");

        exception.Reason.Should().Be("Insufficient permissions");
        exception.RequiredPermission.Should().Be("rooms:create");
    }

    [Fact]
    public void UnauthorizedException_should_allow_null_permission()
    {
        var exception = new UnauthorizedException("Access denied");

        exception.RequiredPermission.Should().BeNull();
    }

    [Fact]
    public void All_domain_exceptions_should_inherit_from_DomainException()
    {
        var validation = new ValidationException("Field", "Error");
        var notFound = new EntityNotFoundException("User", "id");
        var unauthorized = new UnauthorizedException("Denied");

        validation.Should().BeAssignableTo<DomainException>();
        notFound.Should().BeAssignableTo<DomainException>();
        unauthorized.Should().BeAssignableTo<DomainException>();
    }
}
