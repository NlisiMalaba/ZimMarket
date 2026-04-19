using FluentValidation;

namespace ZimMarket.Application.Logistics;

public sealed class GetBatchesQueryValidator : AbstractValidator<GetBatchesQuery>
{
    public GetBatchesQueryValidator()
    {
        RuleFor(x => x.Page).GreaterThan(0);
        RuleFor(x => x.PageSize).InclusiveBetween(1, 100);
    }
}
