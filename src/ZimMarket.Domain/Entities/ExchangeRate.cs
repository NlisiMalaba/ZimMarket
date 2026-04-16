using ZimMarket.Shared;

namespace ZimMarket.Domain.Entities;

public sealed class ExchangeRate : BaseEntity
{
    private ExchangeRate()
    {
    }

    public string BaseCurrency { get; private set; } = null!;

    public string QuoteCurrency { get; private set; } = null!;

    public decimal Rate { get; private set; }

    public DateTimeOffset EffectiveAt { get; private set; }

    public static Result<ExchangeRate> Create(
        Guid id,
        string baseCurrency,
        string quoteCurrency,
        decimal rate,
        DateTimeOffset effectiveAt,
        DateTimeOffset createdAt,
        DateTimeOffset updatedAt)
    {
        if (string.IsNullOrWhiteSpace(baseCurrency))
            return Result<ExchangeRate>.Failure("Base currency is required.");

        if (string.IsNullOrWhiteSpace(quoteCurrency))
            return Result<ExchangeRate>.Failure("Quote currency is required.");

        if (rate <= 0)
            return Result<ExchangeRate>.Failure("Rate must be greater than zero.");

        var exchangeRate = new ExchangeRate
        {
            Id = id,
            BaseCurrency = baseCurrency.Trim().ToUpperInvariant(),
            QuoteCurrency = quoteCurrency.Trim().ToUpperInvariant(),
            Rate = rate,
            EffectiveAt = effectiveAt,
            CreatedAt = createdAt,
            UpdatedAt = updatedAt
        };

        return Result<ExchangeRate>.Success(exchangeRate);
    }

    public Result<ExchangeRate> UpdateRate(decimal rate, DateTimeOffset effectiveAt)
    {
        if (rate <= 0)
            return Result<ExchangeRate>.Failure("Rate must be greater than zero.");

        Rate = rate;
        EffectiveAt = effectiveAt;
        UpdatedAt = DateTimeOffset.UtcNow;

        return Result<ExchangeRate>.Success(this);
    }
}
