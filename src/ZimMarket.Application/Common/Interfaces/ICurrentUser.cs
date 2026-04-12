using ZimMarket.Domain.Enums;

namespace ZimMarket.Application.Common.Interfaces;

public interface ICurrentUser
{
    Guid UserId { get; }

    UserRole Role { get; }

    bool IsAuthenticated { get; }

    string? GetClaim(string claimType);
}
