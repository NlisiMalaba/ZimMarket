using FluentValidation;

namespace ZimMarket.Application.Drivers;

public sealed class ConfirmBatchCollectedCommandValidator : AbstractValidator<ConfirmBatchCollectedCommand>
{
    public ConfirmBatchCollectedCommandValidator()
    {
        RuleFor(x => x.BatchId).NotEmpty();
    }
}
