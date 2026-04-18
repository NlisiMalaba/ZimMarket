using FluentAssertions;
using Microsoft.Extensions.Logging.Abstractions;
using NSubstitute;
using ZimMarket.Application.Common.Interfaces;
using ZimMarket.Application.Files;

namespace ZimMarket.Application.Tests.Files;

public sealed class GetPresignedUploadUrlQueryTests
{
    [Fact]
    public async Task Handler_authenticated_user_returns_upload_url_file_key_and_expiry()
    {
        var currentUser = Substitute.For<ICurrentUser>();
        Guid userId = Guid.NewGuid();
        currentUser.IsAuthenticated.Returns(true);
        currentUser.UserId.Returns(userId);

        var fileStorage = Substitute.For<IFileStorage>();
        fileStorage.GetPresignedUploadUrlAsync(Arg.Any<string>(), "image/png", Arg.Any<CancellationToken>())
            .Returns("https://blob.example/upload-sas");

        var handler = new GetPresignedUploadUrlQueryHandler(
            currentUser,
            fileStorage,
            NullLogger<GetPresignedUploadUrlQueryHandler>.Instance);

        DateTimeOffset before = DateTimeOffset.UtcNow;
        var result = await handler.Handle(
            new GetPresignedUploadUrlQuery(FileType.ProductImage, "image/png", 1024),
            CancellationToken.None);
        DateTimeOffset after = DateTimeOffset.UtcNow;

        result.IsSuccess.Should().BeTrue();
        result.Value.Should().NotBeNull();
        result.Value!.UploadUrl.Should().Be("https://blob.example/upload-sas");
        result.Value.FileKey.Should().Match($"product-images/{userId:D}/*.png");
        result.Value.ExpiresAt.Should().BeOnOrAfter(before.AddMinutes(59));
        result.Value.ExpiresAt.Should().BeOnOrBefore(after.AddHours(1).AddMinutes(1));
    }

    [Fact]
    public async Task Handler_unauthenticated_user_returns_forbidden()
    {
        var currentUser = Substitute.For<ICurrentUser>();
        currentUser.IsAuthenticated.Returns(false);
        currentUser.UserId.Returns(Guid.Empty);

        var handler = new GetPresignedUploadUrlQueryHandler(
            currentUser,
            Substitute.For<IFileStorage>(),
            NullLogger<GetPresignedUploadUrlQueryHandler>.Instance);

        var result = await handler.Handle(
            new GetPresignedUploadUrlQuery(FileType.ProductImage, "image/jpeg", 1024),
            CancellationToken.None);

        result.IsSuccess.Should().BeFalse();
        result.ErrorCode.Should().Be("Files.Forbidden");
    }

    [Fact]
    public void Validator_rejects_unsupported_content_type_and_large_file()
    {
        var validator = new GetPresignedUploadUrlQueryValidator();

        var validation = validator.Validate(
            new GetPresignedUploadUrlQuery(FileType.DriverLicense, "application/pdf", 10 * 1024 * 1024));

        validation.IsValid.Should().BeFalse();
        validation.Errors.Should().Contain(e => e.PropertyName == nameof(GetPresignedUploadUrlQuery.ContentType));
        validation.Errors.Should().Contain(e => e.PropertyName == nameof(GetPresignedUploadUrlQuery.FileSizeBytes));
    }
}
