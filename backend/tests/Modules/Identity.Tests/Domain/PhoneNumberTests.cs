using Muntada.Identity.Domain.User;

namespace Muntada.Identity.Tests.Domain;

public class PhoneNumberTests
{
    [Theory]
    [InlineData("+966501234567")]
    [InlineData("+14155552671")]
    [InlineData("+447911123456")]
    public void Create_should_accept_valid_E164(string input)
    {
        var phone = PhoneNumber.Create(input);
        phone.Value.Should().Be(input);
    }

    [Theory]
    [InlineData("")]
    [InlineData("0501234567")]      // missing +country
    [InlineData("+0501234567")]     // starts with 0
    [InlineData("966501234567")]    // missing +
    [InlineData("+1")]              // too short
    public void Create_should_reject_invalid_numbers(string input)
    {
        var act = () => PhoneNumber.Create(input);
        act.Should().Throw<ArgumentException>();
    }

    [Fact]
    public void Equal_numbers_should_be_equal()
    {
        var a = PhoneNumber.Create("+966501234567");
        var b = PhoneNumber.Create("+966501234567");
        a.Should().Be(b);
    }
}
