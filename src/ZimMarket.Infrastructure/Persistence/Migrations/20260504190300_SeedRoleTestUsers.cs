using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;
using ZimMarket.Domain.Entities.Users;
using ZimMarket.Domain.Enums;
using ZimMarket.Domain.ValueObjects;
using ZimMarket.Shared;

#nullable disable

namespace ZimMarket.Infrastructure.Persistence.Migrations;

/// <summary>
/// Dev/testing seed for one account per role.
/// Opt-in only: set <c>ZIMMARKET_SEED_TEST_USERS=true</c> in the environment or repository root <c>.env</c> before this migration runs.
/// Optional password override: <c>ZIMMARKET_TEST_USERS_PASSWORD</c> (default: TestPass123!).
/// </summary>
/// <remarks>
/// If this migration was already applied without the flag, <c>Up</c> did nothing but EF still recorded it.
/// Remove <c>20260504190300_SeedRoleTestUsers</c> from <c>__EFMigrationsHistory</c>, set the flag, then run <c>dotnet ef database update</c> again.
/// </remarks>
[DbContext(typeof(AppDbContext))]
[Migration("20260504190300_SeedRoleTestUsers")]
public partial class SeedRoleTestUsers : Migration
{
    private static readonly DateTimeOffset SeedTimestamp = new(2026, 5, 4, 0, 0, 0, TimeSpan.Zero);

    private static readonly Guid CustomerUserId = Guid.Parse("d0000000-0000-4000-8000-000000000001");
    private static readonly Guid SellerUserId = Guid.Parse("d0000000-0000-4000-8000-000000000002");
    private static readonly Guid DriverUserId = Guid.Parse("d0000000-0000-4000-8000-000000000003");
    private static readonly Guid AdminUserId = Guid.Parse("d0000000-0000-4000-8000-000000000004");
    private static readonly Guid SuperAdminUserId = Guid.Parse("d0000000-0000-4000-8000-000000000005");

