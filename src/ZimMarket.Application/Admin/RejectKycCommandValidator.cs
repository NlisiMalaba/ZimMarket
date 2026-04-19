using FluentValidation;
using ZimMarket.Domain.Enums;

namespace ZimMarket.Application.Admin;

public sealed class RejectKycCommandValidator : AbstractValidator<RejectKycCommand>
{
    public RejectKycCommandValidator()
    {
        RuleFor(x => x.UserId).NotEmpty();

        RuleFor(x => x.Role)
            .Must(r => r is UserRole.Seller or UserRole.Driver)
            .WithMessage("Role must be Seller or Driver.");

        RuleFor(x => x.Reason)
            .Must(r => !string.IsNullOrWhiteSpace(r))
            .WithMessage("A rejection reason is required.")
            .Must(r => string.IsNullOrWhiteSpace(r) || r.Trim().Length <= 1000)
            .WithMessage("Rejection reason must not exceed 1000 characters.");
    }
}
