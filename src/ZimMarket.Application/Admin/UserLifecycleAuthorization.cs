using ZimMarket.Domain.Enums;

namespace ZimMarket.Application.Admin;

internal static class UserLifecycleAuthorization
{
    /// <summary>
    /// <see cref="UserRole.Admin"/> may only manage marketplace users; <see cref="UserRole.SuperAdmin"/> may manage any role.
    /// </summary>
    public static bool CallerMayManageTarget(UserRole callerRole, UserRole targetRole) =>
        callerRole switch
        {
            UserRole.SuperAdmin => true,
            UserRole.Admin => targetRole is UserRole.Customer or UserRole.Seller or UserRole.Driver,
            _ => false
        };
}
