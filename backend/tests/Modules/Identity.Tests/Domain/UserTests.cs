using Muntada.Identity.Domain.User;
using UserEntity = Muntada.Identity.Domain.User.User;

namespace Muntada.Identity.Tests.Domain;

public class UserTests
{
    private static UserEntity CreateTestUser()
    {
        var email = Email.Create("test@example.com");
        var password = PasswordHash.Create("StrongP@ssw0rd!");
        return UserEntity.Create(email, password, "system");
    }

    [Fact]
    public void Create_should_set_Unverified_status()
    {
        var user = CreateTestUser();
        user.Status.Should().Be(UserStatus.Unverified);
    }

    [Fact]
    public void Create_should_assign_id_and_timestamps()
    {
        var user = CreateTestUser();
        user.Id.Should().NotBeEmpty();
        user.CreatedAt.Should().BeCloseTo(DateTimeOffset.UtcNow, TimeSpan.FromSeconds(5));
    }

    [Fact]
    public void Activate_should_transition_from_Unverified_to_Active()
    {
        var user = CreateTestUser();
        user.Activate();
        user.Status.Should().Be(UserStatus.Active);
        user.Version.Should().Be(1);
    }

    [Fact]
    public void Activate_should_reject_non_Unverified_user()
    {
        var user = CreateTestUser();
        user.Activate(); // → Active

        var act = () => user.Activate();
        act.Should().Throw<InvalidOperationException>();
    }

    [Fact]
    public void Suspend_should_transition_from_Active_to_Suspended()
    {
        var user = CreateTestUser();
        user.Activate();
        user.Suspend();
        user.Status.Should().Be(UserStatus.Suspended);
    }

    [Fact]
    public void Suspend_should_reject_non_Active_user()
    {
        var user = CreateTestUser();
        var act = () => user.Suspend();
        act.Should().Throw<InvalidOperationException>();
    }

    [Fact]
    public void RecordLogin_should_update_LastLoginAt()
    {
        var user = CreateTestUser();
        user.RecordLogin();
        user.LastLoginAt.Should().NotBeNull();
        user.LastLoginAt.Should().BeCloseTo(DateTimeOffset.UtcNow, TimeSpan.FromSeconds(5));
    }

    [Fact]
    public void ChangePassword_should_update_hash()
    {
        var user = CreateTestUser();
        var newHash = PasswordHash.Create("NewStrongP@ss1!");
        user.ChangePassword(newHash);
        user.PasswordHash.Verify("NewStrongP@ss1!").Should().BeTrue();
    }
}
