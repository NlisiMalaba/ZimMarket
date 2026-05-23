using FluentValidation;

namespace ZimMarket.Application.Files;

public sealed class ResolveFileReadUrlsQueryValidator : AbstractValidator<ResolveFileReadUrlsQuery>
{
    public const int MaxKeysPerRequest = 50;

    public ResolveFileReadUrlsQueryValidator()
    {
        RuleFor(x => x.Keys).NotNull();
        RuleFor(x => x.Keys.Count).InclusiveBetween(0, MaxKeysPerRequest);
        RuleForEach(x => x.Keys).NotEmpty().MaximumLength(512);
    }
}
