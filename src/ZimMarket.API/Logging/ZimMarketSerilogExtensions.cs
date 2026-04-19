using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Serilog;
using Serilog.AspNetCore;
using Serilog.Events;

namespace ZimMarket.API.Logging;

public static class ZimMarketSerilogExtensions
{
    public static WebApplicationBuilder AddZimMarketSerilog(this WebApplicationBuilder builder)
    {
        ArgumentNullException.ThrowIfNull(builder);

        builder.Logging.ClearProviders();

        builder.Host.UseSerilog(
            (context, services, loggerConfiguration) =>
            {
                loggerConfiguration
                    .ReadFrom.Configuration(context.Configuration)
                    .ReadFrom.Services(services)
                    .Enrich.FromLogContext()
                    .Enrich.WithProperty("Application", "ZimMarket.API")
                    .Destructure.With(new RedactSensitivePropertiesDestructuringPolicy());

                if (context.HostingEnvironment.IsDevelopment())
                {
                    loggerConfiguration.WriteTo.Console(
                        outputTemplate: "[{Timestamp:HH:mm:ss} {Level:u3}] {SourceContext}: {Message:lj}{NewLine}{Exception}",
                        restrictedToMinimumLevel: LogEventLevel.Debug);
                }
                else
                {
                    string filePath = context.Configuration["Serilog:File:Path"] ?? "logs/zimmarket-.log";
                    int retainedFileCount = context.Configuration.GetValue("Serilog:File:RetainedFileCountLimit", 31);
                    loggerConfiguration.WriteTo.File(
                        path: filePath,
                        rollingInterval: RollingInterval.Day,
                        retainedFileCountLimit: retainedFileCount,
                        shared: true,
                        outputTemplate: "{Timestamp:yyyy-MM-dd HH:mm:ss.fff zzz} [{Level:u3}] {SourceContext}: {Message:lj}{NewLine}{Exception}",
                        restrictedToMinimumLevel: LogEventLevel.Information);
                }

                string? seqUrl = context.Configuration["Serilog:Seq:ServerUrl"];
                if (!string.IsNullOrWhiteSpace(seqUrl))
                {
                    string? apiKey = context.Configuration["Serilog:Seq:ApiKey"];
                    loggerConfiguration.WriteTo.Seq(
                        serverUrl: seqUrl,
                        apiKey: string.IsNullOrWhiteSpace(apiKey) ? null : apiKey,
                        restrictedToMinimumLevel: LogEventLevel.Information);
                }
            },
            preserveStaticLogger: false,
            writeToProviders: false);

        return builder;
    }

    public static IApplicationBuilder UseZimMarketSerilogRequestLogging(this IApplicationBuilder app)
    {
        ArgumentNullException.ThrowIfNull(app);

        return app.UseSerilogRequestLogging(options =>
        {
            options.MessageTemplate =
                "HTTP {RequestMethod} {RequestPath} responded {StatusCode} in {Elapsed:0.0000} ms";

            options.GetLevel = (httpContext, elapsed, ex) =>
            {
                if (ex is not null)
                    return LogEventLevel.Error;

                if (httpContext.Response.StatusCode > 499)
                    return LogEventLevel.Error;

                PathString path = httpContext.Request.Path;
                if (path.StartsWithSegments("/health", StringComparison.OrdinalIgnoreCase)
                    || path.StartsWithSegments("/scalar", StringComparison.OrdinalIgnoreCase)
                    || path.StartsWithSegments("/openapi", StringComparison.OrdinalIgnoreCase))
                {
                    return LogEventLevel.Verbose;
                }

                return LogEventLevel.Information;
            };
        });
    }
}
