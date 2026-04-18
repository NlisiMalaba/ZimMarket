using FluentValidation;

namespace ZimMarket.Application.Orders;

public sealed class CancelOrderCommandValidator : AbstractValidator<CancelOrderCommand>
{
    public const int ReasonMaxLength = 1000;

    public CancelOrderCommandValidator()
    {
        RuleFor(x => x.OrderId)
            .NotEmpty();

        RuleFor(x => x.Reason)
            .NotEmpty()
            .MaximumLength(ReasonMaxLength);
    }
}
