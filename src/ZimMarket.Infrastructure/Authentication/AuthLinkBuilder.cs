using Microsoft.Extensions.Configuration;
using ZimMarket.Application.Common.Interfaces;

namespace ZimMarket.Infrastructure.Authentication;

public sealed class AuthLinkBuilder : IAuthLinkBuilder
{
    private readonly string _adminPanelOrigin;

    public AuthLinkBuilder(IConfiguration configuration)
    {
        ArgumentNullException.ThrowIfNull(configuration);
        _adminPanelOrigin = configuration["Cors:AdminPanelOrigin"]?.TrimEnd('/') ?? "http://localhost:5173";
    }

    public string BuildAdminEmailVerificationLink(string token) =>
        $"{_adminPanelOrigin}/auth/verify-email?token={Uri.EscapeDataString(token)}";

    public string BuildResetPasswordLink(string token) =>
        $"{_adminPanelOrigin}/auth/reset-password?token={Uri.EscapeDataString(token)}";
}
