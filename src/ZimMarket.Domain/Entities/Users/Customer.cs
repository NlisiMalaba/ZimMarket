using ZimMarket.Domain.Enums;
using ZimMarket.Domain.Exceptions;
using ZimMarket.Domain.ValueObjects;

namespace ZimMarket.Domain.Entities.Users;

public sealed class Customer : User
{
    public const int MaxDeliveryAddresses = 5;

    private readonly List<CustomerDeliveryAddress> _deliveryAddresses = [];

    private Customer()
    {
    }

    public Customer(
        Guid id,
        string email,
        string fullName,
        PhoneNumber phoneNumber,
        string passwordHash,
        KycStatus kycStatus,
        bool isActive,
        string? refreshTokenHash,
        DateTimeOffset? refreshTokenExpiry,
        DateTimeOffset createdAt,
        DateTimeOffset updatedAt,
        string? pushNotificationToken = null)
        : base(
            id,
            email,
            fullName,
            phoneNumber,
            passwordHash,
            UserRole.Customer,
            kycStatus,
            isActive,
            refreshTokenHash,
            refreshTokenExpiry,
            createdAt,
            updatedAt)
    {
        PushNotificationToken = pushNotificationToken;
    }

    public IReadOnlyList<CustomerDeliveryAddress> DeliveryAddresses => _deliveryAddresses;

    public string? PushNotificationToken { get; private set; }

    public void AddAddress(Address address)
    {
        ArgumentNullException.ThrowIfNull(address);

        if (_deliveryAddresses.Count >= MaxDeliveryAddresses)
            throw new DomainException($"A maximum of {MaxDeliveryAddresses} delivery addresses is allowed.");

        _deliveryAddresses.Add(new CustomerDeliveryAddress(Guid.NewGuid(), address));
        UpdatedAt = DateTimeOffset.UtcNow;
    }

    public void RemoveAddress(Guid deliveryAddressId)
    {
        var removed = _deliveryAddresses.RemoveAll(a => a.Id == deliveryAddressId);
        if (removed == 0)
            throw new DomainException("Delivery address not found.");

        UpdatedAt = DateTimeOffset.UtcNow;
    }

    public void UpdatePushToken(string pushNotificationToken)
    {
        PushNotificationToken = string.IsNullOrWhiteSpace(pushNotificationToken)
            ? null
            : pushNotificationToken.Trim();

        UpdatedAt = DateTimeOffset.UtcNow;
    }
}
