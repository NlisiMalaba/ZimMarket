using System.Net.Http;
using System.Text.Json.Nodes;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.OpenApi;
using Microsoft.OpenApi;

namespace ZimMarket.API.OpenApi;

/// <summary>
/// Enriches the generated OpenAPI document for Scalar: tags, examples, JWT bearer scheme, and default security requirements.
/// </summary>
internal sealed class ZimMarketOpenApiDocumentTransformer(IAuthenticationSchemeProvider authenticationSchemeProvider)
    : IOpenApiDocumentTransformer
{
    private const string ApiPrefix = "/api/v1/";

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

        bool jwtEnabled = schemes.Any(s =>
            string.Equals(s.Name, JwtBearerDefaults.AuthenticationScheme, StringComparison.Ordinal));

        if (jwtEnabled)
        {
            document.Components ??= new OpenApiComponents();
            document.Components.SecuritySchemes ??= new Dictionary<string, IOpenApiSecurityScheme>(StringComparer.Ordinal);
            document.Components.SecuritySchemes[JwtBearerDefaults.AuthenticationScheme] = new OpenApiSecurityScheme
            {
                Type = SecuritySchemeType.Http,
                Scheme = "bearer",
                BearerFormat = "JWT",
                Description =
                    "Paste a JWT access token from `POST /api/v1/auth/login` or a register response. Scalar sends `Authorization: Bearer {token}`."
            };

            document.Security ??= new List<OpenApiSecurityRequirement>();
            document.Security.Add(
                new OpenApiSecurityRequirement
                {
                    [new OpenApiSecuritySchemeReference(JwtBearerDefaults.AuthenticationScheme)] = []
                });
        }

        AddTagDefinitions(document);
        EnrichOperations(document, jwtEnabled);
    }

    private static void AddTagDefinitions(OpenApiDocument document)
    {
        document.Tags ??= new HashSet<OpenApiTag>();

        void AddTag(string name, string description)
        {
            if (document.Tags.Any(t => string.Equals(t.Name, name, StringComparison.Ordinal)))
                return;

            document.Tags.Add(new OpenApiTag { Name = name, Description = description });
        }

        AddTag("Authentication", "Register, login, refresh tokens, logout, and KYC submission.");
        AddTag("Admin", "Administrator operations (KYC review, users, dashboard).");
        AddTag("Warehouse", "Inbound warehouse operations and QC.");
        AddTag("Drivers", "Driver mobile APIs (location, deliveries).");
        AddTag("Delivery batches", "Create and manage delivery batches.");
        AddTag("Files", "Presigned uploads and secure file access.");
        AddTag("Orders", "Customer ordering.");
        AddTag("Payments", "Payment initiation and history.");
        AddTag("Products", "Catalogue and seller product management.");
    }

    private static void EnrichOperations(OpenApiDocument document, bool jwtEnabled)
    {
        if (document.Paths is null)
            return;

        foreach (KeyValuePair<string, IOpenApiPathItem> pathPair in document.Paths)
        {
            string path = pathPair.Key;
            IOpenApiPathItem pathItem = pathPair.Value;
            if (pathItem.Operations is null)
                continue;

            foreach (KeyValuePair<HttpMethod, OpenApiOperation> opPair in pathItem.Operations)
            {
                HttpMethod method = opPair.Key;
                OpenApiOperation operation = opPair.Value;
                ApplyTag(path, operation);
                ApplyExamples(path, method, operation);
                EnsureOperationExamples(path, method, operation);
                bool requiresAuthentication = !IsAnonymousOperation(path, method);
                EnsureErrorResponses(operation, requiresAuthentication);
                if (jwtEnabled)
                    ApplySecurity(operation, requiresAuthentication);
            }
        }
    }

    private static void ApplyTag(string path, OpenApiOperation operation)
    {
        string tag = ResolveTag(path);
        operation.Tags ??= new HashSet<OpenApiTagReference>();
        operation.Tags.Clear();
        operation.Tags.Add(new OpenApiTagReference(tag));
    }

    private static string ResolveTag(string path)
    {
        if (!path.StartsWith(ApiPrefix, StringComparison.Ordinal))
            return "Other";

        ReadOnlySpan<char> rest = path.AsSpan(ApiPrefix.Length);
        int slash = rest.IndexOf('/');
        ReadOnlySpan<char> resource = slash >= 0 ? rest[..slash] : rest;

        if (resource.Equals("auth", StringComparison.OrdinalIgnoreCase))
            return "Authentication";
        if (resource.Equals("admin", StringComparison.OrdinalIgnoreCase))
            return "Admin";
        if (resource.Equals("warehouse", StringComparison.OrdinalIgnoreCase))
            return "Warehouse";
        if (resource.Equals("drivers", StringComparison.OrdinalIgnoreCase))
            return "Drivers";
        if (resource.Equals("batches", StringComparison.OrdinalIgnoreCase))
            return "Delivery batches";
        if (resource.Equals("files", StringComparison.OrdinalIgnoreCase))
            return "Files";
        if (resource.Equals("orders", StringComparison.OrdinalIgnoreCase))
            return "Orders";
        if (resource.Equals("payments", StringComparison.OrdinalIgnoreCase))
            return "Payments";
        if (resource.Equals("products", StringComparison.OrdinalIgnoreCase))
            return "Products";

        return "Other";
    }

    private static void ApplySecurity(OpenApiOperation operation, bool requiresAuthentication)
    {
        operation.Security ??= new List<OpenApiSecurityRequirement>();

        if (!requiresAuthentication)
        {
            operation.Security.Clear();
            return;
        }

        operation.Security.Clear();
        operation.Security.Add(
            new OpenApiSecurityRequirement
            {
                [new OpenApiSecuritySchemeReference(JwtBearerDefaults.AuthenticationScheme)] = []
            });
    }

    private static bool IsAnonymousOperation(string path, HttpMethod method)
    {
        if (path.Equals("/api/v1/auth/login", StringComparison.OrdinalIgnoreCase) && method == HttpMethod.Post)
            return true;

        if (path.Contains("/register/", StringComparison.OrdinalIgnoreCase) && method == HttpMethod.Post)
            return true;

        if (path.Equals("/api/v1/auth/refresh", StringComparison.OrdinalIgnoreCase) && method == HttpMethod.Post)
            return true;

        if (path.Equals("/api/v1/products", StringComparison.OrdinalIgnoreCase) && method == HttpMethod.Get)
            return true;

        if (path.Equals("/api/v1/products/categories", StringComparison.OrdinalIgnoreCase) && method == HttpMethod.Get)
            return true;

        if (path.Equals("/api/v1/products/{id}", StringComparison.OrdinalIgnoreCase) && method == HttpMethod.Get)
            return true;

        if (path.Equals("/api/v1/payments/webhook/paynow", StringComparison.OrdinalIgnoreCase) && method == HttpMethod.Post)
            return true;

        if (path.Equals("/api/v1/payments/webhook/ecocash", StringComparison.OrdinalIgnoreCase) && method == HttpMethod.Post)
            return true;

        return false;
    }

    private static void ApplyExamples(string path, HttpMethod method, OpenApiOperation operation)
    {
        if (method == HttpMethod.Post && path.Equals("/api/v1/auth/login", StringComparison.OrdinalIgnoreCase))
        {
            SetJsonExample(
                operation.RequestBody?.Content,
                """
                {
                  "email": "customer@example.com",
                  "password": "YourPassword1",
                  "deviceInfo": "ZimMarket-Android/1.0"
                }
                """);

            SetJsonExampleOnMedia(
                GetResponseMedia(operation.Responses, "200"),
                """
                {
                  "data": {
                    "accessToken": "eyJhbGciOiJIUzI1NiIsInR5cCI6IkpXVCJ9...",
                    "refreshToken": "base64url-encoded-refresh-token",
                    "kycStatus": "notSubmitted"
                  }
                }
                """);
        }

        if (method == HttpMethod.Post && path.Equals("/api/v1/auth/register/customer", StringComparison.OrdinalIgnoreCase))
        {
            SetJsonExample(
                operation.RequestBody?.Content,
                """
                {
                  "email": "customer@example.com",
                  "phone": "+263771234567",
                  "password": "YourPassword1",
                  "fullName": "Jane Customer",
                  "pushToken": "fcm-device-token-optional"
                }
                """);
        }

        AddErrorResponseExample(operation.Responses, "422");
        AddErrorResponseExample(operation.Responses, "400");
    }

    private static void EnsureOperationExamples(string path, HttpMethod method, OpenApiOperation operation)
    {
        if (method == HttpMethod.Get)
        {
            EnsureJsonResponseExample(
                operation.Responses,
                "200",
                """
                {
                  "data": {}
                }
                """);
            return;
        }

        if (path.Equals("/api/v1/payments/webhook/paynow", StringComparison.OrdinalIgnoreCase)
            || path.Equals("/api/v1/payments/webhook/ecocash", StringComparison.OrdinalIgnoreCase))
        {
            return;
        }

        if (method == HttpMethod.Post || method == HttpMethod.Put || method == HttpMethod.Patch)
        {
            EnsureJsonRequestExample(operation.RequestBody?.Content);
            EnsureJsonResponseExample(
                operation.Responses,
                "200",
                """
                {
                  "data": {}
                }
                """);
            EnsureJsonResponseExample(
                operation.Responses,
                "201",
                """
                {
                  "data": "00000000-0000-0000-0000-000000000000"
                }
                """);
        }
    }

    private static void EnsureJsonRequestExample(IDictionary<string, OpenApiMediaType>? content)
    {
        if (content is null || !content.TryGetValue("application/json", out OpenApiMediaType? media))
            return;

        if (media.Example is not null)
            return;

        media.Example = JsonNode.Parse(
            """
            {
              "sample": "value"
            }
            """);
    }

    private static void EnsureJsonResponseExample(OpenApiResponses? responses, string statusCode, string json)
    {
        OpenApiMediaType? media = GetResponseMedia(responses, statusCode);
        if (media is null || media.Example is not null)
            return;

        media.Example = JsonNode.Parse(json);
    }

    private static void EnsureErrorResponses(OpenApiOperation operation, bool requiresAuthentication)
    {
        AddErrorResponse(operation.Responses, "400", "Bad Request");
        AddErrorResponse(operation.Responses, "422", "Validation Error");
        AddErrorResponse(operation.Responses, "429", "Too Many Requests");
        AddErrorResponse(operation.Responses, "500", "Internal Server Error");

        if (requiresAuthentication)
        {
            AddErrorResponse(operation.Responses, "401", "Unauthorized");
            AddErrorResponse(operation.Responses, "403", "Forbidden");
        }
    }

    private static void AddErrorResponse(OpenApiResponses? responses, string statusCode, string description)
    {
        if (responses is null)
            return;

        if (!responses.TryGetValue(statusCode, out IOpenApiResponse? existingResponse))
        {
            var created = new OpenApiResponse
            {
                Description = description,
                Content = new Dictionary<string, OpenApiMediaType>(StringComparer.OrdinalIgnoreCase)
                {
                    ["application/json"] = new OpenApiMediaType()
                }
            };
            responses.Add(statusCode, created);
            AddErrorResponseExample(responses, statusCode);
            return;
        }

        if (existingResponse is OpenApiResponse openApiResponse)
        {
            if (string.IsNullOrWhiteSpace(openApiResponse.Description))
                openApiResponse.Description = description;

            openApiResponse.Content ??= new Dictionary<string, OpenApiMediaType>(StringComparer.OrdinalIgnoreCase);
            if (!openApiResponse.Content.ContainsKey("application/json"))
                openApiResponse.Content["application/json"] = new OpenApiMediaType();
        }

        AddErrorResponseExample(responses, statusCode);
    }

    private static void SetJsonExample(IDictionary<string, OpenApiMediaType>? content, string json)
    {
        if (content is null || !content.TryGetValue("application/json", out OpenApiMediaType? media))
            return;

        SetJsonExampleOnMedia(media, json);
    }

    private static void SetJsonExampleOnMedia(OpenApiMediaType? media, string json)
    {
        if (media is null)
            return;

        media.Example = JsonNode.Parse(json);
    }

    private static OpenApiMediaType? GetResponseMedia(OpenApiResponses? responses, string statusCode)
    {
        if (responses is null || !responses.TryGetValue(statusCode, out IOpenApiResponse? openApiResponse))
            return null;

        if (openApiResponse is not OpenApiResponse response
            || response.Content is null
            || !response.Content.TryGetValue("application/json", out OpenApiMediaType? media))
        {
            return null;
        }

        return media;
    }

    private static void AddErrorResponseExample(OpenApiResponses? responses, string statusCode)
    {
        OpenApiMediaType? media = GetResponseMedia(responses, statusCode);
        if (media is null || media.Example is not null)
            return;

        media.Example = JsonNode.Parse(
            """
            {
              "errorCode": "Validation",
              "message": "Validation failed.",
              "traceId": "00-abc123-def456-01",
              "validationErrors": [
                { "field": "email", "message": "Email is required." }
              ]
            }
            """);
    }
}
