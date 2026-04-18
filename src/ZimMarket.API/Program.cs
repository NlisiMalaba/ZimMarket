using ZimMarket.Application;
using ZimMarket.Application.Common.Interfaces;
using ZimMarket.Application.Common.Services;
using ZimMarket.Infrastructure;
using ZimMarket.Infrastructure.Authentication;
using ZimMarket.Infrastructure.BackgroundJobs;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddInfrastructure(builder.Configuration);
builder.Services.AddApplication();
builder.Services.AddControllers();
builder.Services.AddHealthChecks();
builder.Services.AddOpenApi();

builder.Services.AddHttpContextAccessor();

for (int i = builder.Services.Count - 1; i >= 0; i--)
{
    if (builder.Services[i].ServiceType == typeof(ICurrentUser))
        builder.Services.RemoveAt(i);
}

builder.Services.AddScoped<ICurrentUser, HttpContextCurrentUser>();

builder.Services.AddZimMarketWebAuthenticationAndAuthorization(builder.Configuration);

var app = builder.Build();

if (HangfireJobSetup.IsHangfireStorageConfigured(app.Configuration))
    app.RegisterZimMarketHangfireRecurringJobs();

if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
}

app.UseAuthentication();
app.UseAuthorization();

if (HangfireJobSetup.IsHangfireStorageConfigured(app.Configuration))
    app.UseZimMarketHangfireDashboard();

app.MapHealthChecks("/health");
app.MapControllers();

app.Run();

public partial class Program
{
}
