using ZimMarket.Domain.Enums;

namespace ZimMarket.Application.Auth;

/// <summary>ASP.NET Core authorization policy names (role and composite checks).</summary>
public static class AuthorizationPolicies
{
    public const string Customer = nameof(UserRole.Customer);

    public const string Seller = nameof(UserRole.Seller);

    public const string Driver = nameof(UserRole.Driver);

    public const string Admin = nameof(UserRole.Admin);

    public const string SuperAdmin = nameof(UserRole.SuperAdmin);

    /// <summary>Requires <see cref="UserRole.Admin"/> or <see cref="UserRole.SuperAdmin"/>.</summary>
    public const string AdminOrAbove = nameof(AdminOrAbove);

    /// <summary>Requires any authenticated user (SignalR hubs enforce finer rules per hub method).</summary>
    public const string TrackingHub = nameof(TrackingHub);

    /// <summary>Requires JWT <see cref="AuthClaimTypes.KycStatus"/> claim <c>Approved</c>.</summary>
    public const string KycApproved = "KycApproved";

    public const string SellerKycApproved = "SellerKycApproved";

    public const string DriverKycApproved = "DriverKycApproved";
}
