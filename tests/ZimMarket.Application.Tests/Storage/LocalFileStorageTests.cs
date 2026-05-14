using System.Text;
using FluentAssertions;
using Microsoft.Extensions.FileProviders;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using NSubstitute;
using ZimMarket.Infrastructure.Configuration;
using ZimMarket.Infrastructure.Storage;

namespace ZimMarket.Application.Tests.Storage;

public sealed class LocalFileStorageTests : IDisposable
{
    private readonly string _tempRoot = Path.Combine(Path.GetTempPath(), $"zimmarket-storage-{Guid.NewGuid():N}");

    [Fact]
    public async Task Upload_and_read_roundtrip_uses_local_filesystem()
    {
        LocalFileStorage storage = CreateStorage();
        const string key = "product-images/seller/image-1.png";
        await using var input = new MemoryStream(Encoding.UTF8.GetBytes("image-bytes"));

        await storage.UploadAsync(input, key, "image/png", CancellationToken.None);

        bool exists = await storage.ExistsAsync(key, CancellationToken.None);
        string readUrl = await storage.GenerateSasUrlAsync(key, DateTimeOffset.UtcNow.AddMinutes(5), CancellationToken.None);
        await using Stream output = await storage.OpenReadAsync(key, CancellationToken.None);
        using var reader = new StreamReader(output);

        exists.Should().BeTrue();
        readUrl.Should().Contain("/api/v1/files/local-read/product-images/seller/image-1.png");
        storage.GetContentType(key).Should().Be("image/png");
        (await reader.ReadToEndAsync()).Should().Be("image-bytes");
    }

    [Fact]
    public async Task Presigned_upload_url_authorizes_matching_request_only()
    {
        LocalFileStorage storage = CreateStorage();
        const string key = "product-images/seller/image-1.jpg";

        string uploadUrl = await storage.GetPresignedUploadUrlAsync(key, "image/jpeg", CancellationToken.None);
        var uri = new Uri(uploadUrl);
        Dictionary<string, string> query = ParseQuery(uri.Query);
        DateTimeOffset expiresAt = DateTimeOffset.FromUnixTimeSeconds(long.Parse(query["expires"]));

        storage.IsUploadRequestAuthorized(key, "image/jpeg", expiresAt, query["signature"]).Should().BeTrue();
        storage.IsUploadRequestAuthorized(key, "image/png", expiresAt, query["signature"]).Should().BeFalse();
    }

    public void Dispose()
    {
        if (Directory.Exists(_tempRoot))
            Directory.Delete(_tempRoot, recursive: true);
    }

    private LocalFileStorage CreateStorage()
    {
        var environment = Substitute.For<IHostEnvironment>();
        environment.ContentRootPath.Returns(_tempRoot);
        environment.ContentRootFileProvider.Returns(Substitute.For<IFileProvider>());
        environment.EnvironmentName.Returns(Environments.Development);
        environment.ApplicationName.Returns("ZimMarket.Tests");

        var options = Options.Create(new LocalFileStorageOptions
        {
            RootPath = "files",
            PublicBaseUrl = "http://localhost:8080",
            SigningKey = "local-test-signing-key"
        });

        return new LocalFileStorage(options, environment, NullLogger<LocalFileStorage>.Instance);
    }

    private static Dictionary<string, string> ParseQuery(string query)
    {
        return query
            .TrimStart('?')
            .Split('&', StringSplitOptions.RemoveEmptyEntries)
            .Select(part => part.Split('=', 2))
            .ToDictionary(
                parts => Uri.UnescapeDataString(parts[0]),
                parts => Uri.UnescapeDataString(parts[1]));
    }
}
