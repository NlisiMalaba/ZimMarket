using ZimMarket.Application;
using ZimMarket.Infrastructure;
using ZimMarket.Infrastructure.BackgroundJobs;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddInfrastructure(builder.Configuration);
builder.Services.AddApplication();
builder.Services.AddControllers();
builder.Services.AddHealthChecks();
builder.Services.AddOpenApi();

var app = builder.Build();

if (HangfireJobSetup.IsHangfireStorageConfigured(app.Configuration))
    app.RegisterZimMarketHangfireRecurringJobs();

if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
}

// TLS is expected at the reverse proxy; Docker / local HTTP uses Kestrel without HTTPS.
app.UseAuthorization();

if (HangfireJobSetup.IsHangfireStorageConfigured(app.Configuration))
    app.UseZimMarketHangfireDashboard();

app.MapHealthChecks("/health");
app.MapControllers();

app.Run();
