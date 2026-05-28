using ZimMarket.Application.Common.Abstractions;

namespace ZimMarket.Application.Files;

public sealed record UploadProfilePhotoCommand(
    string ContentType,
    Stream Content,
    long FileSizeBytes) : IQuery<string>;
