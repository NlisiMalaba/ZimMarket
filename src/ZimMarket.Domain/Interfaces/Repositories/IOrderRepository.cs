using ZimMarket.Domain.Entities.Orders;
using ZimMarket.Domain.Enums;
using ZimMarket.Domain.ReadModels;
using ZimMarket.Shared;

namespace ZimMarket.Domain.Interfaces.Repositories;

public interface IOrderRepository
{
    Task<Order?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);

    Task<Order?> GetByIdWithItemsAsync(Guid id, CancellationToken cancellationToken = default);

    Task<Order?> GetByIdForUpdateAsync(Guid id, CancellationToken cancellationToken = default);

    Task<IReadOnlyList<Order>> GetByCustomerAsync(Guid customerId, CancellationToken cancellationToken = default);

    Task<PagedList<Order>> GetByCustomerPagedAsync(
        Guid customerId,
        PaginationParams pagination,
        OrderStatus? statusFilter,
        CancellationToken cancellationToken = default);

    Task<PagedList<Order>> GetBySellerPagedAsync(
        Guid sellerId,
        PaginationParams pagination,
        IReadOnlyList<OrderStatus>? statusFilters,
        CancellationToken cancellationToken = default);

    /// <summary>All orders for admin consoles, optionally filtered by status and creation date range (inclusive).</summary>
    Task<PagedList<OrderListAdminRow>> GetAllPagedForAdminAsync(
        OrderStatus? status,
        DateTimeOffset? dateFromInclusive,
        DateTimeOffset? dateToInclusive,
        PaginationParams pagination,
        CancellationToken cancellationToken = default);

    Task<IReadOnlyList<Order>> GetByStatusAsync(OrderStatus status, CancellationToken cancellationToken = default);

    Task AddAsync(Order order, CancellationToken cancellationToken = default);

    Task UpdateAsync(Order order, CancellationToken cancellationToken = default);
}
