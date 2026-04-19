using FluentValidation;
using ZimMarket.Domain.Enums;

namespace ZimMarket.Application.Warehouse;

public sealed class UpdateQcStatusCommandValidator : AbstractValidator<UpdateQcStatusCommand>
{
    public UpdateQcStatusCommandValidator()
    {
        RuleFor(x => x.WarehouseItemId).NotEmpty();
        RuleFor(x => x.QcStatus)
            .Must(s => s is WarehouseQcStatus.Passed or WarehouseQcStatus.Failed)
            .WithMessage("QC status must be Passed or Failed.");
        RuleFor(x => x.Notes)
            .MaximumLength(1000)
            .When(x => x.Notes is not null);
    }
}
