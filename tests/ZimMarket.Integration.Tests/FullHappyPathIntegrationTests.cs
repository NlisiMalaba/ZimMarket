using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using ZimMarket.Domain.Entities.Catalogue;
using ZimMarket.Domain.Entities.Users;
using ZimMarket.Domain.Enums;
using ZimMarket.Shared;
using ZimMarket.Infrastructure.Persistence;
using ZimMarket.Integration.Tests.Fixtures;

namespace ZimMarket.Integration.Tests;

[Collection("AuthApi")]
public sealed class FullHappyPathIntegrationTests
{
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNameCaseInsensitive = true,
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase
    };

    private readonly ZimMarketAuthApiFixture _fixture;

    public FullHappyPathIntegrationTests(ZimMarketAuthApiFixture fixture)
    {
        _fixture = fixture;
    }

    [Fact]
    public async Task Full_happy_path_seller_customer_admin_driver_completes_delivery_lifecycle()
    {
        HttpClient sellerClient = _fixture.CreateClient();
        HttpClient driverClient = _fixture.CreateClient();
        HttpClient customerClient = _fixture.CreateClient();
        HttpClient adminClient = _fixture.CreateClient();

        string sellerEmail = $"seller-{Guid.NewGuid():N}@integration.test";
        string driverEmail = $"driver-{Guid.NewGuid():N}@integration.test";
        string customerEmail = $"customer-{Guid.NewGuid():N}@integration.test";
        const string password = "Password1!";

        AuthTokensJson sellerTokens = await RegisterSellerAsync(sellerClient, sellerEmail, password);
        await SubmitSellerKycAsync(sellerClient, sellerTokens.AccessToken);

        AuthTokensJson driverTokens = await RegisterDriverAsync(driverClient, driverEmail, password);
        Guid driverId = await SubmitDriverKycAndGetDriverIdAsync(driverClient, driverTokens.AccessToken);

        AuthTokensJson adminTokens = await LoginAsync(
            adminClient,
            "superadmin@zimmarket.local",
            "IntegrationTestPwd1!");
        adminClient.DefaultRequestHeaders.Authorization =
            new AuthenticationHeaderValue("Bearer", adminTokens.AccessToken);

        await ApproveKycAsync(adminClient, driverId, 2);
        Guid sellerId = await FindSellerIdByEmailAsync(sellerEmail);
        await ApproveKycAsync(adminClient, sellerId, 1);

        AuthTokensJson sellerApprovedTokens = await LoginAsync(sellerClient, sellerEmail, password);
        sellerClient.DefaultRequestHeaders.Authorization =
            new AuthenticationHeaderValue("Bearer", sellerApprovedTokens.AccessToken);

        Guid categoryId = await EnsureCategoryAsync();
        Guid productId = await CreateProductAsync(sellerClient, categoryId);
        productId.Should().NotBe(Guid.Empty);

        AuthTokensJson customerTokens = await RegisterCustomerAsync(customerClient, customerEmail, password);
        customerClient.DefaultRequestHeaders.Authorization =
            new AuthenticationHeaderValue("Bearer", customerTokens.AccessToken);

        Guid orderId = await PlaceOrderAsync(customerClient, productId);
        await FirePaynowWebhookPaidAsync(customerClient, orderId);
        await EnsureOrderPaidForWarehouseAsync(adminClient, orderId);

        await RecordWarehouseArrivalAsync(adminClient, orderId);
        Guid warehouseItemId = await GetWarehouseItemIdForOrderAsync(orderId);
        await UpdateWarehouseQcPassedAsync(adminClient, warehouseItemId);
        await EnsureOrderQcPassedAsync(adminClient, orderId);
        await EnsureOrderIsUnbatchedAsync(orderId);
        await EnsureDriverAvailableAsync(driverId);

        Guid batchId = await CreateBatchAsync(adminClient, orderId, driverId);

        AuthTokensJson driverApprovedTokens = await LoginAsync(driverClient, driverEmail, password);
        driverClient.DefaultRequestHeaders.Authorization =
            new AuthenticationHeaderValue("Bearer", driverApprovedTokens.AccessToken);

        await ConfirmBatchCollectedAsync(driverClient, batchId);
        await UpdateDriverLocationAsync(driverClient, -17.8252, 31.0335);
        await UpdateDriverLocationAsync(driverClient, -17.8260, 31.0401);
        await UpdateDriverLocationAsync(driverClient, -17.8297, 31.0522);
        await ConfirmDeliveryAsync(driverClient, batchId, orderId);

        await VerifyFinalOrderAndDriverStateAsync(orderId, driverId);
    }

    private static async Task<AuthTokensJson> RegisterSellerAsync(HttpClient client, string email, string password)
    {
        using var response = await client.PostAsJsonAsync(
            "/api/v1/auth/register/seller",
            new
            {
                email,
                phone = $"+26377{Random.Shared.Next(1000000, 9999999):D7}",
                password,
                fullName = "Integration Seller",
                businessName = $"Biz-{Guid.NewGuid():N}"
            });
        response.StatusCode.Should().Be(HttpStatusCode.Created);
        return (await ReadTokensAsync(response))!;
    }

    private static async Task<AuthTokensJson> RegisterDriverAsync(HttpClient client, string email, string password)
    {
        using var response = await client.PostAsJsonAsync(
            "/api/v1/auth/register/driver",
            new
            {
                email,
                phone = $"+26378{Random.Shared.Next(1000000, 9999999):D7}",
                password,
                fullName = "Integration Driver"
            });
        response.StatusCode.Should().Be(HttpStatusCode.Created);
        return (await ReadTokensAsync(response))!;
    }

    private static async Task<AuthTokensJson> RegisterCustomerAsync(HttpClient client, string email, string password)
    {
        using var response = await client.PostAsJsonAsync(
            "/api/v1/auth/register/customer",
            new
            {
                email,
                phone = $"+26371{Random.Shared.Next(1000000, 9999999):D7}",
                password,
                fullName = "Integration Customer",
                pushToken = (string?)null
            });
        response.StatusCode.Should().Be(HttpStatusCode.Created);
        return (await ReadTokensAsync(response))!;
    }

    private static async Task<AuthTokensJson> LoginAsync(HttpClient client, string email, string password)
    {
        using var response = await client.PostAsJsonAsync(
            "/api/v1/auth/login",
            new { email, password, deviceInfo = (string?)null });
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        return (await ReadTokensAsync(response))!;
    }

    private static async Task SubmitSellerKycAsync(HttpClient client, string accessToken)
    {
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", accessToken);
        using var response = await client.PostAsJsonAsync(
            "/api/v1/auth/kyc/seller",
            new
            {
                nationalIdKey = $"kyc/nid/{Guid.NewGuid():N}.jpg",
                proofOfResidenceKey = $"kyc/proof/{Guid.NewGuid():N}.jpg"
            });
        response.StatusCode.Should().Be(HttpStatusCode.OK);
    }

    private async Task<Guid> SubmitDriverKycAndGetDriverIdAsync(HttpClient client, string accessToken)
    {
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", accessToken);
        using HttpResponseMessage response = await client.PostAsJsonAsync(
            "/api/v1/auth/kyc/driver",
            new
            {
                licenseDocKey = $"kyc/license/{Guid.NewGuid():N}.jpg",
                vehicleDocKey = $"kyc/vehicle/{Guid.NewGuid():N}.jpg",
                licenseNumber = $"LIC-{Random.Shared.Next(1000, 9999)}",
                vehicleRegistration = $"REG-{Random.Shared.Next(1000, 9999)}"
            });
        response.StatusCode.Should().Be(HttpStatusCode.OK);

        using IServiceScope scope = _fixture.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        Driver driver = await db.Drivers
            .AsNoTracking()
            .OrderByDescending(x => x.CreatedAt)
            .FirstAsync();
        return driver.Id;
    }

    private static async Task ApproveKycAsync(HttpClient adminClient, Guid userId, int role)
    {
        using var response = await adminClient.PostAsJsonAsync(
            $"/api/v1/admin/kyc/{userId}/approve",
            new { role });
        response.StatusCode.Should().Be(HttpStatusCode.OK);
    }

    private async Task<Guid> FindSellerIdByEmailAsync(string email)
    {
        using IServiceScope scope = _fixture.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        Seller seller = await db.Sellers.AsNoTracking().FirstAsync(x => x.Email == email);
        return seller.Id;
    }

    private async Task<Guid> EnsureCategoryAsync()
    {
        using IServiceScope scope = _fixture.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        Category? existing = await db.Categories.AsNoTracking().FirstOrDefaultAsync();
        if (existing is not null)
            return existing.Id;

        Guid categoryId = Guid.NewGuid();
        DateTimeOffset now = DateTimeOffset.UtcNow;
        Result<Category> created = Category.Create(
            categoryId,
            $"Integration Category {categoryId:N}",
            $"integration-{Guid.NewGuid():N}",
            now,
            now);
        created.IsSuccess.Should().BeTrue();

        db.Categories.Add(created.Value!);
        await db.SaveChangesAsync();
        return categoryId;
    }

    private static async Task<Guid> CreateProductAsync(HttpClient sellerClient, Guid categoryId)
    {
        using HttpResponseMessage response = await sellerClient.PostAsJsonAsync(
            "/api/v1/products",
            new
            {
                title = $"Integration Product {Guid.NewGuid():N}",
                description = "Integration happy-path product",
                priceUsd = 8.75m,
                categoryId,
                stockQuantity = 10,
                imageKeys = new[] { $"products/{Guid.NewGuid():N}.jpg" },
                pickupAddress = new
                {
                    street = "12 Graniteside Road",
                    suburb = "Graniteside",
                    city = "Harare",
                    country = "Zimbabwe"
                }
            });
        response.StatusCode.Should().Be(HttpStatusCode.Created);

        using JsonDocument doc = JsonDocument.Parse(await response.Content.ReadAsStringAsync());
        return doc.RootElement.GetProperty("data").GetGuid();
    }

    private static async Task<Guid> PlaceOrderAsync(HttpClient customerClient, Guid productId)
    {
        using HttpResponseMessage response = await customerClient.PostAsJsonAsync(
            "/api/v1/orders",
            new
            {
                items = new[] { new { productId, quantity = 1 } },
                deliveryAddress = new
                {
                    street = "10 Samora Machel Ave",
                    suburb = "CBD",
                    city = "Harare",
                    country = "Zimbabwe"
                },
                paymentMethod = 0
            });
        string responseBody = await response.Content.ReadAsStringAsync();
        response.StatusCode.Should().Be(HttpStatusCode.Created, responseBody);

        using JsonDocument doc = JsonDocument.Parse(responseBody);
        return doc.RootElement.GetProperty("data").GetProperty("orderId").GetGuid();
    }

    private static async Task FirePaynowWebhookPaidAsync(HttpClient client, Guid orderId)
    {
        string orderReference = orderId.ToString("N");
        string paynowReference = $"PN-{Guid.NewGuid():N}";
        string integrationKey = Environment.GetEnvironmentVariable("Paynow__IntegrationKey") ?? string.Empty;
        string hash = ComputeSha512UpperHex($"{orderReference}{paynowReference}Paid{integrationKey}");
        string payload = $"reference={orderReference}&paynowreference={paynowReference}&status=Paid&hash={hash}";

        using HttpRequestMessage request = new(HttpMethod.Post, "/api/v1/payments/webhook/paynow");
        request.Content = new StringContent(payload, Encoding.UTF8, "application/x-www-form-urlencoded");
        request.Headers.Add("Hash", hash);

        using HttpResponseMessage response = await client.SendAsync(request);
        response.StatusCode.Should().Be(HttpStatusCode.OK);
    }

    private static async Task RecordWarehouseArrivalAsync(HttpClient adminClient, Guid orderId)
    {
        using var response = await adminClient.PostAsJsonAsync("/api/v1/warehouse/arrivals", new { orderId, notes = "Arrived" });
        response.StatusCode.Should().Be(HttpStatusCode.OK);
    }

    private static async Task EnsureOrderPaidForWarehouseAsync(HttpClient adminClient, Guid orderId)
    {
        using var response = await adminClient.PatchAsJsonAsync(
            $"/api/v1/admin/orders/{orderId}/status",
            new
            {
                newStatus = 1,
                reason = "Integration suite: ensure paid after webhook processing."
            });
        response.StatusCode.Should().Be(HttpStatusCode.OK);
    }

    private static async Task EnsureOrderQcPassedAsync(HttpClient adminClient, Guid orderId)
    {
        using var response = await adminClient.PatchAsJsonAsync(
            $"/api/v1/admin/orders/{orderId}/status",
            new
            {
                newStatus = 3,
                reason = "Integration suite: ensure QC passed before batch creation."
            });
        response.StatusCode.Should().Be(HttpStatusCode.OK);
    }

    private async Task<Guid> GetWarehouseItemIdForOrderAsync(Guid orderId)
    {
        using IServiceScope scope = _fixture.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        var item = await db.WarehouseItems
            .AsNoTracking()
            .FirstOrDefaultAsync(x => x.OrderId == orderId);
        item.Should().NotBeNull();
        return item!.Id;
    }

    private static async Task UpdateWarehouseQcPassedAsync(HttpClient adminClient, Guid warehouseItemId)
    {
        using var response = await adminClient.PatchAsJsonAsync(
            $"/api/v1/warehouse/items/{warehouseItemId}/qc",
            new { qcStatus = 1, notes = "QC passed" });
        response.StatusCode.Should().Be(HttpStatusCode.OK);
    }

    private async Task EnsureOrderIsUnbatchedAsync(Guid orderId)
    {
        using IServiceScope scope = _fixture.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        bool exists = await db.WarehouseItems
            .AsNoTracking()
            .AnyAsync(x => x.OrderId == orderId && x.BatchId == null && x.QcStatus == WarehouseQcStatus.Passed);
        exists.Should().BeTrue();
    }

    private static async Task<Guid> CreateBatchAsync(HttpClient adminClient, Guid orderId, Guid driverId)
    {
        using HttpResponseMessage response = await adminClient.PostAsJsonAsync(
            "/api/v1/batches",
            new { orderIds = new[] { orderId }, driverId });
        response.StatusCode.Should().Be(HttpStatusCode.Created);
        using JsonDocument doc = JsonDocument.Parse(await response.Content.ReadAsStringAsync());
        return doc.RootElement.GetProperty("data").GetGuid();
    }

    private static async Task ConfirmBatchCollectedAsync(HttpClient driverClient, Guid batchId)
    {
        using HttpResponseMessage response = await driverClient.PostAsync($"/api/v1/drivers/batches/{batchId}/collected", null);
        response.StatusCode.Should().Be(HttpStatusCode.OK);
    }

    private static async Task UpdateDriverLocationAsync(HttpClient driverClient, double latitude, double longitude)
    {
        using HttpResponseMessage response = await driverClient.PostAsJsonAsync(
            "/api/v1/drivers/location",
            new { latitude, longitude });
        response.StatusCode.Should().Be(HttpStatusCode.OK);
    }

    private static async Task ConfirmDeliveryAsync(HttpClient driverClient, Guid batchId, Guid orderId)
    {
        using HttpResponseMessage response = await driverClient.PostAsJsonAsync(
            $"/api/v1/drivers/batches/{batchId}/orders/{orderId}/delivered",
            new { deliveryPhotoKey = $"delivery-photos/{Guid.NewGuid():N}.jpg" });
        response.StatusCode.Should().Be(HttpStatusCode.OK);
    }

    private async Task EnsureDriverAvailableAsync(Guid driverId)
    {
        using IServiceScope scope = _fixture.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        Driver? driver = await db.Drivers.FirstOrDefaultAsync(x => x.Id == driverId);
        driver.Should().NotBeNull();
        driver!.SetStatus(DriverStatus.Available);
        await db.SaveChangesAsync();
    }

    private async Task VerifyFinalOrderAndDriverStateAsync(Guid orderId, Guid driverId)
    {
        using IServiceScope scope = _fixture.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();

        var order = await db.Orders.AsNoTracking().FirstAsync(x => x.Id == orderId);
        order.Status.Should().Be(OrderStatus.Delivered);

        Driver driver = await db.Drivers.AsNoTracking().FirstAsync(x => x.Id == driverId);
        driver.DriverStatus.Should().Be(DriverStatus.Available);
    }

    private sealed record ApiSuccess<T>(T? Data);
    private sealed record AuthTokensJson(string AccessToken, string RefreshToken, int KycStatus);

    private static async Task<AuthTokensJson?> ReadTokensAsync(HttpResponseMessage response)
    {
        ApiSuccess<AuthTokensJson>? envelope =
            await response.Content.ReadFromJsonAsync<ApiSuccess<AuthTokensJson>>(JsonOptions).ConfigureAwait(false);
        return envelope?.Data;
    }

    private static string ComputeSha512UpperHex(string value)
    {
        byte[] utf8 = Encoding.UTF8.GetBytes(value);
        byte[] hash = SHA512.HashData(utf8);
        return Convert.ToHexString(hash);
    }

}
