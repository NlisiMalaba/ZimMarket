using FluentValidation;

namespace ZimMarket.Application.Auth;

public sealed class SubmitSellerKycCommandValidator : AbstractValidator<SubmitSellerKycCommand>
{
    public const int DocumentKeyMaxLength = 1024;

    public SubmitSellerKycCommandValidator()
    {
        RuleFor(x => x.NationalIdKey)
            .NotEmpty()
            .MaximumLength(DocumentKeyMaxLength);

        RuleFor(x => x.ProofOfResidenceKey)
            .NotEmpty()
            .MaximumLength(DocumentKeyMaxLength);
    }
}
