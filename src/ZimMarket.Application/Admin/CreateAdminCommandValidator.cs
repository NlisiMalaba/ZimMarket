using FluentValidation;
using ZimMarket.Application.Auth;

namespace ZimMarket.Application.Admin;

public sealed class CreateAdminCommandValidator : AbstractValidator<CreateAdminCommand>
{
    public CreateAdminCommandValidator()
    {
        RuleFor(x => x.Email)
            .NotEmpty()
            .EmailAddress();

        RuleFor(x => x.FullName)
            .NotEmpty()
            .MaximumLength(RegisterCustomerCommandValidator.FullNameMaxLength);

        RuleFor(x => x.Password)
            .Matches(@"^(?=.*[A-Z])(?=.*\d).{8,}$")
            .WithMessage(
                "Password must be at least 8 characters and include at least one uppercase letter and one number.");
    }
}
