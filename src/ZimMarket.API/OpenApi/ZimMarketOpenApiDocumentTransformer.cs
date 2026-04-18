using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.OpenApi;
using Microsoft.OpenApi;

namespace ZimMarket.API.OpenApi;

/// <summary>
/// Enriches the generated OpenAPI document for Scalar: API metadata and JWT bearer scheme when configured.
/// </summary>
internal sealed class ZimMarketOpenApiDocumentTransformer(IAuthenticationSchemeProvider authenticationSchemeProvider)
    : IOpenApiDocumentTransformer
{
    public async Task TransformAsync(
        OpenApiDocument document,
        OpenApiDocumentTransformerContext context,
        CancellationToken cancellationToken)
    {
        document.Info ??= new OpenApiInfo();
        document.Info.Title = "ZimMarket API";
        document.Info.Version = "v1";
        document.Info.Description =
            "HTTP API for ZimMarket. Authenticated routes expect `Authorization: Bearer {accessToken}` from login or registration.";

        IEnumerable<AuthenticationScheme> schemes =
            await authenticationSchemeProvider.GetAllSchemesAsync().ConfigureAwait(false);

        if (!schemes.Any(s =>
                string.Equals(s.Name, JwtBearerDefaults.AuthenticationScheme, StringComparison.Ordinal)))
            return;

        document.Components ??= new OpenApiComponents();
        document.Components.SecuritySchemes ??= new Dictionary<string, IOpenApiSecurityScheme>(StringComparer.Ordinal);
        document.Components.SecuritySchemes[JwtBearerDefaults.AuthenticationScheme] = new OpenApiSecurityScheme
        {
            Type = SecuritySchemeType.Http,
            Scheme = "bearer",
            BearerFormat = "JWT",
            Description = "Obtain a token from `POST /api/v1/auth/login` or a register endpoint, then paste the access token."
        };
    }
}
