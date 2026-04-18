using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;
using FluentAssertions;
using ZimMarket.Application.Common;
using ZimMarket.Integration.Tests.Fixtures;

namespace ZimMarket.Integration.Tests;

[Collection("AuthApi")]
public sealed class AuthEndpointsIntegrationTests
{
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNameCaseInsensitive = true,
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase
    };

    private readonly ZimMarketAuthApiFixture _fixture;

    public AuthEndpointsIntegrationTests(ZimMarketAuthApiFixture fixture)
    {
        _fixture = fixture;
    }

    [Fact]
    public async Task Register_login_refresh_logout_happy_path()
    {
        HttpClient client = _fixture.CreateClient();
        string email = $"auth-flow-{Guid.NewGuid():N}@example.com";
        const string password = "Password1";
        const string phone = "+263774123456";

        using var registerResponse = await client.PostAsJsonAsync(
            "/api/v1/auth/register/customer",
            new RegisterCustomerApiDto(email, phone, password, "Integration User", null));

        registerResponse.StatusCode.Should().Be(HttpStatusCode.Created);
        AuthTokensJson? registerTokens = await ReadTokensAsync(registerResponse);
        registerTokens.Should().NotBeNull();

        using var loginResponse = await client.PostAsJsonAsync(
            "/api/v1/auth/login",
            new LoginApiDto(email, password, null));

        loginResponse.StatusCode.Should().Be(HttpStatusCode.OK);
        AuthTokensJson? loginTokens = await ReadTokensAsync(loginResponse);
        loginTokens.Should().NotBeNull();

        await Task.Delay(TimeSpan.FromSeconds(1));

        using var refreshResponse = await client.PostAsJsonAsync(
            "/api/v1/auth/refresh",
            new RefreshApiDto(loginTokens!.AccessToken, loginTokens.RefreshToken));

        refreshResponse.StatusCode.Should().Be(HttpStatusCode.OK);
        AuthTokensJson? refreshTokens = await ReadTokensAsync(refreshResponse);
        refreshTokens.Should().NotBeNull();

        client.DefaultRequestHeaders.Authorization =
            new AuthenticationHeaderValue("Bearer", refreshTokens!.AccessToken);

        using HttpResponseMessage logoutResponse = await client.PostAsJsonAsync(
            "/api/v1/auth/logout",
            new LogoutApiDto(refreshTokens!.RefreshToken));

        logoutResponse.StatusCode.Should().Be(HttpStatusCode.OK);
    }

    [Fact]
    public async Task Concurrent_register_same_email_one_conflict_or_server_error()
    {
        HttpClient a = _fixture.CreateClient();
        HttpClient b = _fixture.CreateClient();

        string email = $"dup-{Guid.NewGuid():N}@example.com";
        const string password = "Password1";

        var bodyA = new RegisterCustomerApiDto(email, "+263775123456", password, "User A", null);
        var bodyB = new RegisterCustomerApiDto(email, "+263776123456", password, "User B", null);

        Task<HttpResponseMessage> first = a.PostAsJsonAsync("/api/v1/auth/register/customer", bodyA);
        Task<HttpResponseMessage> second = b.PostAsJsonAsync("/api/v1/auth/register/customer", bodyB);

        HttpResponseMessage[] responses = await Task.WhenAll(first, second);

        int created = responses.Count(r => r.StatusCode == HttpStatusCode.Created);
        created.Should().Be(1);

        HttpResponseMessage failed = responses.Single(r => r.StatusCode != HttpStatusCode.Created);
        failed.StatusCode.Should().BeOneOf(HttpStatusCode.Conflict, HttpStatusCode.InternalServerError);

        if (failed.StatusCode == HttpStatusCode.Conflict)
        {
            string json = await failed.Content.ReadAsStringAsync();
            using var doc = JsonDocument.Parse(json);
            doc.RootElement.GetProperty("errorCode").GetString().Should().Be(AuthErrorCodes.UserAlreadyExists);
        }
    }

    private sealed record RegisterCustomerApiDto(
        string Email,
        string Phone,
        string Password,
        string FullName,
        string? PushToken);

    private sealed record LoginApiDto(string Email, string Password, string? DeviceInfo);

    private sealed record RefreshApiDto(string AccessToken, string RefreshToken);

    private sealed record LogoutApiDto(string RefreshToken);

    private sealed record ApiSuccess<T>(T? Data);

    private sealed record AuthTokensJson(string AccessToken, string RefreshToken, int KycStatus);

    private static async Task<AuthTokensJson?> ReadTokensAsync(HttpResponseMessage response)
    {
        ApiSuccess<AuthTokensJson>? envelope =
            await response.Content.ReadFromJsonAsync<ApiSuccess<AuthTokensJson>>(JsonOptions).ConfigureAwait(false);

        return envelope?.Data;
    }
}
