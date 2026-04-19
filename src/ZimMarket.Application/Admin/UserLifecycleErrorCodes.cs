namespace ZimMarket.Application.Admin;

public static class UserLifecycleErrorCodes
{
    public const string Forbidden = "Admin.UserLifecycle.Forbidden";

    public const string UserNotFound = "Admin.UserLifecycle.UserNotFound";

    public const string CannotActOnSelf = "Admin.UserLifecycle.CannotActOnSelf";

    public const string InsufficientPrivilegeForTarget = "Admin.UserLifecycle.InsufficientPrivilegeForTarget";
}
