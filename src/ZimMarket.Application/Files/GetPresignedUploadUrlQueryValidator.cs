using FluentValidation;

namespace ZimMarket.Application.Files;

public sealed class GetPresignedUploadUrlQueryValidator : AbstractValidator<GetPresignedUploadUrlQuery>
{
    public const long MaxFileSizeBytes = 5 * 1024 * 1024;
    private static readonly string[] AllowedImageContentTypes = ["image/jpeg", "image/png", "image/webp"];

    public GetPresignedUploadUrlQueryValidator()
    {
        RuleFor(x => x.FileType)
            .IsInEnum();

        RuleFor(x => x.ContentType)
            .NotEmpty()
            .Must(static contentType =>
                AllowedImageContentTypes.Any(allowed =>
                    string.Equals(allowed, contentType.Trim(), StringComparison.OrdinalIgnoreCase)))
            .WithMessage("ContentType must be one of: image/jpeg, image/png, image/webp.");

        RuleFor(x => x.FileSizeBytes)
            .GreaterThan(0)
            .LessThanOrEqualTo(MaxFileSizeBytes)
            .WithMessage($"File size must be between 1 and {MaxFileSizeBytes} bytes (5MB).");
    }
}
