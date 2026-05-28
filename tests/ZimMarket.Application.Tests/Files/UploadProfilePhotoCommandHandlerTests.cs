using System.Text;
using FluentAssertions;
using Microsoft.Extensions.Logging.Abstractions;
using NSubstitute;
using ZimMarket.Application.Common.Interfaces;
using ZimMarket.Application.Files;
using ZimMarket.Domain.Entities.Users;
using ZimMarket.Domain.Enums;
using ZimMarket.Domain.Interfaces;
using ZimMarket.Domain.Interfaces.Repositories;
using ZimMarket.Domain.ValueObjects;

namespace ZimMarket.Application.Tests.Files;

public sealed class UploadProfilePhotoCommandHandlerTests
{
    private static readonly PhoneNumber TestPhone = PhoneNumber.Create("+263771234567").Value!;

    [Fact]
    public async Task Handle_replaces_photo_and_deletes_previous_blob()
    {
        var sellerId = Guid.NewGuid();
        var seller = CreateSeller(sellerId);
        string previousKey = $"profile-photos/{sellerId:D}/old.jpg";
        seller.SetProfilePhotoKey(previousKey);

        var currentUser = Substitute.For<ICurrentUser>();
        currentUser.IsAuthenticated.Returns(true);
        currentUser.Role.Returns(UserRole.Seller);
        currentUser.UserId.Returns(sellerId);

        var userLogin = Substitute.For<IUserLoginRepository>();
        userLogin.GetTrackedByIdAsync(sellerId, Arg.Any<CancellationToken>()).Returns(seller);

        var unitOfWork = Substitute.For<IUnitOfWork>();
        var fileStorage = Substitute.For<IFileStorage>();

        fileStorage
            .UploadAsync(Arg.Any<Stream>(), Arg.Any<string>(), "image/png", Arg.Any<CancellationToken>())
            .Returns(call =>
            {
                string key = call.ArgAt<string>(1);
                return key;
            });

        var handler = new UploadProfilePhotoCommandHandler(
            currentUser,
            userLogin,
            unitOfWork,
            fileStorage,
            NullLogger<UploadProfilePhotoCommandHandler>.Instance);

        await using var stream = new MemoryStream(Encoding.UTF8.GetBytes("png-bytes"));
        var result = await handler.Handle(
            new UploadProfilePhotoCommand("image/png", stream, stream.Length),
            CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value.Should().StartWith($"profile-photos/{sellerId:D}/");
        seller.ProfilePhotoKey.Should().Be(result.Value);

        await fileStorage.Received(1).DeleteAsync(previousKey, Arg.Any<CancellationToken>());
        await unitOfWork.Received(1).SaveChangesAsync(Arg.Any<CancellationToken>());
    }

    private static Seller CreateSeller(Guid id) =>
        new(
            id,
            "seller@example.com",
            "Seller Name",
            TestPhone,
            "HASH",
            KycStatus.Approved,
            isActive: true,
            refreshTokenHash: null,
            refreshTokenExpiry: null,
            DateTimeOffset.UtcNow,
            DateTimeOffset.UtcNow,
            "Test Shop",
            "kyc/national",
            "kyc/proof",
            isApproved: true,
            rejectionReason: null);
}
