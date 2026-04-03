using Muntada.Identity.Domain.Session;

namespace Muntada.Identity.Tests.Domain;

public class SessionTests
{
    private static Session CreateTestSession()
    {
        var deviceInfo = new DeviceInfo("Mozilla/5.0", "192.168.1.1", "SA");
        return Session.Create(Guid.NewGuid(), deviceInfo, Guid.NewGuid(), TimeSpan.FromDays(30));
    }

    [Fact]
    public void Create_should_set_Active_status()
    {
        var session = CreateTestSession();
        session.Status.Should().Be(SessionStatus.Active);
    }

    [Fact]
    public void Create_should_set_expiration()
    {
        var session = CreateTestSession();
        session.ExpiresAt.Should().BeAfter(DateTimeOffset.UtcNow);
    }

    [Fact]
    public void IsValid_should_return_true_for_active_non_expired_session()
    {
        var session = CreateTestSession();
        session.IsValid().Should().BeTrue();
    }

    [Fact]
    public void Revoke_should_transition_to_Revoked()
    {
        var session = CreateTestSession();
        session.Revoke();
        session.Status.Should().Be(SessionStatus.Revoked);
        session.IsValid().Should().BeFalse();
    }

    [Fact]
    public void Expire_should_transition_to_Expired()
    {
        var session = CreateTestSession();
        session.Expire();
        session.Status.Should().Be(SessionStatus.Expired);
        session.IsValid().Should().BeFalse();
    }

    [Fact]
    public void Revoke_should_be_idempotent_for_already_revoked()
    {
        var session = CreateTestSession();
        session.Revoke();
        var version = session.Version;
        session.Revoke(); // second call — no-op
        session.Version.Should().Be(version);
    }

    [Fact]
    public void RecordActivity_should_update_LastActivityAt()
    {
        var session = CreateTestSession();
        var before = session.LastActivityAt;
        session.RecordActivity();
        session.LastActivityAt.Should().BeOnOrAfter(before!.Value);
    }
}

public class RefreshTokenTests
{
    [Fact]
    public void Create_should_set_Active_status()
    {
        var token = RefreshToken.Create(Guid.NewGuid(), "hash", TimeSpan.FromDays(30));
        token.Status.Should().Be(RefreshTokenStatus.Active);
        token.IsValid().Should().BeTrue();
    }

    [Fact]
    public void Revoke_should_transition_to_Revoked()
    {
        var token = RefreshToken.Create(Guid.NewGuid(), "hash", TimeSpan.FromDays(30));
        token.Revoke();
        token.Status.Should().Be(RefreshTokenStatus.Revoked);
        token.IsValid().Should().BeFalse();
        token.RevokedAt.Should().NotBeNull();
    }
}
