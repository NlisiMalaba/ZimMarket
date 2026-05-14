namespace ZimMarket.Application.Common.Interfaces;

public interface ILocalFileStorageAccess
{
    bool IsReadRequestAuthorized(string key, DateTimeOffset expiresAt, string signature);

    bool IsUploadRequestAuthorized(string key, string contentType, DateTimeOffset expiresAt, string signature);

    Task<Stream> OpenReadAsync(string key, CancellationToken cancellationToken = default);

    string GetContentType(string key);
}
