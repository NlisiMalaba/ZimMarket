using ZimMarket.Domain.Enums;

namespace ZimMarket.Application.Sellers;

public sealed class SellerVerificationDto
{
    public required KycStatus KycStatus { get; init; }

    public string? RejectionReason { get; init; }
}
