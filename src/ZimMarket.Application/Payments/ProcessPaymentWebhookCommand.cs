using ZimMarket.Application.Common.Abstractions;
using ZimMarket.Domain.Enums;

namespace ZimMarket.Application.Payments;

public sealed record ProcessPaymentWebhookCommand(
    string Payload,
    string? Signature,
    PaymentGatewayType GatewayType) : ICommand;
