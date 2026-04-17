using ZimMarket.Domain.ValueObjects;

namespace ZimMarket.Domain.Entities.Users;

public sealed class CustomerDeliveryAddress
{
    private CustomerDeliveryAddress()
    {
        Address = null!;
    }

    public CustomerDeliveryAddress(Guid id, Address address)
    {
        ArgumentNullException.ThrowIfNull(address);
        Id = id;
        Address = address;
    }

    public Guid Id { get; private set; }

    public Address Address { get; private set; } = null!;
}
