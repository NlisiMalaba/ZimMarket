using MediatR;
using ZimMarket.Application.Catalogue;
using ZimMarket.Application.Common.Interfaces;
using ZimMarket.Application.Common.Models;

namespace ZimMarket.Application.Files;

public sealed class ResolveFileReadUrlsQueryHandler
    : IRequestHandler<ResolveFileReadUrlsQuery, Result<IReadOnlyList<FileReadUrlDto>>>
{
    private static readonly TimeSpan ReadUrlTtl = TimeSpan.FromHours(24);

    private readonly IFileStorage _fileStorage;

    public ResolveFileReadUrlsQueryHandler(IFileStorage fileStorage)
    {
        _fileStorage = fileStorage ?? throw new ArgumentNullException(nameof(fileStorage));
    }

    public async Task<Result<IReadOnlyList<FileReadUrlDto>>> Handle(
        ResolveFileReadUrlsQuery request,
        CancellationToken cancellationToken)
    {
        DateTimeOffset expiresAt = DateTimeOffset.UtcNow.Add(ReadUrlTtl);
        var items = new List<FileReadUrlDto>(request.Keys.Count);

        foreach (string key in request.Keys)
        {
            string trimmed = key.Trim();
            if (!IsAllowedReadKey(trimmed))
            {
                return Result<IReadOnlyList<FileReadUrlDto>>.ValidationFailure(
                [
                    new ValidationError(nameof(request.Keys), $"Invalid or unsupported file key: {trimmed}")
                ]);
            }

            string url = await _fileStorage
                .GenerateSasUrlAsync(trimmed, expiresAt, cancellationToken)
                .ConfigureAwait(false);

            items.Add(new FileReadUrlDto(trimmed, url, expiresAt));
        }

        return Result<IReadOnlyList<FileReadUrlDto>>.Success(items);
    }

    private static bool IsAllowedReadKey(string key) =>
        ProductImageStorage.IsProductImageKey(key) ||
        key.StartsWith("profile-photos/", StringComparison.OrdinalIgnoreCase);
}
