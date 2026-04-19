using FluentValidation;
using ZimMarket.Domain.Entities.Orders;

namespace ZimMarket.Application.Orders;

public sealed class OverrideOrderStatusCommandValidator : AbstractValidator<OverrideOrderStatusCommand>
{
    public OverrideOrderStatusCommandValidator()
    {
        RuleFor(x => x.OrderId).NotEmpty();

        RuleFor(x => x.Reason)
            .Must(r => !string.IsNullOrWhiteSpace(r))
            .WithMessage("An override reason is required.")
            .Must(r => string.IsNullOrWhiteSpace(r) || r.Trim().Length <= Order.MaxAdminOverrideReasonLength)
            .WithMessage($"Reason must not exceed {Order.MaxAdminOverrideReasonLength} characters.");
    }
}
