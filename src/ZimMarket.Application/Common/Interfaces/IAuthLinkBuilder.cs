namespace ZimMarket.Application.Common.Interfaces;

public interface IAuthLinkBuilder
{
    string BuildAdminEmailVerificationLink(string token);

    string BuildResetPasswordLink(string token);
}
