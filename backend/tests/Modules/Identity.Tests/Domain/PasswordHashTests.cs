using Muntada.Identity.Domain.User;

namespace Muntada.Identity.Tests.Domain;

public class PasswordHashTests
{
    private const string ValidPassword = "StrongP@ssw0rd!";

    [Fact]
    public void Create_should_produce_bcrypt_hash()
    {
        var hash = PasswordHash.Create(ValidPassword);
        hash.Hash.Should().StartWith("$2");
    }

    [Fact]
    public void Verify_should_return_true_for_correct_password()
    {
        var hash = PasswordHash.Create(ValidPassword);
        hash.Verify(ValidPassword).Should().BeTrue();
    }

    [Fact]
    public void Verify_should_return_false_for_wrong_password()
    {
        var hash = PasswordHash.Create(ValidPassword);
        hash.Verify("WrongP@ssw0rd!").Should().BeFalse();
    }

    [Theory]
    [InlineData("short")]           // too short
    [InlineData("alllowercase1!")]  // no uppercase
    [InlineData("nouppercase1!!")]  // no uppercase letter
    [InlineData("NoDigitsHere!!")]  // no digit
    [InlineData("NoSpecial1Chars")] // no special char
    public void Create_should_reject_weak_passwords(string weak)
    {
        var act = () => PasswordHash.Create(weak);
        act.Should().Throw<ArgumentException>();
    }

    [Fact]
    public void FromHash_should_wrap_existing_hash()
    {
        var original = PasswordHash.Create(ValidPassword);
        var fromDb = PasswordHash.FromHash(original.Hash);
        fromDb.Verify(ValidPassword).Should().BeTrue();
    }
}
