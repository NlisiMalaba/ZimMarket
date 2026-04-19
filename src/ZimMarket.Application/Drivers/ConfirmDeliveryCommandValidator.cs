using FluentValidation;

namespace ZimMarket.Application.Drivers;

public sealed class ConfirmDeliveryCommandValidator : AbstractValidator<ConfirmDeliveryCommand>
{
    public ConfirmDeliveryCommandValidator()
    {
        RuleFor(x => x.BatchId).NotEmpty();
        RuleFor(x => x.OrderId).NotEmpty();
        RuleFor(x => x.DeliveryPhotoKey).NotEmpty();
    }
}
