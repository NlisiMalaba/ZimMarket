using ZimMarket.Domain.Enums;
using ZimMarket.Domain.ValueObjects;
using FluentAssertions;

namespace ZimMarket.Domain.Tests;

public class MoneyTests
{
    [Fact]
    public void Create_rejects_negative_amount()
    {
        var result = Money.Create(-1m, Currency.USD);
        result.IsFailure.Should().BeTrue();
    }

    [Fact]
    public void ToZwg_converts_usd_using_rate()
    {
        var usd = Money.Create(10m, Currency.USD).Value!;
        var zwg = usd.ToZwg(25.5m).Value!;

        zwg.Currency.Should().Be(Currency.ZWG);
        zwg.Amount.Should().Be(255.00m);
    }

    [Fact]
    public void Same_amount_and_currency_behaves_as_value_equality_for_tests()
    {
        var a = Money.Create(100m, Currency.USD).Value!;
        var b = Money.Create(100m, Currency.USD).Value!;

        a.Amount.Should().Be(b.Amount);
        a.Currency.Should().Be(b.Currency);
        ReferenceEquals(a, b).Should().BeFalse();
    }
}
