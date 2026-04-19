using System.Security.Cryptography;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.IdentityModel.Tokens;
using ZimMarket.Application.Auth;
using ZimMarket.Domain.Enums;
using ZimMarket.Infrastructure.Configuration;

namespace ZimMarket.Infrastructure.Authentication;

public static class WebAuthenticationServiceCollectionExtensions
{
    /// <summary>
    /// Registers role- and KYC-based authorization policies (claim names match <see cref="ZimMarket.Infrastructure.Security.JwtService"/>).
    /// </summary>
    public static IServiceCollection AddZimMarketAuthorizationPolicies(this IServiceCollection services)
    {
        services.AddAuthorization(options =>
        {
            options.AddPolicy(
                AuthorizationPolicies.Customer,
                p => p.RequireAuthenticatedUser().RequireClaim(AuthClaimTypes.Role, UserRole.Customer.ToString()));

            options.AddPolicy(
                AuthorizationPolicies.Seller,
                p => p.RequireAuthenticatedUser().RequireClaim(AuthClaimTypes.Role, UserRole.Seller.ToString()));

            options.AddPolicy(
                AuthorizationPolicies.Driver,
                p => p.RequireAuthenticatedUser().RequireClaim(AuthClaimTypes.Role, UserRole.Driver.ToString()));

            options.AddPolicy(
                AuthorizationPolicies.Admin,
                p => p.RequireAuthenticatedUser().RequireClaim(AuthClaimTypes.Role, UserRole.Admin.ToString()));

            options.AddPolicy(
                AuthorizationPolicies.SuperAdmin,
                p => p.RequireAuthenticatedUser().RequireClaim(AuthClaimTypes.Role, UserRole.SuperAdmin.ToString()));

            options.AddPolicy(
                AuthorizationPolicies.AdminOrAbove,
                p =>
                {
                    p.RequireAuthenticatedUser();
                    p.RequireAssertion(ctx =>
                    {
                        string? role = ctx.User.FindFirst(AuthClaimTypes.Role)?.Value;
                        return role == UserRole.Admin.ToString() || role == UserRole.SuperAdmin.ToString();
                    });
                });

            options.AddPolicy(
                AuthorizationPolicies.KycApproved,
                p => p.RequireAuthenticatedUser()
                    .RequireClaim(AuthClaimTypes.KycStatus, KycStatus.Approved.ToString()));

            options.AddPolicy(
                AuthorizationPolicies.SellerKycApproved,
                p =>
                {
                    p.RequireAuthenticatedUser();
                    p.RequireClaim(AuthClaimTypes.Role, UserRole.Seller.ToString());
                    p.RequireClaim(AuthClaimTypes.KycStatus, KycStatus.Approved.ToString());
                });

            options.AddPolicy(
                AuthorizationPolicies.DriverKycApproved,
                p =>
                {
                    p.RequireAuthenticatedUser();
                    p.RequireClaim(AuthClaimTypes.Role, UserRole.Driver.ToString());
                    p.RequireClaim(AuthClaimTypes.KycStatus, KycStatus.Approved.ToString());
                });
        });

        return services;
    }

    /// <summary>
    /// Registers RS256 JWT bearer authentication (same issuer, audience, algorithms, and claim types as <see cref="ZimMarket.Infrastructure.Security.JwtService"/>).
    /// </summary>
    /// <exception cref="InvalidOperationException">When <see cref="JwtOptions.PublicKeyPem"/> is missing or invalid.</exception>
    public static IServiceCollection AddZimMarketJwtBearer(this IServiceCollection services, IConfiguration configuration)
    {
        JwtOptions jwt = configuration.GetSection(JwtOptions.SectionName).Get<JwtOptions>() ?? new JwtOptions();

        if (string.IsNullOrWhiteSpace(jwt.PublicKeyPem))
        {
            throw new InvalidOperationException(
                "JWT bearer authentication requires Jwt:PublicKeyPem (RSA public key PEM). " +
                "Set Jwt:PrivateKeyPem and Jwt:PublicKeyPem so access tokens can be validated.");
        }

        using RSA rsa = RSA.Create();
        rsa.ImportFromPem(jwt.PublicKeyPem);
        // Copy parameters so the key is not tied to `rsa`'s lifetime (a `using`-disposed RSA would break validation).
        var signingKey = new RsaSecurityKey(rsa.ExportParameters(false));

        services.AddAuthentication(options =>
            {
                options.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
                options.DefaultChallengeScheme = JwtBearerDefaults.AuthenticationScheme;
            })
            .AddJwtBearer(options =>
            {
                options.MapInboundClaims = false;
                options.TokenValidationParameters = new TokenValidationParameters
                {
                    ValidateIssuerSigningKey = true,
                    IssuerSigningKey = signingKey,
                    ValidateIssuer = true,
                    ValidIssuer = jwt.Issuer,
                    ValidateAudience = true,
                    ValidAudience = jwt.Audience,
                    ValidateLifetime = true,
                    ClockSkew = TimeSpan.FromMinutes(1),
                    ValidAlgorithms = [SecurityAlgorithms.RsaSha256],
                    NameClaimType = "sub",
                    RoleClaimType = AuthClaimTypes.Role
                };
            });

        return services;
    }

    /// <summary>
    /// Registers authorization policies and JWT bearer when <see cref="JwtOptions.PublicKeyPem"/> is configured; otherwise only policies and a no-op authentication builder.
    /// </summary>
    public static IServiceCollection AddZimMarketWebAuthenticationAndAuthorization(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        services.AddZimMarketAuthorizationPolicies();

        string? publicKeyPem = configuration["Jwt:PublicKeyPem"];
        if (string.IsNullOrWhiteSpace(publicKeyPem))
        {
            services.AddAuthentication();
            return services;
        }

        return services.AddZimMarketJwtBearer(configuration);
    }
}
