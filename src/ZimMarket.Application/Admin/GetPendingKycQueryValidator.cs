using FluentValidation;
using ZimMarket.Domain.Enums;

namespace ZimMarket.Application.Admin;

public sealed class GetPendingKycQueryValidator : AbstractValidator<GetPendingKycQuery>
{
    public GetPendingKycQueryValidator()
    {
        RuleFor(x => x.Role)
            .Must(r => r is UserRole.Seller or UserRole.Driver)
            .WithMessage("Role must be Seller or Driver.");
    }
}
