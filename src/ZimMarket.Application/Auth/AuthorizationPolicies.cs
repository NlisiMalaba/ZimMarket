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

    /// <summary>Requires JWT <see cref="AuthClaimTypes.KycStatus"/> claim <c>Approved</c>.</summary>
    public const string KycApproved = "KycApproved";

    public const string SellerKycApproved = "SellerKycApproved";

    public const string DriverKycApproved = "DriverKycApproved";
}
