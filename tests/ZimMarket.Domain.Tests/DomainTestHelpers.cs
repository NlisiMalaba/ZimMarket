using ZimMarket.Domain.Entities.Users;
using ZimMarket.Domain.Enums;
using ZimMarket.Domain.ValueObjects;

namespace ZimMarket.Domain.Tests;

internal static class DomainTestHelpers
{
    public static Address ValidAddress =>
        Address.Create("12 Market Road", "Avondale", "Harare", "Zimbabwe").Value!;

    public static PhoneNumber ValidPhone =>
        PhoneNumber.Create("+263771234567").Value!;

    public static Money TenUsd =>
        Money.Create(10.00m, Currency.USD).Value!;

    public static Seller NewSeller(KycStatus kyc = KycStatus.NotSubmitted) =>
        new(
            Guid.NewGuid(),
            "seller@test.com",
            ValidPhone,
            "hash",
            kyc,
            isActive: true,
            refreshTokenHash: null,
            refreshTokenExpiry: null,
            createdAt: DateTimeOffset.UtcNow,
            updatedAt: DateTimeOffset.UtcNow,
            businessName: "Biz",
            nationalIdDocumentKey: "",
            proofOfResidenceDocumentKey: "",
            isApproved: false,
            rejectionReason: null);
}
