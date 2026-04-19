using FluentValidation;
using ZimMarket.Domain.Entities.Catalogue;

namespace ZimMarket.Application.Admin;

public sealed class SuspendProductCommandValidator : AbstractValidator<SuspendProductCommand>
{
    public SuspendProductCommandValidator()
    {
        RuleFor(x => x.ProductId).NotEmpty();

        RuleFor(x => x.Reason)
            .Must(r => !string.IsNullOrWhiteSpace(r))
            .WithMessage("A suspension reason is required.")
            .Must(r => string.IsNullOrWhiteSpace(r) || r.Trim().Length <= Product.MaxSuspensionReasonLength)
            .WithMessage($"Suspension reason must not exceed {Product.MaxSuspensionReasonLength} characters.");
    }
}
