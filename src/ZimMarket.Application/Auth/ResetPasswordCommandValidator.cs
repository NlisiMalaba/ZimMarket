using FluentValidation;

namespace ZimMarket.Application.Auth;

public sealed class ResetPasswordCommandValidator : AbstractValidator<ResetPasswordCommand>
{
    public ResetPasswordCommandValidator()
    {
        RuleFor(x => x.Token).NotEmpty().MinimumLength(32);
        RuleFor(x => x.NewPassword)
            .Matches(@"^(?=.*[A-Z])(?=.*\d).{8,}$")
            .WithMessage(
                "Password must be at least 8 characters and include at least one uppercase letter and one number.");
    }
}
