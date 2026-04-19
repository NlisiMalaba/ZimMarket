using FluentValidation;

namespace ZimMarket.Application.Logistics;

public sealed class CreateDeliveryBatchCommandValidator : AbstractValidator<CreateDeliveryBatchCommand>
{
    public CreateDeliveryBatchCommandValidator()
    {
        RuleFor(x => x.DriverId).NotEmpty();
        RuleFor(x => x.OrderIds)
            .NotEmpty()
            .Must(ids => ids.Distinct().Count() == ids.Count)
            .WithMessage("Order ids must be unique.")
            .Must(ids => ids.All(id => id != Guid.Empty))
            .WithMessage("Order ids cannot be empty.");
    }
}
