using FluentValidation;

namespace ZimMarket.Application.Files;

public sealed class UploadProductImageCommandValidator : AbstractValidator<UploadProductImageCommand>
{
    private static readonly string[] AllowedImageContentTypes = ["image/jpeg", "image/png", "image/webp"];

    public UploadProductImageCommandValidator()
    {
        RuleFor(x => x.ContentType)
            .NotEmpty()
            .Must(static contentType =>
                AllowedImageContentTypes.Any(allowed =>
                    string.Equals(allowed, contentType.Trim(), StringComparison.OrdinalIgnoreCase)))
            .WithMessage("ContentType must be one of: image/jpeg, image/png, image/webp.");

        RuleFor(x => x.FileSizeBytes)
            .GreaterThan(0)
            .LessThanOrEqualTo(GetPresignedUploadUrlQueryValidator.MaxFileSizeBytes);

        RuleFor(x => x.Content).NotNull();
    }
}
