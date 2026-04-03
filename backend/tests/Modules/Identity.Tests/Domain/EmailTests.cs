using Muntada.Identity.Domain.User;

namespace Muntada.Identity.Tests.Domain;

public class EmailTests
{
    [Theory]
    [InlineData("User@Example.com", "user@example.com")]
    [InlineData("  test@test.com  ", "test@test.com")]
    public void Create_should_normalize_to_lowercase_trimmed(string input, string expected)
    {
        var email = Email.Create(input);
        email.Value.Should().Be(expected);
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    [InlineData("invalid")]
    [InlineData("@missing-local.com")]
    [InlineData("missing-domain@")]
    [InlineData("missing@.com")]
    public void Create_should_reject_invalid_emails(string input)
    {
        var act = () => Email.Create(input);
        act.Should().Throw<ArgumentException>();
    }

    [Fact]
    public void Equal_emails_should_be_equal()
    {
        var a = Email.Create("user@test.com");
        var b = Email.Create("USER@TEST.COM");
        a.Should().Be(b);
    }
}
