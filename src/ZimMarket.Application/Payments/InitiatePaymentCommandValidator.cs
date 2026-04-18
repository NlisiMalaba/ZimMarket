using FluentValidation;

namespace ZimMarket.Application.Payments;

public sealed class InitiatePaymentCommandValidator : AbstractValidator<InitiatePaymentCommand>
{
    public const int IdempotencyKeyMaxLength = 128;

    public InitiatePaymentCommandValidator()
    {
        RuleFor(x => x.OrderId).NotEmpty();

        RuleFor(x => x.IdempotencyKey)
            .NotEmpty()
            .MaximumLength(IdempotencyKeyMaxLength)
            .Must(static k => k.Trim().Length > 0)
            .WithMessage("Idempotency key cannot be whitespace.");
    }
}
