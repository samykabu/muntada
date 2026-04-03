using Muntada.Identity.Domain.Otp;

namespace Muntada.Identity.Tests.Domain;

public class OtpChallengeTests
{
    private static OtpChallenge CreateTestChallenge()
    {
        return OtpChallenge.Create("+966501234567", "hashed_code", TimeSpan.FromMinutes(15));
    }

    [Fact]
    public void Create_should_set_Pending_status()
    {
        var challenge = CreateTestChallenge();
        challenge.Status.Should().Be(OtpStatus.Pending);
        challenge.FailedAttempts.Should().Be(0);
    }

    [Fact]
    public void IsValid_should_return_true_for_new_challenge()
    {
        var challenge = CreateTestChallenge();
        challenge.IsValid().Should().BeTrue();
    }

    [Fact]
    public void IncrementFailedAttempts_should_increase_count()
    {
        var challenge = CreateTestChallenge();
        challenge.IncrementFailedAttempts();
        challenge.FailedAttempts.Should().Be(1);
    }

    [Fact]
    public void IsValid_should_return_false_after_3_attempts()
    {
        var challenge = CreateTestChallenge();
        challenge.IncrementFailedAttempts();
        challenge.IncrementFailedAttempts();
        challenge.IncrementFailedAttempts();
        challenge.IsValid().Should().BeFalse();
    }

    [Fact]
    public void MarkVerified_should_transition_to_Verified()
    {
        var challenge = CreateTestChallenge();
        challenge.MarkVerified();
        challenge.Status.Should().Be(OtpStatus.Verified);
    }

    [Fact]
    public void MarkVerified_should_be_idempotent_for_non_Pending()
    {
        var challenge = CreateTestChallenge();
        challenge.MarkVerified();
        var version = challenge.Version;
        challenge.MarkVerified(); // second call — no-op
        challenge.Version.Should().Be(version);
        challenge.Status.Should().Be(OtpStatus.Verified);
    }
}
