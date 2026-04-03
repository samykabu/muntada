using Microsoft.Extensions.Configuration;
using Muntada.Identity.Infrastructure.Services;

namespace Muntada.Identity.Tests.Infrastructure;

public class JwtTokenServiceTests
{
    private static JwtTokenService CreateService()
    {
        var config = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["Jwt:Issuer"] = "muntada-test",
                ["Jwt:AccessTokenLifetimeMinutes"] = "15",
                ["Jwt:SecretKey"] = Convert.ToBase64String(new byte[64]),
                ["Jwt:KeyId"] = "test-key-1"
            })
            .Build();

        return new JwtTokenService(config);
    }

    [Fact]
    public void GenerateAccessToken_should_produce_valid_jwt()
    {
        var service = CreateService();
        var token = service.GenerateAccessToken("usr_test123", "tenant_1", new[] { "rooms:read" });

        token.Should().NotBeNullOrEmpty();
        token.Split('.').Should().HaveCount(3); // JWT has 3 parts
    }

    [Fact]
    public void ValidateAccessToken_should_extract_claims()
    {
        var service = CreateService();
        var token = service.GenerateAccessToken("usr_test123", "tenant_1", new[] { "rooms:read", "rooms:write" });

        var claims = service.ValidateAccessToken(token);

        claims.Should().NotBeNull();
        claims!.UserId.Should().Be("usr_test123");
        claims.Scopes.Should().Contain("rooms:read");
        claims.Scopes.Should().Contain("rooms:write");
    }

    [Fact]
    public void ValidateAccessToken_should_return_null_for_invalid_token()
    {
        var service = CreateService();
        var claims = service.ValidateAccessToken("invalid.jwt.token");

        claims.Should().BeNull();
    }

    [Fact]
    public void GenerateRefreshToken_should_produce_unique_tokens()
    {
        var service = CreateService();
        var tokens = Enumerable.Range(0, 10)
            .Select(_ => service.GenerateRefreshToken())
            .ToList();

        tokens.Should().OnlyHaveUniqueItems();
    }

    [Fact]
    public void GenerateRefreshToken_should_be_base64()
    {
        var service = CreateService();
        var token = service.GenerateRefreshToken();

        var act = () => Convert.FromBase64String(token);
        act.Should().NotThrow();
    }
}
