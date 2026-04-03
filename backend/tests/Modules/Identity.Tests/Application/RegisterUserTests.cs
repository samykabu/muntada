using System.Security.Cryptography;
using System.Text;
using Microsoft.EntityFrameworkCore;
using Moq;
using Muntada.Identity.Application.Commands;
using Muntada.Identity.Application.Services;
using Muntada.Identity.Domain.EmailVerification;
using Muntada.Identity.Domain.User;
using Muntada.Identity.Infrastructure;
using Muntada.SharedKernel.Application;
using UserEntity = Muntada.Identity.Domain.User.User;

namespace Muntada.Identity.Tests.Application;

public class RegisterUserTests
{
    private static IdentityDbContext CreateInMemoryDb()
    {
        var options = new DbContextOptionsBuilder<IdentityDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;
        return new IdentityDbContext(options);
    }

    [Fact]
    public async Task Handle_should_create_user_with_Unverified_status()
    {
        using var db = CreateInMemoryDb();
        var emailService = new Mock<IEmailService>();
        var eventPublisher = new Mock<IIntegrationEventPublisher>();

        var handler = new RegisterUserCommandHandler(db, emailService.Object, eventPublisher.Object);
        var command = new RegisterUserCommand("test@example.com", "StrongP@ssw0rd!", "StrongP@ssw0rd!");

        var result = await handler.Handle(command, CancellationToken.None);

        result.Should().NotBeNull();
        result.Email.Should().Be("test@example.com");

        var user = await db.Set<UserEntity>().FirstOrDefaultAsync();
        user.Should().NotBeNull();
        user!.Status.Should().Be(UserStatus.Unverified);
    }

    [Fact]
    public async Task Handle_should_send_verification_email()
    {
        using var db = CreateInMemoryDb();
        var emailService = new Mock<IEmailService>();
        var eventPublisher = new Mock<IIntegrationEventPublisher>();

        var handler = new RegisterUserCommandHandler(db, emailService.Object, eventPublisher.Object);
        var command = new RegisterUserCommand("test@example.com", "StrongP@ssw0rd!", "StrongP@ssw0rd!");

        await handler.Handle(command, CancellationToken.None);

        emailService.Verify(
            s => s.SendVerificationEmailAsync(
                "test@example.com",
                It.IsAny<string>(),
                It.IsAny<CancellationToken>()),
            Times.Once);
    }

    [Fact]
    public async Task Handle_should_reject_duplicate_email()
    {
        using var db = CreateInMemoryDb();
        var emailService = new Mock<IEmailService>();
        var eventPublisher = new Mock<IIntegrationEventPublisher>();

        // Register first user
        var handler = new RegisterUserCommandHandler(db, emailService.Object, eventPublisher.Object);
        await handler.Handle(
            new RegisterUserCommand("test@example.com", "StrongP@ssw0rd!", "StrongP@ssw0rd!"),
            CancellationToken.None);

        // Try to register same email
        var act = () => handler.Handle(
            new RegisterUserCommand("test@example.com", "AnotherP@ss1!", "AnotherP@ss1!"),
            CancellationToken.None);

        await act.Should().ThrowAsync<FluentValidation.ValidationException>();
    }

    [Fact]
    public async Task Handle_should_store_verification_token()
    {
        using var db = CreateInMemoryDb();
        var emailService = new Mock<IEmailService>();
        var eventPublisher = new Mock<IIntegrationEventPublisher>();

        var handler = new RegisterUserCommandHandler(db, emailService.Object, eventPublisher.Object);
        await handler.Handle(
            new RegisterUserCommand("test@example.com", "StrongP@ssw0rd!", "StrongP@ssw0rd!"),
            CancellationToken.None);

        var token = await db.Set<EmailVerificationToken>().FirstOrDefaultAsync();
        token.Should().NotBeNull();
        token!.TokenHash.Should().NotBeNullOrEmpty();
    }
}
