using Muntada.Identity.Infrastructure.RateLimiting;

namespace Muntada.Identity.Tests.Infrastructure;

public class RateLimitingTests
{
    [Fact]
    public async Task InMemoryStore_should_allow_within_limit()
    {
        var store = new InMemoryRateLimitStore();

        var result = await store.IsAllowedAsync("test-key", 5, TimeSpan.FromMinutes(15));

        result.Should().BeTrue();
    }

    [Fact]
    public async Task InMemoryStore_should_block_after_exceeding_limit()
    {
        var store = new InMemoryRateLimitStore();

        for (var i = 0; i < 5; i++)
            await store.IsAllowedAsync("test-key", 5, TimeSpan.FromMinutes(15));

        var result = await store.IsAllowedAsync("test-key", 5, TimeSpan.FromMinutes(15));

        result.Should().BeFalse();
    }

    [Fact]
    public async Task InMemoryStore_should_isolate_keys()
    {
        var store = new InMemoryRateLimitStore();

        for (var i = 0; i < 5; i++)
            await store.IsAllowedAsync("key-a", 5, TimeSpan.FromMinutes(15));

        var result = await store.IsAllowedAsync("key-b", 5, TimeSpan.FromMinutes(15));

        result.Should().BeTrue();
    }

    [Fact]
    public void GetPolicy_should_return_login_policy()
    {
        var policy = RateLimitPolicies.GetPolicy("/api/v1/identity/auth/login", "POST");

        policy.Should().NotBeNull();
        policy!.MaxAttempts.Should().Be(5);
        policy.Window.Should().Be(TimeSpan.FromMinutes(15));
    }

    [Fact]
    public void GetPolicy_should_return_null_for_unknown_path()
    {
        var policy = RateLimitPolicies.GetPolicy("/api/v1/other/endpoint", "POST");

        policy.Should().BeNull();
    }

    [Fact]
    public void GetPolicy_should_return_otp_policy()
    {
        var policy = RateLimitPolicies.GetPolicy("/api/v1/identity/auth/otp/challenge", "POST");

        policy.Should().NotBeNull();
        policy!.MaxAttempts.Should().Be(3);
    }
}
