using ZimMarket.Application;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddApplication();
builder.Services.AddControllers();
builder.Services.AddHealthChecks();
builder.Services.AddOpenApi();

var app = builder.Build();

if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
}

// TLS is expected at the reverse proxy; Docker / local HTTP uses Kestrel without HTTPS.
app.UseAuthorization();

app.MapHealthChecks("/health");
app.MapControllers();

app.Run();
