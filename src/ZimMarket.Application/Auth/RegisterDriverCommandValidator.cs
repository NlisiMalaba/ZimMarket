using FluentValidation;
using ZimMarket.Domain.ValueObjects;

namespace ZimMarket.Application.Auth;

public sealed class RegisterDriverCommandValidator : AbstractValidator<RegisterDriverCommand>
{
    public RegisterDriverCommandValidator()
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
            .MaximumLength(RegisterCustomerCommandValidator.FullNameMaxLength);

        RuleFor(x => x.Password)
            .Matches(@"^(?=.*[A-Z])(?=.*\d).{8,}$")
            .WithMessage(
                "Password must be at least 8 characters and include at least one uppercase letter and one number.");
    }
}
