using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;
using Microsoft.Extensions.Logging;
using ZimMarket.API.Http;
using ZimMarket.API.RateLimiting;
using ZimMarket.Application.Auth;
using ZimMarket.Application.Common.Interfaces;
using ZimMarket.Application.Common.Models;
using ZimMarket.Application.Files;

namespace ZimMarket.API.Controllers.V1;

[ApiController]
[Route("api/v1/files")]
public sealed class FilesController : ControllerBase
{
    private static readonly TimeSpan KycReadSasLifetime = TimeSpan.FromMinutes(10);
    private const string KycDocumentsContainerPrefix = "kyc-documents/";

    private readonly ISender _sender;
    private readonly IFileStorage _fileStorage;
    private readonly ILocalFileStorageAccess? _localFileStorageAccess;
    private readonly ILogger<FilesController> _logger;

    public FilesController(
        ISender sender,
        IFileStorage fileStorage,
        IEnumerable<ILocalFileStorageAccess> localFileStorageAccess,
        ILogger<FilesController> logger)
    {
        _sender = sender ?? throw new ArgumentNullException(nameof(sender));
        _fileStorage = fileStorage ?? throw new ArgumentNullException(nameof(fileStorage));
        _localFileStorageAccess = localFileStorageAccess?.SingleOrDefault();
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    [HttpPost("product-image")]
    [Authorize(Policy = AuthorizationPolicies.Seller)]
    [RequestSizeLimit(5 * 1024 * 1024)]
    public async Task<IActionResult> UploadProductImage(
        IFormFile file,
        CancellationToken cancellationToken)
    {
        if (file.Length == 0)
        {
            return Result<string>.ValidationFailure(
            [
                new ValidationError(nameof(file), "Image file is required.")
            ]).ToOkActionResult(HttpContext);
        }

        await using Stream stream = file.OpenReadStream();
        var command = new UploadProductImageCommand(
            file.ContentType,
            stream,
            file.Length);

        return (await _sender.Send(command, cancellationToken).ConfigureAwait(false))
            .ToOkActionResult(HttpContext);
    }

    [HttpGet("seller-product-image")]
    [Authorize(Policy = AuthorizationPolicies.Seller)]
    public async Task<IActionResult> GetSellerProductImage(
        [FromQuery] string key,
        CancellationToken cancellationToken)
    {
        Result<SellerProductImageContentDto> result = await _sender
            .Send(new GetSellerProductImageQuery(key), cancellationToken)
            .ConfigureAwait(false);

        if (!result.IsSuccess)
            return result.ToOkActionResult(HttpContext);

        return File(result.Value!.Content, result.Value.ContentType);
    }

    [HttpPost("resolve-read-urls")]
    [Authorize]
    public async Task<IActionResult> ResolveReadUrls(
        [FromBody] ResolveFileReadUrlsRequest request,
        CancellationToken cancellationToken)
    {
        var query = new ResolveFileReadUrlsQuery(request.Keys ?? []);
        return (await _sender.Send(query, cancellationToken).ConfigureAwait(false))
            .ToOkActionResult(HttpContext);
    }

    [HttpPost("presigned-url")]
    [Authorize]
    [EnableRateLimiting(ZimMarketRateLimitPolicies.PresignByUser)]
    public async Task<IActionResult> GetPresignedUploadUrl(
        [FromBody] GetPresignedUploadUrlRequest request,
        CancellationToken cancellationToken)
    {
        var query = new GetPresignedUploadUrlQuery(request.FileType, request.ContentType, request.FileSizeBytes);
        return (await _sender.Send(query, cancellationToken).ConfigureAwait(false))
            .ToOkActionResult(HttpContext);
    }

    [HttpGet("kyc-document/{*key}")]
    [Authorize(Policy = AuthorizationPolicies.Admin)]
    public async Task<IActionResult> GetKycDocumentUrl(string key, CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(key))
        {
            return Result<KycDocumentAccessDto>.ValidationFailure(
            [
                new ValidationError(nameof(key), "KYC document key is required.")
            ]).ToOkActionResult(HttpContext);
        }

        string normalizedKey = Uri.UnescapeDataString(key).Trim();
        if (!normalizedKey.StartsWith(KycDocumentsContainerPrefix, StringComparison.OrdinalIgnoreCase))
        {
            return Result<KycDocumentAccessDto>.Failure(
                "Files.InvalidKycKey",
                "Only keys under the kyc-documents container are supported by this endpoint.")
                .ToOkActionResult(HttpContext);
        }

        DateTimeOffset expiresAt = DateTimeOffset.UtcNow.Add(KycReadSasLifetime);
        try
        {
            string url = await _fileStorage
                .GenerateSasUrlAsync(normalizedKey, expiresAt, cancellationToken)
                .ConfigureAwait(false);

            _logger.LogInformation(
                "KYC document URL issued for key {Key} by admin {AdminSubject}. Expires at {ExpiresAtUtc}.",
                normalizedKey,
                User.FindFirst("sub")?.Value ?? "unknown",
                expiresAt);

            return Result<KycDocumentAccessDto>.Success(
                new KycDocumentAccessDto(normalizedKey, url, expiresAt))
                .ToOkActionResult(HttpContext);
        }
        catch (ArgumentException ex)
        {
            return Result<KycDocumentAccessDto>.ValidationFailure(
            [
                new ValidationError(nameof(key), ex.Message)
            ]).ToOkActionResult(HttpContext);
        }
    }

