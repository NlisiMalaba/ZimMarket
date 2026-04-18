using System.Security.Claims;
using Microsoft.AspNetCore.Http;
using ZimMarket.Application.Auth;
using ZimMarket.Application.Common.Interfaces;
using ZimMarket.Domain.Enums;

namespace ZimMarket.Application.Common.Services;

/// <summary>
/// Resolves the current caller from <see cref="HttpContext.User"/> (JWT bearer principal).
/// </summary>
public sealed class HttpContextCurrentUser : ICurrentUser
{
    private readonly IHttpContextAccessor _httpContextAccessor;

    public HttpContextCurrentUser(IHttpContextAccessor httpContextAccessor)
    {
        _httpContextAccessor = httpContextAccessor ?? throw new ArgumentNullException(nameof(httpContextAccessor));
    }

    private ClaimsPrincipal? Principal => _httpContextAccessor.HttpContext?.User;

    public bool IsAuthenticated => Principal?.Identity?.IsAuthenticated == true;

    public Guid UserId
    {
        get
        {
            // Matches JwtRegisteredClaimNames.Sub used when issuing tokens.
            string? sub = Principal?.FindFirst("sub")?.Value
                ?? Principal?.FindFirst(ClaimTypes.NameIdentifier)?.Value;

            return Guid.TryParse(sub, out Guid id) ? id : Guid.Empty;
        }
    }

    public UserRole Role
    {
        get
        {
            string? raw = Principal?.FindFirst(AuthClaimTypes.Role)?.Value;
            return Enum.TryParse(raw, ignoreCase: true, out UserRole role) ? role : default;
        }
    }

    public string? GetClaim(string claimType) =>
        Principal?.FindFirst(claimType)?.Value;
}
