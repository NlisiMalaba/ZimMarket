using ZimMarket.Domain.Enums;
using ZimMarket.Domain.Exceptions;
using FluentAssertions;

namespace ZimMarket.Domain.Tests;

public class SellerKycTests
{
    [Fact]
    public void SubmitKyc_only_allowed_from_NotSubmitted()
    {
        var seller = DomainTestHelpers.NewSeller(KycStatus.NotSubmitted);
        seller.SubmitKyc("id-key", "proof-key");
        seller.KycStatus.Should().Be(KycStatus.PendingReview);

        var act = () => seller.SubmitKyc("x", "y");
        act.Should().Throw<DomainException>();
    }

    [Fact]
    public void Approve_only_allowed_from_PendingReview()
    {
        var pending = DomainTestHelpers.NewSeller(KycStatus.PendingReview);
        pending.Approve();
        pending.KycStatus.Should().Be(KycStatus.Approved);

        var notSubmitted = DomainTestHelpers.NewSeller(KycStatus.NotSubmitted);
        var act = () => notSubmitted.Approve();
        act.Should().Throw<DomainException>();
    }
}
