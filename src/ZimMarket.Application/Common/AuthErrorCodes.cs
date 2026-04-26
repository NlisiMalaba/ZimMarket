namespace ZimMarket.Application.Common;

/// <summary>Error codes for authentication flows (aligned with planning/DESIGN.md).</summary>
public static class AuthErrorCodes
{
    public const string UserAlreadyExists = "USER_ALREADY_EXISTS";

    public const string UserPhoneAlreadyExists = "USER_PHONE_ALREADY_EXISTS";

    public const string AuthInvalidCredentials = "AUTH_INVALID_CREDENTIALS";

    public const string AuthAccountLocked = "AUTH_ACCOUNT_LOCKED";

    public const string AuthRefreshInvalid = "AUTH_REFRESH_INVALID";

    public const string AuthInvalidAccessToken = "AUTH_INVALID_ACCESS_TOKEN";

    public const string AuthAccessTokenNotExpired = "AUTH_ACCESS_TOKEN_NOT_EXPIRED";

    public const string AuthEmailVerificationInvalid = "AUTH_EMAIL_VERIFICATION_INVALID";

    public const string AuthEmailVerificationRequired = "AUTH_EMAIL_VERIFICATION_REQUIRED";

    public const string AuthAdminApprovalPending = "AUTH_ADMIN_APPROVAL_PENDING";

    public const string AuthPasswordResetInvalid = "AUTH_PASSWORD_RESET_INVALID";
}
