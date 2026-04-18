using ZimMarket.Application.Common.Abstractions;

namespace ZimMarket.Application.Auth;

public sealed record SubmitDriverKycCommand(
    string LicenseDocKey,
    string VehicleDocKey,
    string LicenseNumber,
    string VehicleRegistration) : ICommand;
