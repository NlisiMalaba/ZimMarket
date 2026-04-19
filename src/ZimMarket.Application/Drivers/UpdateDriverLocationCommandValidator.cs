using FluentValidation;

namespace ZimMarket.Application.Drivers;

public sealed class UpdateDriverLocationCommandValidator : AbstractValidator<UpdateDriverLocationCommand>
{
    public UpdateDriverLocationCommandValidator()
    {
        RuleFor(x => x.Latitude).InclusiveBetween(-90, 90);
        RuleFor(x => x.Longitude).InclusiveBetween(-180, 180);
    }
}
