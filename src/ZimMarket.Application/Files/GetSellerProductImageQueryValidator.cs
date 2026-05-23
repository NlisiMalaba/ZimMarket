using FluentValidation;

namespace ZimMarket.Application.Files;

public sealed class GetSellerProductImageQueryValidator : AbstractValidator<GetSellerProductImageQuery>
{
    public GetSellerProductImageQueryValidator()
    {
        RuleFor(x => x.ImageKey).NotEmpty().MaximumLength(512);
    }
}
