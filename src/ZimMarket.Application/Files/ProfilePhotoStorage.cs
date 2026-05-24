using Microsoft.Extensions.Logging;
using ZimMarket.Application.Common.Interfaces;

namespace ZimMarket.Application.Files;

internal static class ProfilePhotoStorage
{
    private const string ProfilePhotosPrefix = "profile-photos/";
    private const string ProductImagesPrefix = "product-images/";

    public static string BuildKey(Guid userId, string extension) =>
        $"{ProfilePhotosPrefix}{userId:D}/{Guid.NewGuid():N}.{extension}";

    public static bool IsProfilePhotoKeyForUser(string key, Guid userId)
    {
        string trimmed = key.Trim();
        string profilePrefix = $"{ProfilePhotosPrefix}{userId:D}/";
        if (trimmed.StartsWith(profilePrefix, StringComparison.OrdinalIgnoreCase))
            return true;

        // Legacy keys from before the dedicated profile-photos container existed.
        string legacyPrefix = $"{ProductImagesPrefix}{userId:D}/";
        return trimmed.StartsWith(legacyPrefix, StringComparison.OrdinalIgnoreCase);
    }

    public static async Task TryDeleteAsync(
        IFileStorage fileStorage,
        ILogger logger,
        Guid sellerId,
        string? key,
        CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(key))
            return;

        string trimmedKey = key.Trim();
        if (!IsDeletableProfilePhotoKey(trimmedKey, sellerId))
            return;

        try
        {
            await fileStorage.DeleteAsync(trimmedKey, cancellationToken).ConfigureAwait(false);
            logger.LogInformation("Deleted profile photo {ImageKey}.", trimmedKey);
        }
        catch (Exception ex)
        {
            logger.LogWarning(ex, "Failed to delete profile photo {ImageKey}.", trimmedKey);
        }
    }

    private static bool IsDeletableProfilePhotoKey(string key, Guid sellerId) =>
        IsProfilePhotoKeyForUser(key, sellerId);
}
