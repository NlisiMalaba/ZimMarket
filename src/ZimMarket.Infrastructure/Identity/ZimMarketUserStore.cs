using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using ZimMarket.Domain.Entities.Users;
using ZimMarket.Infrastructure.Persistence;

namespace ZimMarket.Infrastructure.Identity;

/// <summary>
/// ASP.NET Core Identity user store backed by the shared <c>users</c> table (TPH) in <see cref="AppDbContext"/>.
/// Registration flows use application commands; this store supports lookup and password updates.
/// </summary>
public sealed class ZimMarketUserStore :
    IUserStore<IdentityUser<Guid>>,
    IUserPasswordStore<IdentityUser<Guid>>,
    IUserEmailStore<IdentityUser<Guid>>,
    IUserSecurityStampStore<IdentityUser<Guid>>,
    IUserTwoFactorStore<IdentityUser<Guid>>
{
    public const string SecurityStampShadowProperty = "SecurityStamp";

    private readonly AppDbContext _dbContext;

    public ZimMarketUserStore(AppDbContext dbContext)
    {
        _dbContext = dbContext ?? throw new ArgumentNullException(nameof(dbContext));
    }

    public void Dispose()
    {
    }

    public Task<IdentityResult> CreateAsync(IdentityUser<Guid> user, CancellationToken cancellationToken) =>
        Task.FromResult(IdentityResult.Failed(new IdentityError
        {
            Code = "NotSupported",
            Description =
                "User creation is handled by ZimMarket registration commands (customer, seller, driver), not Identity UserManager.CreateAsync."
        }));

    public Task<IdentityResult> DeleteAsync(IdentityUser<Guid> user, CancellationToken cancellationToken) =>
        Task.FromResult(IdentityResult.Failed(new IdentityError
        {
            Code = "NotSupported",
            Description = "User deletion is not supported through the Identity user store."
        }));

    public async Task<IdentityUser<Guid>?> FindByIdAsync(string userId, CancellationToken cancellationToken)
    {
        if (!Guid.TryParse(userId, out Guid id))
            return null;

        return await FindProjectedAsync(id, cancellationToken).ConfigureAwait(false);
    }

    public Task<IdentityUser<Guid>?> FindByNameAsync(string normalizedUserName, CancellationToken cancellationToken) =>
        FindByEmailAsync(normalizedUserName, cancellationToken);

    public Task<string?> GetNormalizedUserNameAsync(IdentityUser<Guid> user, CancellationToken cancellationToken) =>
        Task.FromResult(user.NormalizedUserName);

    public Task<string> GetUserIdAsync(IdentityUser<Guid> user, CancellationToken cancellationToken) =>
        Task.FromResult(user.Id.ToString("D"));

    public Task<string?> GetUserNameAsync(IdentityUser<Guid> user, CancellationToken cancellationToken) =>
        Task.FromResult(user.UserName);

    public Task SetNormalizedUserNameAsync(
        IdentityUser<Guid> user,
        string? normalizedName,
        CancellationToken cancellationToken)
    {
        user.NormalizedUserName = normalizedName;
        return Task.CompletedTask;
    }

    public Task SetUserNameAsync(IdentityUser<Guid> user, string? userName, CancellationToken cancellationToken)
    {
        user.UserName = userName;
        return Task.CompletedTask;
    }

    public async Task<IdentityResult> UpdateAsync(IdentityUser<Guid> user, CancellationToken cancellationToken)
    {
        User? entity = await _dbContext.Set<User>()
            .FirstOrDefaultAsync(u => u.Id == user.Id, cancellationToken)
            .ConfigureAwait(false);

        if (entity is null)
        {
            return IdentityResult.Failed(new IdentityError
            {
                Code = "UserMissing",
                Description = $"No user with id {user.Id} exists."
            });
        }

        Microsoft.EntityFrameworkCore.ChangeTracking.EntityEntry<User> entry = _dbContext.Entry(entity);

        if (!string.IsNullOrEmpty(user.PasswordHash))
            entry.Property<string>(nameof(User.PasswordHash)).CurrentValue = user.PasswordHash;

        if (!string.IsNullOrEmpty(user.Email))
            entry.Property<string>(nameof(User.Email)).CurrentValue = user.Email;

        entry.Property<string>(SecurityStampShadowProperty).CurrentValue =
            user.SecurityStamp ?? Guid.NewGuid().ToString("N");

        await _dbContext.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
        return IdentityResult.Success;
    }

    public Task SetPasswordHashAsync(IdentityUser<Guid> user, string? passwordHash, CancellationToken cancellationToken)
    {
        user.PasswordHash = passwordHash;
        return Task.CompletedTask;
    }

    public Task<string?> GetPasswordHashAsync(IdentityUser<Guid> user, CancellationToken cancellationToken) =>
        Task.FromResult(user.PasswordHash);

    public Task<bool> HasPasswordAsync(IdentityUser<Guid> user, CancellationToken cancellationToken) =>
        Task.FromResult(!string.IsNullOrEmpty(user.PasswordHash));

    public Task SetEmailAsync(IdentityUser<Guid> user, string? email, CancellationToken cancellationToken)
    {
        user.Email = email;
        return Task.CompletedTask;
    }

    public Task<string?> GetEmailAsync(IdentityUser<Guid> user, CancellationToken cancellationToken) =>
        Task.FromResult(user.Email);

    public Task<bool> GetEmailConfirmedAsync(IdentityUser<Guid> user, CancellationToken cancellationToken) =>
        Task.FromResult(true);

    public Task SetEmailConfirmedAsync(IdentityUser<Guid> user, bool confirmed, CancellationToken cancellationToken) =>
        Task.CompletedTask;

    public async Task<IdentityUser<Guid>?> FindByEmailAsync(string normalizedEmail, CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(normalizedEmail))
            return null;

        string normalized = normalizedEmail.Trim().ToUpperInvariant();

        var row = await _dbContext.Set<User>()
            .Where(u => u.Email.ToUpper() == normalized)
            .Select(u => new
            {
                u.Id,
                u.Email,
                u.PasswordHash,
                Stamp = EF.Property<string?>(u, SecurityStampShadowProperty)
            })
            .FirstOrDefaultAsync(cancellationToken)
            .ConfigureAwait(false);

        return row is null ? null : Map(row.Id, row.Email, row.PasswordHash, row.Stamp);
    }

    public Task<string?> GetNormalizedEmailAsync(IdentityUser<Guid> user, CancellationToken cancellationToken) =>
        Task.FromResult(user.NormalizedEmail);

    public Task SetNormalizedEmailAsync(IdentityUser<Guid> user, string? normalizedEmail, CancellationToken cancellationToken)
    {
        user.NormalizedEmail = normalizedEmail;
        return Task.CompletedTask;
    }

    public Task SetSecurityStampAsync(IdentityUser<Guid> user, string stamp, CancellationToken cancellationToken)
    {
        user.SecurityStamp = stamp;
        return Task.CompletedTask;
    }

    public Task<string?> GetSecurityStampAsync(IdentityUser<Guid> user, CancellationToken cancellationToken) =>
        Task.FromResult(user.SecurityStamp);

    public Task SetTwoFactorEnabledAsync(IdentityUser<Guid> user, bool enabled, CancellationToken cancellationToken) =>
        throw new NotSupportedException("Two-factor authentication is not enabled for ZimMarket users.");

    public Task<bool> GetTwoFactorEnabledAsync(IdentityUser<Guid> user, CancellationToken cancellationToken) =>
        Task.FromResult(false);

    private async Task<IdentityUser<Guid>?> FindProjectedAsync(Guid id, CancellationToken cancellationToken)
    {
        var row = await _dbContext.Set<User>()
            .Where(u => u.Id == id)
            .Select(u => new
            {
                u.Id,
                u.Email,
                u.PasswordHash,
                Stamp = EF.Property<string?>(u, SecurityStampShadowProperty)
            })
            .FirstOrDefaultAsync(cancellationToken)
            .ConfigureAwait(false);

        return row is null ? null : Map(row.Id, row.Email, row.PasswordHash, row.Stamp);
    }

    private static IdentityUser<Guid> Map(Guid id, string email, string passwordHash, string? securityStamp)
    {
        string stamp = string.IsNullOrWhiteSpace(securityStamp)
            ? Guid.NewGuid().ToString("N")
            : securityStamp;

        return new IdentityUser<Guid>
        {
            Id = id,
            UserName = email,
            NormalizedUserName = email.ToUpperInvariant(),
            Email = email,
            NormalizedEmail = email.ToUpperInvariant(),
            EmailConfirmed = true,
            PasswordHash = passwordHash,
            SecurityStamp = stamp,
            LockoutEnabled = false
        };
    }
}
