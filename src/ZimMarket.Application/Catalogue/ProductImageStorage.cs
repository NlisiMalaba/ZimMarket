using Microsoft.Extensions.Logging;
using ZimMarket.Application.Common.Interfaces;

namespace ZimMarket.Application.Catalogue;

internal static class ProductImageStorage
{
    private const string ProductImagesPrefix = "product-images/";

    public static async Task DeleteOrphanedAsync(
        IFileStorage fileStorage,
        IReadOnlyList<string> previousKeys,
        IReadOnlyList<string> nextKeys,
        ILogger logger,
        CancellationToken cancellationToken)
    {
        var retainedKeys = new HashSet<string>(
            nextKeys.Select(key => key.Trim()),
            StringComparer.Ordinal);

        foreach (string previousKey in previousKeys)
        {
            string trimmedKey = previousKey.Trim();
            if (retainedKeys.Contains(trimmedKey))
                continue;

            if (!IsProductImageKey(trimmedKey))
                continue;

            await TryDeleteAsync(fileStorage, logger, trimmedKey, cancellationToken).ConfigureAwait(false);
        }
    }

    public static async Task DeleteAllAsync(
        IFileStorage fileStorage,
        IReadOnlyList<string> imageKeys,
        ILogger logger,
        CancellationToken cancellationToken)
    {
        foreach (string imageKey in imageKeys)
        {
            string trimmedKey = imageKey.Trim();
            if (!IsProductImageKey(trimmedKey))
                continue;

            await TryDeleteAsync(fileStorage, logger, trimmedKey, cancellationToken).ConfigureAwait(false);
        }
    }

    private static bool IsProductImageKey(string key) =>
        key.StartsWith(ProductImagesPrefix, StringComparison.OrdinalIgnoreCase);

    private static async Task TryDeleteAsync(
        IFileStorage fileStorage,
        ILogger logger,
        string key,
        CancellationToken cancellationToken)
    {
        try
        {
            await fileStorage.DeleteAsync(key, cancellationToken).ConfigureAwait(false);
            logger.LogInformation("Deleted product image {ImageKey}.", key);
        }
        catch (Exception ex)
        {
            logger.LogWarning(ex, "Failed to delete product image {ImageKey}.", key);
        }
    }
}
