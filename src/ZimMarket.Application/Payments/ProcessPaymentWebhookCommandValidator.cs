using FluentValidation;

namespace ZimMarket.Application.Payments;

public sealed class ProcessPaymentWebhookCommandValidator : AbstractValidator<ProcessPaymentWebhookCommand>
{
    public ProcessPaymentWebhookCommandValidator()
    {
        RuleFor(x => x.Payload)
            .NotEmpty()
            .WithMessage("Webhook payload is required.");

        RuleFor(x => x.GatewayType).IsInEnum();
    }
}
