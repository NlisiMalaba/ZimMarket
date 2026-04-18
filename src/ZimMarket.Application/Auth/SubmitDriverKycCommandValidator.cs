using FluentValidation;

namespace ZimMarket.Application.Auth;

public sealed class SubmitDriverKycCommandValidator : AbstractValidator<SubmitDriverKycCommand>
{
    public const int DocumentKeyMaxLength = 1024;
    public const int LicenseOrRegistrationMaxLength = 100;

    public SubmitDriverKycCommandValidator()
    {
        RuleFor(x => x.LicenseDocKey)
            .NotEmpty()
            .MaximumLength(DocumentKeyMaxLength);

        RuleFor(x => x.VehicleDocKey)
            .NotEmpty()
            .MaximumLength(DocumentKeyMaxLength);

        RuleFor(x => x.LicenseNumber)
            .NotEmpty()
            .MaximumLength(LicenseOrRegistrationMaxLength);

        RuleFor(x => x.VehicleRegistration)
            .NotEmpty()
            .MaximumLength(LicenseOrRegistrationMaxLength);
    }
}
