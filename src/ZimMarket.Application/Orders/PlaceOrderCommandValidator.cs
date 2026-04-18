using FluentValidation;

namespace ZimMarket.Application.Orders;

public sealed class PlaceOrderCommandValidator : AbstractValidator<PlaceOrderCommand>
{
    public PlaceOrderCommandValidator()
    {
        RuleFor(x => x.Items)
            .NotNull()
            .NotEmpty();

        RuleForEach(x => x.Items)
            .ChildRules(item =>
            {
                item.RuleFor(i => i.ProductId).NotEmpty();
                item.RuleFor(i => i.Quantity).GreaterThan(0);
            });

        RuleFor(x => x.DeliveryAddress).NotNull();

        RuleFor(x => x.DeliveryAddress.Street)
            .NotEmpty();

        RuleFor(x => x.DeliveryAddress.Suburb)
            .NotEmpty();

        RuleFor(x => x.DeliveryAddress.City)
            .NotEmpty();

        RuleFor(x => x.DeliveryAddress.Country)
            .NotEmpty();
    }
}
