using ZimMarket.Application.Common.Abstractions;

namespace ZimMarket.Application.Drivers;

public sealed record ConfirmDeliveryCommand(Guid BatchId, Guid OrderId, string DeliveryPhotoKey) : ICommand;
