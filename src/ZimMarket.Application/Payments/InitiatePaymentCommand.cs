using ZimMarket.Application.Common.Abstractions;
using ZimMarket.Application.Common.Models;
using ZimMarket.Domain.Enums;

namespace ZimMarket.Application.Payments;

public sealed record InitiatePaymentCommand(
    Guid OrderId,
    PaymentMethod PaymentMethod,
    string IdempotencyKey) : ICommand<PaymentInitiateDto>;
