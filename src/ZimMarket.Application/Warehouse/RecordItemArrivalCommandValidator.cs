using FluentValidation;

namespace ZimMarket.Application.Warehouse;

public sealed class RecordItemArrivalCommandValidator : AbstractValidator<RecordItemArrivalCommand>
{
    public RecordItemArrivalCommandValidator()
    {
        RuleFor(x => x.OrderId).NotEmpty();
        RuleFor(x => x.Notes)
            .MaximumLength(1000)
            .When(x => !string.IsNullOrWhiteSpace(x.Notes));
    }
}
