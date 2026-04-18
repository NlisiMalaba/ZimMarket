using FluentValidation;

namespace ZimMarket.Application.Catalogue;

public sealed class GetSellerProductsQueryValidator : AbstractValidator<GetSellerProductsQuery>
{
    public GetSellerProductsQueryValidator()
    {
        RuleFor(x => x.Page)
            .GreaterThan(0);

        RuleFor(x => x.PageSize)
            .GreaterThan(0)
            .LessThanOrEqualTo(100);
    }
}
