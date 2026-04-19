using Microsoft.AspNetCore.SignalR;

namespace ZimMarket.Infrastructure.RealTime;

/// <summary>Real-time driver and order tracking. Clients join groups named <c>order:</c> plus order id, or <c>admin:drivers</c>.</summary>
public sealed class TrackingHub : Hub
{
}
