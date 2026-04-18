using System.Security.Claims;
using Hangfire.Dashboard;
using ZimMarket.Domain.Enums;

namespace ZimMarket.Infrastructure.BackgroundJobs;

/// <summary>
/// Restricts Hangfire dashboard access to authenticated platform administrators.
/// Matches JWT role claims issued by <see cref="ZimMarket.Infrastructure.Security.JwtService"/> (claim type <c>role</c>).
/// </summary>
public sealed class HangfireAuthFilter : IDashboardAuthorizationFilter
{
    internal const string RoleClaimType = "role";

    public bool Authorize(DashboardContext context)
    {
        ArgumentNullException.ThrowIfNull(context);

        ClaimsPrincipal user = context.GetHttpContext().User;
        if (user.Identity?.IsAuthenticated != true)
            return false;

        foreach (Claim claim in user.Claims)
        {
            if (claim.Type != RoleClaimType)
                continue;

            if (claim.Value == nameof(UserRole.Admin) || claim.Value == nameof(UserRole.SuperAdmin))
                return true;
        }

        return false;
    }
}
