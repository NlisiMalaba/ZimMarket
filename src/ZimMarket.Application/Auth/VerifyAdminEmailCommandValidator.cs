using FluentValidation;

namespace ZimMarket.Application.Auth;

public sealed class VerifyAdminEmailCommandValidator : AbstractValidator<VerifyAdminEmailCommand>
{
    public VerifyAdminEmailCommandValidator()
    {
        RuleFor(x => x.Token).NotEmpty().MinimumLength(32);
    }
}
