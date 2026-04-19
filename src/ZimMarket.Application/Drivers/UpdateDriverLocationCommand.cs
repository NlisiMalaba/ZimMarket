using ZimMarket.Application.Common.Abstractions;

namespace ZimMarket.Application.Drivers;

public sealed record UpdateDriverLocationCommand(double Latitude, double Longitude) : ICommand;
