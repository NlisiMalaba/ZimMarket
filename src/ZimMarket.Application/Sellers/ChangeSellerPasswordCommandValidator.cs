using FluentValidation;

namespace ZimMarket.Application.Sellers;

public sealed class ChangeSellerPasswordCommandValidator : AbstractValidator<ChangeSellerPasswordCommand>
{
    public ChangeSellerPasswordCommandValidator()
    {
        RuleFor(x => x.CurrentPassword).NotEmpty();

        RuleFor(x => x.NewPassword)
            .Matches(@"^(?=.*[A-Z])(?=.*\d).{8,}$")
            .WithMessage(
                "Password must be at least 8 characters and include at least one uppercase letter and one number.");
    }
}
