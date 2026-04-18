using FluentValidation;

namespace ZimMarket.Application.Catalogue;

public sealed class UpdateStockCommandValidator : AbstractValidator<UpdateStockCommand>
{
    public UpdateStockCommandValidator()
    {
        RuleFor(x => x.ProductId)
            .NotEmpty();

        RuleFor(x => x.Delta)
            .NotEqual(0)
            .WithMessage("Delta must be non-zero.");
    }
}
