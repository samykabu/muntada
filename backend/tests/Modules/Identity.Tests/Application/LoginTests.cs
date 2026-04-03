using Microsoft.EntityFrameworkCore;
using Moq;
using Muntada.Identity.Application.Commands;
using Muntada.Identity.Application.Services;
using Muntada.Identity.Domain.User;
using Muntada.Identity.Infrastructure;
using Muntada.SharedKernel.Application;
using UserEntity = Muntada.Identity.Domain.User.User;

namespace Muntada.Identity.Tests.Application;

public class LoginTests
{
    private static IdentityDbContext CreateInMemoryDb()
    {
        var options = new DbContextOptionsBuilder<IdentityDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;
        return new IdentityDbContext(options);
    }

    private static async Task<UserEntity> SeedActiveUser(IdentityDbContext db, string email = "test@example.com", string password = "StrongP@ssw0rd!")
    {
        var user = UserEntity.Create(Email.Create(email), PasswordHash.Create(password), "system");
        user.Activate();
        db.Set<UserEntity>().Add(user);
        await db.SaveChangesAsync();
        return user;
    }

    [Fact]
    public async Task Handle_should_return_access_token_for_valid_credentials()
    {
        using var db = CreateInMemoryDb();
        await SeedActiveUser(db);

        var tokenService = new Mock<ITokenService>();
        tokenService.Setup(t => t.GenerateAccessToken(It.IsAny<string>(), It.IsAny<string?>(), It.IsAny<IEnumerable<string>>()))
            .Returns("jwt.test.token");
        tokenService.Setup(t => t.GenerateRefreshToken()).Returns("refresh-token-value");

        var eventPublisher = new Mock<IIntegrationEventPublisher>();

        var handler = new LoginCommandHandler(db, tokenService.Object, eventPublisher.Object);
        var command = new LoginCommand("test@example.com", "StrongP@ssw0rd!", "Mozilla/5.0", "127.0.0.1");

        var result = await handler.Handle(command, CancellationToken.None);

        result.Should().NotBeNull();
        result.AccessToken.Should().Be("jwt.test.token");
    }

    [Fact]
    public async Task Handle_should_reject_invalid_password()
    {
        using var db = CreateInMemoryDb();
        await SeedActiveUser(db);

        var tokenService = new Mock<ITokenService>();
        var eventPublisher = new Mock<IIntegrationEventPublisher>();

        var handler = new LoginCommandHandler(db, tokenService.Object, eventPublisher.Object);
        var command = new LoginCommand("test@example.com", "WrongPassword1!", "Mozilla/5.0", "127.0.0.1");

        var act = () => handler.Handle(command, CancellationToken.None);

        await act.Should().ThrowAsync<FluentValidation.ValidationException>()
            .WithMessage("*Invalid email or password*");
    }

    [Fact]
    public async Task Handle_should_reject_nonexistent_email()
    {
        using var db = CreateInMemoryDb();

        var tokenService = new Mock<ITokenService>();
        var eventPublisher = new Mock<IIntegrationEventPublisher>();

        var handler = new LoginCommandHandler(db, tokenService.Object, eventPublisher.Object);
        var command = new LoginCommand("nobody@example.com", "StrongP@ssw0rd!", "Mozilla/5.0", "127.0.0.1");

        var act = () => handler.Handle(command, CancellationToken.None);

        await act.Should().ThrowAsync<FluentValidation.ValidationException>()
            .WithMessage("*Invalid email or password*");
    }

    [Fact]
    public async Task Handle_should_reject_unverified_user()
    {
        using var db = CreateInMemoryDb();
        // Create user but DON'T activate
        var user = UserEntity.Create(Email.Create("unverified@example.com"), PasswordHash.Create("StrongP@ssw0rd!"), "system");
        db.Set<UserEntity>().Add(user);
        await db.SaveChangesAsync();

        var tokenService = new Mock<ITokenService>();
        var eventPublisher = new Mock<IIntegrationEventPublisher>();

        var handler = new LoginCommandHandler(db, tokenService.Object, eventPublisher.Object);
        var command = new LoginCommand("unverified@example.com", "StrongP@ssw0rd!", "Mozilla/5.0", "127.0.0.1");

        var act = () => handler.Handle(command, CancellationToken.None);

        await act.Should().ThrowAsync<FluentValidation.ValidationException>();
    }

    [Fact]
    public async Task Handle_should_create_session()
    {
        using var db = CreateInMemoryDb();
        await SeedActiveUser(db);

        var tokenService = new Mock<ITokenService>();
        tokenService.Setup(t => t.GenerateAccessToken(It.IsAny<string>(), It.IsAny<string?>(), It.IsAny<IEnumerable<string>>()))
            .Returns("jwt.test.token");
        tokenService.Setup(t => t.GenerateRefreshToken()).Returns("refresh-token-value");

        var eventPublisher = new Mock<IIntegrationEventPublisher>();

        var handler = new LoginCommandHandler(db, tokenService.Object, eventPublisher.Object);
        await handler.Handle(
            new LoginCommand("test@example.com", "StrongP@ssw0rd!", "Mozilla/5.0", "127.0.0.1"),
            CancellationToken.None);

        var session = await db.Sessions.FirstOrDefaultAsync();
        session.Should().NotBeNull();
        session!.Status.Should().Be(Muntada.Identity.Domain.Session.SessionStatus.Active);
    }
}
