using ZimMarket.Domain.Enums;
using ZimMarket.Shared;

namespace ZimMarket.Domain.ValueObjects;

public sealed class Money
{
    private const int DecimalPlaces = 2;

    private Money(decimal amount, Currency currency)
    {
        Amount = amount;
        Currency = currency;
    }

    public decimal Amount { get; }

    public Currency Currency { get; }

    public static Result<Money> Create(decimal amount, Currency currency)
    {
        if (amount < 0)
            return Result<Money>.Failure("Amount cannot be negative.");

        if (!HasAtMostTwoDecimalPlaces(amount))
            return Result<Money>.Failure("Amount must have at most 2 decimal places.");

        return Result<Money>.Success(new Money(amount, currency));
    }

    public Result<Money> ToZwg(decimal zwgPerUsd)
    {
        if (zwgPerUsd <= 0)
            return Result<Money>.Failure("Exchange rate must be greater than zero.");

        if (Currency == Currency.ZWG)
            return Result<Money>.Success(this);

        var converted = decimal.Round(Amount * zwgPerUsd, DecimalPlaces, MidpointRounding.AwayFromZero);
        return Create(converted, Currency.ZWG);
    }

    public Result<Money> ToUsd(decimal zwgPerUsd)
    {
        if (zwgPerUsd <= 0)
            return Result<Money>.Failure("Exchange rate must be greater than zero.");

        if (Currency == Currency.USD)
            return Result<Money>.Success(this);

        var converted = decimal.Round(Amount / zwgPerUsd, DecimalPlaces, MidpointRounding.AwayFromZero);
        return Create(converted, Currency.USD);
    }

    private static bool HasAtMostTwoDecimalPlaces(decimal amount) =>
        amount == decimal.Round(amount, DecimalPlaces, MidpointRounding.AwayFromZero);
}
