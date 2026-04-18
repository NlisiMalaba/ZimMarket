namespace ZimMarket.Application.Common.Models;

public sealed record PaymentInitiateDto
{
    public required string PaymentUrl { get; init; }

    public required string GatewayReference { get; init; }
}
