namespace ZimMarket.Application.Common;

public static class AdminKycErrorCodes
{
    public const string Forbidden = "ADMIN_KYC_FORBIDDEN";

    public const string SasGenerationFailed = "ADMIN_KYC_SAS_FAILED";

    /// <summary>Domain rules blocked approval (e.g. KYC not in pending review).</summary>
    public const string CannotApprove = "ADMIN_KYC_CANNOT_APPROVE";

    /// <summary>Domain rules blocked rejection (e.g. KYC not in pending review).</summary>
    public const string CannotReject = "ADMIN_KYC_CANNOT_REJECT";
}