    [HttpPut("local-upload/{*key}")]
    [AllowAnonymous]
    public async Task<IActionResult> UploadLocalFile(
        string key,
        [FromQuery] long expires,
        [FromQuery] string signature,
        [FromQuery] string contentType,
        CancellationToken cancellationToken)
    {
        if (_localFileStorageAccess is null)
            return NotFound();

        string normalizedKey = Uri.UnescapeDataString(key).Trim();
        if (!TryCreateExpiry(expires, out DateTimeOffset expiresAt))
            return Unauthorized();

        string requestContentType = Request.ContentType ?? contentType;

        if (!_localFileStorageAccess.IsUploadRequestAuthorized(normalizedKey, requestContentType, expiresAt, signature))
            return Unauthorized();

        await _fileStorage
            .UploadAsync(Request.Body, normalizedKey, requestContentType, cancellationToken)
            .ConfigureAwait(false);

        return Ok();
    }

    [HttpGet("local-read/{*key}")]
    [AllowAnonymous]
    public async Task<IActionResult> ReadLocalFile(
        string key,
        [FromQuery] long expires,
        [FromQuery] string signature,
        CancellationToken cancellationToken)
    {
        if (_localFileStorageAccess is null)
            return NotFound();

        string normalizedKey = Uri.UnescapeDataString(key).Trim();
        if (!TryCreateExpiry(expires, out DateTimeOffset expiresAt))
            return Unauthorized();

        if (!_localFileStorageAccess.IsReadRequestAuthorized(normalizedKey, expiresAt, signature))
            return Unauthorized();

        try
        {
            Stream stream = await _localFileStorageAccess.OpenReadAsync(normalizedKey, cancellationToken)
                .ConfigureAwait(false);
            return File(stream, _localFileStorageAccess.GetContentType(normalizedKey));
        }
        catch (FileNotFoundException)
        {
            return NotFound();
        }
    }

    private static bool TryCreateExpiry(long unixTimeSeconds, out DateTimeOffset expiresAt)
    {
        try
        {
            expiresAt = DateTimeOffset.FromUnixTimeSeconds(unixTimeSeconds);
            return true;
        }
        catch (ArgumentOutOfRangeException)
        {
            expiresAt = default;
            return false;
        }
    }

    public sealed record GetPresignedUploadUrlRequest(FileType FileType, string ContentType, long FileSizeBytes);

    public sealed record ResolveFileReadUrlsRequest(IReadOnlyList<string> Keys);

    public sealed record KycDocumentAccessDto(string Key, string Url, DateTimeOffset ExpiresAt);
}
