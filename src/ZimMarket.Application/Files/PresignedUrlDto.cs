namespace ZimMarket.Application.Files;

public sealed class PresignedUrlDto
{
    public required string UploadUrl { get; init; }

    public required string FileKey { get; init; }

    public required DateTimeOffset ExpiresAt { get; init; }
}
