namespace ZimMarket.Application.Admin;

public static class CreateAdminErrorCodes
{
    public const string Forbidden = "SuperAdmin.AdminCreate.Forbidden";

    /// <summary>No unused synthetic phone could be allocated after several attempts (extremely rare).</summary>
    public const string PhoneAllocationFailed = "SuperAdmin.AdminCreate.PhoneAllocationFailed";
}
