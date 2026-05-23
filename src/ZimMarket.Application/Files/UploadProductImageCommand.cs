using ZimMarket.Application.Common.Abstractions;

namespace ZimMarket.Application.Files;

public sealed record UploadProductImageCommand(
    string ContentType,
    Stream Content,
    long FileSizeBytes) : IQuery<string>;
