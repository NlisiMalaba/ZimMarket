using ZimMarket.Application.Common.Interfaces;
using ZimMarket.Domain.Enums;

namespace ZimMarket.Application.Common.Services;

public sealed class AnonymousCurrentUser : ICurrentUser
{
    public Guid UserId => Guid.Empty;

    public UserRole Role => default;

    public bool IsAuthenticated => false;

    public string? GetClaim(string claimType) => null;
}