    protected override void Up(MigrationBuilder migrationBuilder)
    {
        // Migrations do not load .env automatically; align with design-time factory and Docker Compose.
        ZimMarket.Infrastructure.Persistence.RepositoryDotEnv.TryApply();

        if (!ShouldSeedTestUsers())
            return;

        string password = ResolveTestPassword();

        string customerHash = HashPassword(CreateCustomerStub(), password);
        string sellerHash = HashPassword(CreateSellerStub(), password);
        string driverHash = HashPassword(CreateDriverStub(), password);
        string adminHash = HashPassword(CreateAdminStub(), password);
        string superAdminHash = HashPassword(CreateSuperAdminStub(), password);

        migrationBuilder.Sql(
            $"""
            INSERT INTO users (
                "Id","Email","FullName","phone_number","PasswordHash","Role","KycStatus","IsActive","user_type",
                "CreatedAt","UpdatedAt","security_stamp",
                "PushNotificationToken",
                "BusinessName","NationalIdDocumentKey","ProofOfResidenceDocumentKey","IsApproved","RejectionReason",
                "LicenseNumber","LicenseDocumentKey","VehicleRegistration","VehicleDocumentKey","DriverStatus","Driver_IsApproved","Driver_RejectionReason","driver_push_notification_token"
            ) VALUES
            ({Q(CustomerUserId)}, 'customer.test@zimmarket.local', 'Test Customer', '+263770000201', {Q(customerHash)}, {(int)UserRole.Customer}, {(int)KycStatus.Approved}, TRUE, {(int)UserRole.Customer}, {Q(SeedTimestamp)}, {Q(SeedTimestamp)}, 'seed-customer-role-test-users', NULL, NULL, NULL, NULL, NULL, NULL, NULL, NULL, NULL, NULL, NULL, NULL, NULL, NULL),
            ({Q(SellerUserId)}, 'seller.test@zimmarket.local', 'Test Seller', '+263770000202', {Q(sellerHash)}, {(int)UserRole.Seller}, {(int)KycStatus.Approved}, TRUE, {(int)UserRole.Seller}, {Q(SeedTimestamp)}, {Q(SeedTimestamp)}, 'seed-seller-role-test-users', NULL, 'Test Seller Store', 'seed/seller/national-id.pdf', 'seed/seller/proof-residence.pdf', TRUE, NULL, NULL, NULL, NULL, NULL, NULL, NULL, NULL, NULL),
            ({Q(DriverUserId)}, 'driver.test@zimmarket.local', 'Test Driver', '+263770000203', {Q(driverHash)}, {(int)UserRole.Driver}, {(int)KycStatus.Approved}, TRUE, {(int)UserRole.Driver}, {Q(SeedTimestamp)}, {Q(SeedTimestamp)}, 'seed-driver-role-test-users', NULL, NULL, NULL, NULL, NULL, NULL, 'TEST-LIC-ROLE-0001', 'seed/driver/license.pdf', 'TEST-VEH-ROLE-0001', 'seed/driver/vehicle.pdf', {(int)DriverStatus.Offline}, TRUE, NULL, NULL),
            ({Q(AdminUserId)}, 'admin.test@zimmarket.local', 'Test Admin', '+263770000204', {Q(adminHash)}, {(int)UserRole.Admin}, {(int)KycStatus.Approved}, TRUE, {(int)UserRole.Admin}, {Q(SeedTimestamp)}, {Q(SeedTimestamp)}, 'seed-admin-role-test-users', NULL, NULL, NULL, NULL, NULL, NULL, NULL, NULL, NULL, NULL, NULL, NULL, NULL, NULL),
            ({Q(SuperAdminUserId)}, 'superadmin.test@zimmarket.local', 'Test Super Admin', '+263770000205', {Q(superAdminHash)}, {(int)UserRole.SuperAdmin}, {(int)KycStatus.Approved}, TRUE, {(int)UserRole.SuperAdmin}, {Q(SeedTimestamp)}, {Q(SeedTimestamp)}, 'seed-superadmin-role-test-users', NULL, NULL, NULL, NULL, NULL, NULL, NULL, NULL, NULL, NULL, NULL, NULL, NULL, NULL)
            ON CONFLICT ("Id") DO NOTHING;
            """);

        migrationBuilder.Sql(
            $"""
            INSERT INTO admin_approval_states ("Id","UserId","EmailVerifiedAt","ApprovedAt","ApprovedByUserId","CreatedAt","UpdatedAt")
            VALUES ({Q(AdminUserId)},{Q(AdminUserId)},{Q(SeedTimestamp)},{Q(SeedTimestamp)},{Q(SuperAdminUserId)},{Q(SeedTimestamp)},{Q(SeedTimestamp)})
            ON CONFLICT ("Id") DO NOTHING;
            """);
    }

    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.Sql($"DELETE FROM admin_approval_states WHERE \"Id\" = {Q(AdminUserId)};");
        migrationBuilder.Sql(
            $"""
            DELETE FROM users
            WHERE "Id" IN ({Q(CustomerUserId)}, {Q(SellerUserId)}, {Q(DriverUserId)}, {Q(AdminUserId)}, {Q(SuperAdminUserId)});
            """);
    }

    private static bool ShouldSeedTestUsers()
    {
        string raw = Environment.GetEnvironmentVariable("ZIMMARKET_SEED_TEST_USERS") ?? string.Empty;
        raw = raw.Trim();
        if (raw.Length == 0)
            return false;

        return string.Equals(raw, "true", StringComparison.OrdinalIgnoreCase)
            || string.Equals(raw, "1", StringComparison.OrdinalIgnoreCase)
            || string.Equals(raw, "yes", StringComparison.OrdinalIgnoreCase);
    }

    private static string ResolveTestPassword()
    {
        string raw = Environment.GetEnvironmentVariable("ZIMMARKET_TEST_USERS_PASSWORD");
        return string.IsNullOrWhiteSpace(raw) ? "TestPass123!" : raw.Trim();
    }

    private static string HashPassword(User user, string plainTextPassword)
    {
        var hasher = new PasswordHasher<User>();
        return hasher.HashPassword(user, plainTextPassword);
    }

    private static Customer CreateCustomerStub()
    {
        return new Customer(
            CustomerUserId,
            "customer.test@zimmarket.local",
            "Test Customer",
            ParsePhone("+263770000201"),
            "TEMP",
            KycStatus.Approved,
            true,
            null,
            null,
            SeedTimestamp,
            SeedTimestamp);
    }

    private static Seller CreateSellerStub()
    {
        return new Seller(
            SellerUserId,
            "seller.test@zimmarket.local",
            "Test Seller",
            ParsePhone("+263770000202"),
            "TEMP",
            KycStatus.Approved,
            true,
            null,
            null,
            SeedTimestamp,
            SeedTimestamp,
            "Test Seller Store",
            "seed/seller/national-id.pdf",
            "seed/seller/proof-residence.pdf",
            true,
            null);
    }

    private static Driver CreateDriverStub()
    {
        return new Driver(
            DriverUserId,
            "driver.test@zimmarket.local",
            "Test Driver",
            ParsePhone("+263770000203"),
            "TEMP",
            KycStatus.Approved,
            true,
            null,
            null,
            SeedTimestamp,
            SeedTimestamp,
            "TEST-LIC-ROLE-0001",
            "seed/driver/license.pdf",
            "TEST-VEH-ROLE-0001",
            "seed/driver/vehicle.pdf",
            DriverStatus.Offline,
            null,
            true,
            null,
            null);
    }

    private static AdminUser CreateAdminStub()
    {
        return new AdminUser(
            AdminUserId,
            "admin.test@zimmarket.local",
            "Test Admin",
            ParsePhone("+263770000204"),
            "TEMP",
            KycStatus.Approved,
            true,
            null,
            null,
            SeedTimestamp,
            SeedTimestamp);
    }

    private static SuperAdminUser CreateSuperAdminStub()
    {
        return new SuperAdminUser(
            SuperAdminUserId,
            "superadmin.test@zimmarket.local",
            "Test Super Admin",
            ParsePhone("+263770000205"),
            "TEMP",
            KycStatus.Approved,
            true,
            null,
            null,
            SeedTimestamp,
            SeedTimestamp);
    }

    private static PhoneNumber ParsePhone(string rawPhone)
    {
        Result<PhoneNumber> parsed = PhoneNumber.Create(rawPhone);
        if (parsed.IsFailure || parsed.Value is null)
            throw new InvalidOperationException($"Invalid seed phone number: {rawPhone}");

        return parsed.Value;
    }

    private static string Q(Guid value) => $"'{value:D}'";

    private static string Q(DateTimeOffset value) => $"'{value:O}'";

    private static string Q(string value) => $"'{value.Replace("'", "''", StringComparison.Ordinal)}'";
}
