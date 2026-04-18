using MediatR;
using Microsoft.Extensions.Logging;
using ZimMarket.Application.Common;
using ZimMarket.Application.Common.Interfaces;
using ZimMarket.Application.Common.Models;
using ZimMarket.Domain.Entities.Catalogue;
using ZimMarket.Domain.Entities.Orders;
using ZimMarket.Domain.Enums;
using ZimMarket.Domain.Interfaces;
using ZimMarket.Domain.ValueObjects;

namespace ZimMarket.Application.Orders;

public sealed class PlaceOrderCommandHandler : IRequestHandler<PlaceOrderCommand, Result<PlaceOrderResultDto>>
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly ICurrentUser _currentUser;
    private readonly IExchangeRateService _exchangeRateService;
    private readonly ILogger<PlaceOrderCommandHandler> _logger;

    public PlaceOrderCommandHandler(
        IUnitOfWork unitOfWork,
        ICurrentUser currentUser,
        IExchangeRateService exchangeRateService,
        ILogger<PlaceOrderCommandHandler> logger)
    {
        _unitOfWork = unitOfWork ?? throw new ArgumentNullException(nameof(unitOfWork));
        _currentUser = currentUser ?? throw new ArgumentNullException(nameof(currentUser));
        _exchangeRateService = exchangeRateService ?? throw new ArgumentNullException(nameof(exchangeRateService));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    public async Task<Result<PlaceOrderResultDto>> Handle(
        PlaceOrderCommand request,
        CancellationToken cancellationToken)
    {
        if (!_currentUser.IsAuthenticated || _currentUser.UserId == Guid.Empty || _currentUser.Role != UserRole.Customer)
        {
            _logger.LogDebug("Place order rejected: caller is not an authenticated customer.");
            return Result<PlaceOrderResultDto>.Failure(
                OrderErrorCodes.OrderForbidden,
                "Only authenticated customers can place orders.");
        }

        var normalizedItems = request.Items
            .GroupBy(x => x.ProductId)
            .Select(g => new PlaceOrderItemDto(g.Key, g.Sum(x => x.Quantity)))
            .ToList();

        var orderItems = new List<OrderItem>(normalizedItems.Count);
        decimal totalUsd = 0m;

        foreach (PlaceOrderItemDto item in normalizedItems)
        {
            Product? product = await _unitOfWork.Products.GetByIdAsync(item.ProductId, cancellationToken).ConfigureAwait(false);
            if (product is null)
            {
                return Result<PlaceOrderResultDto>.Failure(
                    OrderErrorCodes.ProductNotFound,
                    $"Product '{item.ProductId}' was not found.");
            }

            if (product.Status != ProductStatus.Active)
            {
                return Result<PlaceOrderResultDto>.Failure(
                    OrderErrorCodes.ProductInactive,
                    $"Product '{product.Id}' is not active.");
            }

            if (product.StockQuantity < item.Quantity)
            {
                return Result<PlaceOrderResultDto>.Failure(
                    OrderErrorCodes.ProductOutOfStock,
                    $"Product '{product.Id}' does not have enough stock.");
            }

            if (product.Price.Currency != Currency.USD)
            {
                return Result<PlaceOrderResultDto>.Failure(
                    OrderErrorCodes.ProductUnsupportedCurrency,
                    $"Product '{product.Id}' is not priced in USD.");
            }

            product.UpdateStock(-item.Quantity);
            await _unitOfWork.Products.UpdateAsync(product, cancellationToken).ConfigureAwait(false);

            var orderItemResult = OrderItem.Create(product.Id, product.Title, product.Price, item.Quantity);
            if (orderItemResult.IsFailure)
            {
                return Result<PlaceOrderResultDto>.Failure(OrderErrorCodes.OrderCreateFailed, string.Join("; ", orderItemResult.Errors));
            }

            OrderItem orderItem = orderItemResult.Value!;
            orderItems.Add(orderItem);
            totalUsd += orderItem.LineTotal.Amount;
        }

        var addressResult = Address.Create(
            request.DeliveryAddress.Street,
            request.DeliveryAddress.Suburb,
            request.DeliveryAddress.City,
            request.DeliveryAddress.Country);
        if (addressResult.IsFailure)
        {
            return Result<PlaceOrderResultDto>.Failure(
                OrderErrorCodes.OrderInvalidAddress,
                string.Join("; ", addressResult.Errors));
        }

        var totalAmountResult = Money.Create(
            decimal.Round(totalUsd, 2, MidpointRounding.AwayFromZero),
            Currency.USD);
        if (totalAmountResult.IsFailure)
        {
            return Result<PlaceOrderResultDto>.Failure(OrderErrorCodes.OrderCreateFailed, string.Join("; ", totalAmountResult.Errors));
        }

        var now = DateTimeOffset.UtcNow;
        var orderResult = Order.Create(
            id: Guid.NewGuid(),
            customerId: _currentUser.UserId,
            items: orderItems,
            deliveryAddress: addressResult.Value!,
            totalAmount: totalAmountResult.Value!,
            createdAt: now,
            updatedAt: now);
        if (orderResult.IsFailure)
        {
            return Result<PlaceOrderResultDto>.Failure(OrderErrorCodes.OrderCreateFailed, string.Join("; ", orderResult.Errors));
        }

        Order order = orderResult.Value!;

        await _unitOfWork.Orders.AddAsync(order, cancellationToken).ConfigureAwait(false);

        decimal usdToZwl = await _exchangeRateService.GetUsdToZwlAsync(cancellationToken).ConfigureAwait(false);
        decimal totalZwl = decimal.Round(totalUsd * usdToZwl, 2, MidpointRounding.AwayFromZero);

        return Result<PlaceOrderResultDto>.Success(
            new PlaceOrderResultDto(
                order.Id,
                totalUsd,
                totalZwl));
    }
}
