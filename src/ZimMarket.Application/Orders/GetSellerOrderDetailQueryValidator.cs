using FluentValidation;

namespace ZimMarket.Application.Orders;

public sealed class GetSellerOrderDetailQueryValidator : AbstractValidator<GetSellerOrderDetailQuery>
{
    public GetSellerOrderDetailQueryValidator()
    {
        RuleFor(x => x.OrderId).NotEmpty();
    }
}

