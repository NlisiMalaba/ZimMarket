using FluentValidation;

namespace ZimMarket.Application.Auth;

public sealed class LoginQueryValidator : AbstractValidator<LoginQuery>
{
    public const int DeviceInfoMaxLength = 2048;

    public LoginQueryValidator()
    {
        RuleFor(x => x.Email)
            .NotEmpty()
            .EmailAddress();

        RuleFor(x => x.Password)
            .NotEmpty();

        RuleFor(x => x.DeviceInfo)
            .MaximumLength(DeviceInfoMaxLength)
            .When(x => !string.IsNullOrWhiteSpace(x.DeviceInfo));
    }
}
