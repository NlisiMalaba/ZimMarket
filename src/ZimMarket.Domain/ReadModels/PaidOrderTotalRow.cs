using ZimMarket.Domain.Enums;

namespace ZimMarket.Domain.ReadModels;

/// <summary>Total amount for a paid order row used when aggregating revenue.</summary>
public readonly record struct PaidOrderTotalRow(decimal Amount, Currency Currency);
