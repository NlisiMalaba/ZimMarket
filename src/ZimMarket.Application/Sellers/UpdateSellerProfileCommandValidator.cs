using FluentValidation;
using ZimMarket.Application.Auth;
using ZimMarket.Domain.ValueObjects;

namespace ZimMarket.Application.Sellers;

public sealed class UpdateSellerProfileCommandValidator : AbstractValidator<UpdateSellerProfileCommand>
{
    public UpdateSellerProfileCommandValidator()
    {
        RuleFor(x => x.Email)
            .NotEmpty()
            .EmailAddress();

        RuleFor(x => x.Phone)
            .NotEmpty()
            .Must(static p => PhoneNumber.Create(p.Trim()).IsSuccess)
            .WithMessage("Phone number must be a valid Zimbabwe international number (e.g. +2637XXXXXXXX).");

        RuleFor(x => x.FullName)
            .NotEmpty()
            .MaximumLength(RegisterCustomerCommandValidator.FullNameMaxLength);

        RuleFor(x => x.BusinessName)
            .NotEmpty()
            .MaximumLength(RegisterSellerCommandValidator.BusinessNameMaxLength);

        RuleFor(x => x.ProfilePhotoKey)
            .MaximumLength(SubmitSellerKycCommandValidator.DocumentKeyMaxLength)
            .When(x => !string.IsNullOrWhiteSpace(x.ProfilePhotoKey));

        When(x => x.DefaultPickupAddress is not null, () =>
        {
            RuleFor(x => x.DefaultPickupAddress!.Street).NotEmpty().MaximumLength(Address.MaxStreetLength);
            RuleFor(x => x.DefaultPickupAddress!.Suburb).NotEmpty().MaximumLength(Address.MaxSuburbLength);
            RuleFor(x => x.DefaultPickupAddress!.City).NotEmpty().MaximumLength(Address.MaxCityLength);
            RuleFor(x => x.DefaultPickupAddress!.Country).NotEmpty().MaximumLength(Address.MaxCountryLength);
        });
    }
}
