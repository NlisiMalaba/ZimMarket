using FluentValidation;

namespace ZimMarket.Application.Logistics;

public sealed class GetBatchDetailsQueryValidator : AbstractValidator<GetBatchDetailsQuery>
{
    public GetBatchDetailsQueryValidator()
    {
        RuleFor(x => x.BatchId)
            .NotEmpty();
    }
}
