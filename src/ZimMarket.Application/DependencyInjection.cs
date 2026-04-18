using System.Reflection;
using FluentValidation;
using MediatR;
using Microsoft.Extensions.DependencyInjection;
using ZimMarket.Application.Common.Behaviours;
using ZimMarket.Application.Common.Interfaces;
using ZimMarket.Application.Common.Services;

namespace ZimMarket.Application;

public static class DependencyInjection
{
    public static IServiceCollection AddApplication(this IServiceCollection services) =>
        AddApplication(services, []);

    /// <summary>
    /// Registers Application services. Optional assemblies are scanned for additional MediatR handlers and FluentValidation validators (e.g. test or feature assemblies).
    /// </summary>
    public static IServiceCollection AddApplication(this IServiceCollection services, params Assembly[] additionalAssemblies)
    {
        services.AddValidatorsFromAssembly(Assembly.GetExecutingAssembly());
        foreach (var assembly in additionalAssemblies)
            services.AddValidatorsFromAssembly(assembly);

        // Pipeline: first registered is outermost. Order: Logging -> Validation -> Transaction -> Caching -> handler.
        // Runtime checks: Validation (Result / Result<T>); Transaction (ICommandMarker); Caching (ICacheable).
        services.AddMediatR(cfg =>
        {
            cfg.RegisterServicesFromAssembly(Assembly.GetExecutingAssembly());
            foreach (var assembly in additionalAssemblies)
                cfg.RegisterServicesFromAssembly(assembly);

            cfg.AddOpenBehavior(typeof(LoggingBehaviour<,>));
            cfg.AddOpenBehavior(typeof(ValidationBehaviour<,>));
            cfg.AddOpenBehavior(typeof(TransactionBehaviour<,>));
            cfg.AddOpenBehavior(typeof(CachingBehaviour<,>));
        });

        if (services.All(d => d.ServiceType != typeof(ICacheService)))
            services.AddSingleton<ICacheService, NullCacheService>();

        if (services.All(d => d.ServiceType != typeof(ICurrentUser)))
            services.AddScoped<ICurrentUser, AnonymousCurrentUser>();

        if (services.All(d => d.ServiceType != typeof(IEmailService)))
            services.AddSingleton<IEmailService, NullEmailService>();

        if (services.All(d => d.ServiceType != typeof(ISmsService)))
            services.AddSingleton<ISmsService, NullSmsService>();

        if (services.All(d => d.ServiceType != typeof(IPushNotificationService)))
            services.AddSingleton<IPushNotificationService, NullPushNotificationService>();

        if (services.All(d => d.ServiceType != typeof(INotificationJobScheduler)))
            services.AddSingleton<INotificationJobScheduler, InlineNotificationJobScheduler>();

        return services;
    }
}
