using ZimMarket.Application.Common.Abstractions;
using ZimMarket.Domain.Enums;

namespace ZimMarket.Application.Warehouse;

public sealed record UpdateQcStatusCommand(Guid WarehouseItemId, WarehouseQcStatus QcStatus, string? Notes) : ICommand;
