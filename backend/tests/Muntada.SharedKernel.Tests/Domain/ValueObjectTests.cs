using Muntada.SharedKernel.Domain;

namespace Muntada.SharedKernel.Tests.Domain;

public class ValueObjectTests
{
    private class Money : ValueObject
    {
        public decimal Amount { get; }
        public string Currency { get; }

        public Money(decimal amount, string currency)
        {
            Amount = amount;
            Currency = currency;
        }

        protected override IEnumerable<object?> GetEqualityComponents()
        {
            yield return Amount;
            yield return Currency;
        }
    }

    [Fact]
    public void Value_objects_with_same_values_should_be_equal()
    {
        var money1 = new Money(100m, "SAR");
        var money2 = new Money(100m, "SAR");

        money1.Should().Be(money2);
        (money1 == money2).Should().BeTrue();
    }

    [Fact]
    public void Value_objects_with_different_values_should_not_be_equal()
    {
        var money1 = new Money(100m, "SAR");
        var money2 = new Money(200m, "SAR");

        money1.Should().NotBe(money2);
        (money1 != money2).Should().BeTrue();
    }

    [Fact]
    public void Value_objects_with_same_values_should_have_same_hash_code()
    {
        var money1 = new Money(100m, "SAR");
        var money2 = new Money(100m, "SAR");

        money1.GetHashCode().Should().Be(money2.GetHashCode());
    }

    [Fact]
    public void Value_object_should_not_equal_null()
    {
        var money = new Money(100m, "SAR");

        money.Equals(null).Should().BeFalse();
    }
}
