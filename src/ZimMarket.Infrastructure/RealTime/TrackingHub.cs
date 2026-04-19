using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.SignalR;
using Microsoft.Extensions.Logging;
using ZimMarket.Application.Auth;
using ZimMarket.Application.Common;
using ZimMarket.Application.Common.Interfaces;
using ZimMarket.Domain.Enums;

namespace ZimMarket.Infrastructure.RealTime;

/// <summary>Real-time driver and order tracking. Authenticated clients join groups via hub methods.</summary>
[Authorize(Policy = AuthorizationPolicies.TrackingHub)]
public sealed class TrackingHub : Hub
{
    private readonly ITrackingHubSubscriptionService _subscriptionService;
    private readonly ILogger<TrackingHub> _logger;

    public TrackingHub(
        ITrackingHubSubscriptionService subscriptionService,
        ILogger<TrackingHub> logger)
    {
        _subscriptionService = subscriptionService ?? throw new ArgumentNullException(nameof(subscriptionService));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    /// <summary>Adds the connection to <c>order:{orderId}</c> when the caller is the order's customer.</summary>
    public async Task SubscribeToOrder(Guid orderId)
    {
        (Guid userId, UserRole role) = RequireAuthenticatedUser();

        if (role != UserRole.Customer)
        {
            _logger.LogDebug("SubscribeToOrder rejected: user {UserId} is not a customer.", userId);
            throw new HubException("Only customers can subscribe to order tracking.");
        }

        if (!await _subscriptionService
                .CanCustomerTrackOrderAsync(userId, orderId, Context.ConnectionAborted)
                .ConfigureAwait(false))
        {
            _logger.LogDebug(
                "SubscribeToOrder rejected: customer {UserId} cannot track order {OrderId}.",
                userId,
                orderId);
            throw new HubException("You cannot subscribe to this order.");
        }

        await Groups.AddToGroupAsync(Context.ConnectionId, TrackingRealtimeConstants.OrderGroupName(orderId))
            .ConfigureAwait(false);
    }

    /// <summary>Adds the connection to <c>admin:drivers</c> when the caller is an administrator.</summary>
    public async Task SubscribeToAdminMap()
    {
        (Guid userId, UserRole role) = RequireAuthenticatedUser();

        if (!_subscriptionService.CanAdminTrackDriverMap(role))
        {
            _logger.LogDebug("SubscribeToAdminMap rejected: user {UserId} is not an admin.", userId);
            throw new HubException("Only administrators can subscribe to the driver map.");
        }

        await Groups.AddToGroupAsync(Context.ConnectionId, TrackingRealtimeConstants.AdminDriversGroupName)
            .ConfigureAwait(false);
    }

    /// <summary>Removes the connection from <c>order:{orderId}</c> (e.g. after delivery or when leaving the page).</summary>
    public async Task UnsubscribeFromOrder(Guid orderId)
    {
        (Guid userId, UserRole role) = RequireAuthenticatedUser();

        if (role != UserRole.Customer)
        {
            _logger.LogDebug("UnsubscribeFromOrder rejected: user {UserId} is not a customer.", userId);
            throw new HubException("Only customers can unsubscribe from order tracking.");
        }

        if (!await _subscriptionService
                .CanCustomerTrackOrderAsync(userId, orderId, Context.ConnectionAborted)
                .ConfigureAwait(false))
        {
            _logger.LogDebug(
                "UnsubscribeFromOrder rejected: customer {UserId} cannot modify subscription for order {OrderId}.",
                userId,
                orderId);
            throw new HubException("You cannot unsubscribe from this order.");
        }

        await Groups.RemoveFromGroupAsync(Context.ConnectionId, TrackingRealtimeConstants.OrderGroupName(orderId))
            .ConfigureAwait(false);
    }

    private (Guid UserId, UserRole Role) RequireAuthenticatedUser()
    {
        ClaimsPrincipal user = Context.User ?? throw new HubException("Authentication is required.");

        Guid userId = ResolveUserId(user);
        if (userId == Guid.Empty)
            throw new HubException("Authentication is required.");

        UserRole role = ResolveRole(user);
        return (userId, role);
    }

    private static Guid ResolveUserId(ClaimsPrincipal user)
    {
        string? sub = user.FindFirst("sub")?.Value
            ?? user.FindFirst(ClaimTypes.NameIdentifier)?.Value;

        return Guid.TryParse(sub, out Guid id) ? id : Guid.Empty;
    }

    private static UserRole ResolveRole(ClaimsPrincipal user)
    {
        string? raw = user.FindFirst(AuthClaimTypes.Role)?.Value;
        return Enum.TryParse(raw, ignoreCase: true, out UserRole role) ? role : default;
    }
}
