using FluentValidation;
using ZimMarket.Domain.ValueObjects;

namespace ZimMarket.Application.Auth;

public sealed class RegisterCustomerCommandValidator : AbstractValidator<RegisterCustomerCommand>
{
    public const int FullNameMaxLength = 200;

    public RegisterCustomerCommandValidator()
    {
        RuleFor(x => x.Email)
            .NotEmpty()
            .EmailAddress();

        RuleFor(x => x.Phone)
            .NotEmpty()
            .Must(static p => PhoneNumber.Create(p.Trim()).IsSuccess)
            .WithMessage("Phone number must be a valid Zimbabwe international number (e.g. +2637XXXXXXXX).");

        RuleFor(x => x.FullName)
            .NotEmpty()
            .MaximumLength(FullNameMaxLength);

        RuleFor(x => x.Password)
            .Matches(@"^(?=.*[A-Z])(?=.*\d).{8,}$")
            .WithMessage(
                "Password must be at least 8 characters and include at least one uppercase letter and one number.");

        RuleFor(x => x.PushToken)
            .MaximumLength(512)
            .When(x => !string.IsNullOrWhiteSpace(x.PushToken));
    }
}
