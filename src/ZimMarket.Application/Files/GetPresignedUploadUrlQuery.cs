using ZimMarket.Application.Common.Abstractions;

namespace ZimMarket.Application.Files;

public sealed record GetPresignedUploadUrlQuery(
    FileType FileType,
    string ContentType,
    long FileSizeBytes) : IQuery<PresignedUrlDto>;
