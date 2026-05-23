using MediatR;
using ZimMarket.Application.Common;
using ZimMarket.Application.Common.Interfaces;
using ZimMarket.Application.Common.Models;
using ZimMarket.Domain.Entities.Orders;
using ZimMarket.Domain.Entities.Users;
using ZimMarket.Domain.Enums;
using ZimMarket.Domain.Interfaces;

namespace ZimMarket.Application.Orders;

public sealed class GetSellerOrdersQueryHandler
    : IRequestHandler<GetSellerOrdersQuery, Result<ZimMarket.Shared.PagedList<SellerOrderListItemDto>>>
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly ICurrentUser _currentUser;

    public GetSellerOrdersQueryHandler(
        IUnitOfWork unitOfWork,
        ICurrentUser currentUser)
    {
        _unitOfWork = unitOfWork ?? throw new ArgumentNullException(nameof(unitOfWork));
        _currentUser = currentUser ?? throw new ArgumentNullException(nameof(currentUser));
    }

    public async Task<Result<ZimMarket.Shared.PagedList<SellerOrderListItemDto>>> Handle(
        GetSellerOrdersQuery request,
        CancellationToken cancellationToken)
    {
        if (!_currentUser.IsAuthenticated || _currentUser.UserId == Guid.Empty || _currentUser.Role != UserRole.Seller)
        {
            return Result<ZimMarket.Shared.PagedList<SellerOrderListItemDto>>.Failure(
                OrderErrorCodes.OrderForbidden,
                "Only authenticated sellers can view seller orders.");
        }

        var pagination = new ZimMarket.Shared.PaginationParams
        {
            Page = request.Page,
            PageSize = request.PageSize
        };

        IReadOnlyList<OrderStatus>? statusFilters = request.StatusGroup.HasValue
            ? SellerOrderStatusGroups.ResolveStatuses(request.StatusGroup)
            : request.StatusFilter.HasValue
                ? [request.StatusFilter.Value]
                : null;

        ZimMarket.Shared.PagedList<Order> orders = await _unitOfWork.Orders
            .GetBySellerPagedAsync(_currentUser.UserId, pagination, statusFilters, cancellationToken)
            .ConfigureAwait(false);

        var sellerProducts = await _unitOfWork.Products
            .FindBySellerAsync(_currentUser.UserId, cancellationToken)
            .ConfigureAwait(false);

        var sellerProductIds = sellerProducts.Select(p => p.Id).ToHashSet();
        var customerCache = new Dictionary<Guid, Customer?>();
        var items = new List<SellerOrderListItemDto>(orders.Items.Count);

        foreach (Order order in orders.Items)
        {
            List<OrderItem> sellerItems = order.Items
                .Where(item => sellerProductIds.Contains(item.ProductId))
                .ToList();

            if (sellerItems.Count == 0)
                continue;

            Customer? customer = await GetCustomerAsync(order.CustomerId, customerCache, cancellationToken)
                .ConfigureAwait(false);

            string primaryProductTitle = BuildPrimaryProductTitle(sellerItems);
            decimal sellerTotalUsd = sellerItems.Sum(item => item.LineTotal.Amount);

            items.Add(new SellerOrderListItemDto(
                order.Id,
                order.Status,
                order.PaymentStatus,
                order.TotalAmount.Amount,
                sellerTotalUsd,
                sellerItems.Count,
                order.CreatedAt,
                customer?.FullName ?? "Unknown customer",
                customer?.Email ?? string.Empty,
                primaryProductTitle));
        }

        return Result<ZimMarket.Shared.PagedList<SellerOrderListItemDto>>.Success(
            new ZimMarket.Shared.PagedList<SellerOrderListItemDto>(
                items,
                orders.Page,
                orders.PageSize,
                orders.TotalCount));
    }

    private async Task<Customer?> GetCustomerAsync(
        Guid customerId,
        Dictionary<Guid, Customer?> cache,
        CancellationToken cancellationToken)
    {
        if (cache.TryGetValue(customerId, out Customer? cached))
            return cached;

        Customer? customer = await _unitOfWork.Customers
            .GetByIdAsync(customerId, cancellationToken)
            .ConfigureAwait(false);

        cache[customerId] = customer;
        return customer;
    }

    private static string BuildPrimaryProductTitle(IReadOnlyList<OrderItem> sellerItems)
    {
        string primaryTitle = sellerItems[0].ProductTitle;

        if (sellerItems.Count > 1)
            return $"{primaryTitle} +{sellerItems.Count - 1} more";

        return primaryTitle;
    }
}
