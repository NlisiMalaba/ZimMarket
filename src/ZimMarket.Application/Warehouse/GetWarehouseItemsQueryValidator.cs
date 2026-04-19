using FluentValidation;

namespace ZimMarket.Application.Warehouse;

public sealed class GetWarehouseItemsQueryValidator : AbstractValidator<GetWarehouseItemsQuery>
{
    public GetWarehouseItemsQueryValidator()
    {
        RuleFor(x => x.Page).GreaterThan(0);
        RuleFor(x => x.PageSize).InclusiveBetween(1, 100);
    }
}
