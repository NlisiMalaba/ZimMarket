using FluentValidation;
using ZimMarket.Domain.Entities.Catalogue;
using ZimMarket.Domain.ValueObjects;

namespace ZimMarket.Application.Catalogue;

public sealed class UpdateProductCommandValidator : AbstractValidator<UpdateProductCommand>
{
    public UpdateProductCommandValidator()
    {
        RuleFor(x => x.ProductId).NotEmpty();

        RuleFor(x => x.Title)
            .NotEmpty()
            .MaximumLength(Product.MaxTitleLength);

        RuleFor(x => x.Description)
            .NotEmpty()
            .MaximumLength(Product.MaxDescriptionLength);

        RuleFor(x => x.PriceUsd)
            .GreaterThan(0);

        RuleFor(x => x.CategoryId)
            .NotEmpty();

        RuleFor(x => x.ImageKeys)
            .NotNull()
            .Must(keys => keys.Count <= Product.MaxImageKeys)
            .WithMessage($"A product can have at most {Product.MaxImageKeys} images.");

        RuleForEach(x => x.ImageKeys)
            .NotEmpty();

        RuleFor(x => x.PickupAddress)
            .NotNull();

        When(x => x.PickupAddress is not null, () =>
        {
            RuleFor(x => x.PickupAddress.Street)
                .NotEmpty()
                .MaximumLength(Address.MaxStreetLength);

            RuleFor(x => x.PickupAddress.Suburb)
                .NotEmpty()
                .MaximumLength(Address.MaxSuburbLength);

            RuleFor(x => x.PickupAddress.City)
                .NotEmpty()
                .MaximumLength(Address.MaxCityLength);

            RuleFor(x => x.PickupAddress.Country)
                .NotEmpty()
                .MaximumLength(Address.MaxCountryLength);
        });
    }
}
