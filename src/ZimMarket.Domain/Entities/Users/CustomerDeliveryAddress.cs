using ZimMarket.Domain.ValueObjects;

namespace ZimMarket.Domain.Entities.Users;

public sealed record CustomerDeliveryAddress(Guid Id, Address Address);
