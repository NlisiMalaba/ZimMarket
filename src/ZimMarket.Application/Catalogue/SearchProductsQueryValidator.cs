using FluentValidation;

namespace ZimMarket.Application.Catalogue;

public sealed class SearchProductsQueryValidator : AbstractValidator<SearchProductsQuery>
{
    public SearchProductsQueryValidator()
    {
        RuleFor(x => x.Page)
            .GreaterThan(0);

        RuleFor(x => x.PageSize)
            .GreaterThan(0)
            .LessThanOrEqualTo(100);

        RuleFor(x => x.MinPriceUsd)
            .GreaterThanOrEqualTo(0)
            .When(x => x.MinPriceUsd.HasValue);

        RuleFor(x => x.MaxPriceUsd)
            .GreaterThanOrEqualTo(0)
            .When(x => x.MaxPriceUsd.HasValue);

        RuleFor(x => x)
            .Must(x => !x.MinPriceUsd.HasValue || !x.MaxPriceUsd.HasValue || x.MinPriceUsd <= x.MaxPriceUsd)
            .WithMessage("MinPriceUsd cannot be greater than MaxPriceUsd.");
    }
}
