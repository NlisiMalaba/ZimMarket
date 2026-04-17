using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using ZimMarket.Application.Common.Interfaces;
using ZimMarket.Infrastructure.Configuration;
using ZimMarket.Infrastructure.Security;

namespace ZimMarket.Infrastructure;

public static class DependencyInjection
{
    public static IServiceCollection AddInfrastructure(this IServiceCollection services, IConfiguration configuration)
    {
        ArgumentNullException.ThrowIfNull(configuration);

        services.AddOptions<JwtOptions>()
            .Bind(configuration.GetSection(JwtOptions.SectionName))
            .ValidateDataAnnotations()
            .Validate(
                o => string.IsNullOrWhiteSpace(o.PrivateKeyPem) == string.IsNullOrWhiteSpace(o.PublicKeyPem),
                "Jwt:PrivateKeyPem and Jwt:PublicKeyPem must both be set or both be empty (empty disables token operations until configured).");

        services.AddSingleton<IJwtService, JwtService>();

        return services;
    }
}
