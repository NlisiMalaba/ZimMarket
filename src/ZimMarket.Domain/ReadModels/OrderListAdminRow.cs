using ZimMarket.Domain.Enums;

namespace ZimMarket.Domain.ReadModels;

/// <summary>Summary row for admin order management lists.</summary>
public sealed record OrderListAdminRow(
    Guid OrderId,
    Guid CustomerId,
    OrderStatus Status,
    PaymentStatus PaymentStatus,
    decimal TotalAmount,
    Currency TotalCurrency,
    int LineItemCount,
    DateTimeOffset CreatedAt);
