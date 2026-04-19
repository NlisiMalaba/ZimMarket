using ZimMarket.Application.Common.Interfaces;
using ZimMarket.Domain.Entities.Orders;
using ZimMarket.Domain.Enums;
using ZimMarket.Domain.Interfaces;

namespace ZimMarket.Infrastructure.RealTime;

public sealed class TrackingHubSubscriptionService : ITrackingHubSubscriptionService
{
    private readonly IUnitOfWork _unitOfWork;

    public TrackingHubSubscriptionService(IUnitOfWork unitOfWork)
    {
        _unitOfWork = unitOfWork ?? throw new ArgumentNullException(nameof(unitOfWork));
    }

    public async Task<bool> CanCustomerTrackOrderAsync(
        Guid customerUserId,
        Guid orderId,
        CancellationToken cancellationToken = default)
    {
        if (customerUserId == Guid.Empty || orderId == Guid.Empty)
            return false;

        Order? order = await _unitOfWork.Orders
            .GetByIdAsync(orderId, cancellationToken)
            .ConfigureAwait(false);

        return order is not null && order.CustomerId == customerUserId;
    }

    public bool CanAdminTrackDriverMap(UserRole role) =>
        role is UserRole.Admin or UserRole.SuperAdmin;
}
