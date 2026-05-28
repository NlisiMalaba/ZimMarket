using System.Text.Json;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.OpenApi;
using Scalar.AspNetCore;
using Serilog;
using ZimMarket.API.Configuration;
using ZimMarket.API.Cors;
using ZimMarket.API.Health;
using ZimMarket.API.Logging;
using ZimMarket.API.Middleware;
using ZimMarket.API.OpenApi;
using ZimMarket.API.RateLimiting;
using ZimMarket.Application;
using ZimMarket.Application.Auth;
using ZimMarket.Application.Common.Interfaces;
using ZimMarket.Application.Common.Services;
using ZimMarket.Infrastructure;
using ZimMarket.Infrastructure.Authentication;
using ZimMarket.Infrastructure.BackgroundJobs;
using ZimMarket.Infrastructure.RealTime;

Log.Logger = new LoggerConfiguration()
    .MinimumLevel.Information()
    .WriteTo.Console()
    .CreateBootstrapLogger();

try
{
    var builder = WebApplication.CreateBuilder(args);

    builder.AddZimMarketSerilog();
    builder.Services.AddZimMarketConfigurationValidation(
        builder.Configuration,
        enforceRequiredConfiguration: builder.Environment.IsProduction());

    builder.Services.AddInfrastructure(builder.Configuration);
    builder.Services.AddApplication();
    builder.Services.AddControllers()
        .AddJsonOptions(options =>
        {
            options.JsonSerializerOptions.PropertyNamingPolicy = JsonNamingPolicy.CamelCase;
            options.JsonSerializerOptions.DictionaryKeyPolicy = JsonNamingPolicy.CamelCase;
        });
    builder.Services.AddZimMarketCors(builder.Configuration);
    builder.Services.AddZimMarketRateLimiter();
    builder.Services.AddZimMarketHealthChecks(builder.Configuration);
    builder.Services.AddTransient<ZimMarketOpenApiDocumentTransformer>();
    builder.Services.AddOpenApi(options =>
        options.AddDocumentTransformer<ZimMarketOpenApiDocumentTransformer>());

    builder.Services.AddHttpContextAccessor();

    for (int i = builder.Services.Count - 1; i >= 0; i--)
    {
        if (builder.Services[i].ServiceType == typeof(ICurrentUser))
            builder.Services.RemoveAt(i);
    }

    builder.Services.AddScoped<ICurrentUser, HttpContextCurrentUser>();

    builder.Services.AddZimMarketWebAuthenticationAndAuthorization(builder.Configuration);

    var app = builder.Build();

    app.UseMiddleware<ExceptionHandlingMiddleware>();
    app.UseZimMarketSerilogRequestLogging();
    app.UseCors(ZimMarketCorsExtensions.PolicyName);

    if (HangfireJobSetup.IsHangfireStorageConfigured(app.Configuration))
        app.RegisterZimMarketHangfireRecurringJobs();

    if (app.Environment.IsDevelopment())
    {
        app.MapOpenApi();
        app.MapScalarApiReference(options =>
        {
            options.WithTitle("ZimMarket API");
            options.WithOpenApiRoutePattern("/openapi/{documentName}.json");
            options.AddDocument("v1", "ZimMarket API");
            options.SortTagsAlphabetically();
            options.SortOperationsByMethod();
            options.AddPreferredSecuritySchemes(JwtBearerDefaults.AuthenticationScheme);
            options.AddHttpAuthentication(JwtBearerDefaults.AuthenticationScheme, _ => { });
        }).AllowAnonymous();
    }

    app.UseAuthentication();
    app.UseRateLimiter();
    app.UseAuthorization();
    app.UseMiddleware<IdempotencyMiddleware>();

    if (HangfireJobSetup.IsHangfireStorageConfigured(app.Configuration))
        app.UseZimMarketHangfireDashboard();

    app.MapZimMarketHealthChecks();
    app.MapControllers();
    app.MapHub<TrackingHub>("/hubs/tracking")
        .RequireAuthorization(AuthorizationPolicies.TrackingHub);

    app.Run();
}
catch (Exception ex)
{
    Log.Fatal(ex, "Application terminated unexpectedly.");
    throw;
}
finally
{
    Log.CloseAndFlush();
}

public partial class Program
{
}
