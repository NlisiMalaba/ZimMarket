using FluentValidation;

namespace ZimMarket.Application.Orders;

public sealed class GetAllOrdersQueryValidator : AbstractValidator<GetAllOrdersQuery>
{
    public GetAllOrdersQueryValidator()
    {
        RuleFor(x => x)
            .Must(q => !q.DateFrom.HasValue || !q.DateTo.HasValue || q.DateFrom.Value <= q.DateTo.Value)
            .WithMessage("DateFrom must be on or before DateTo.");
    }
}
