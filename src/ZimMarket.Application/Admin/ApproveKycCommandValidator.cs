using FluentValidation;
using ZimMarket.Domain.Enums;

namespace ZimMarket.Application.Admin;

public sealed class ApproveKycCommandValidator : AbstractValidator<ApproveKycCommand>
{
    public ApproveKycCommandValidator()
    {
        RuleFor(x => x.UserId).NotEmpty();

        RuleFor(x => x.Role)
            .Must(r => r is UserRole.Seller or UserRole.Driver)
            .WithMessage("Role must be Seller or Driver.");
    }
}
