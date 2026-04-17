using System.Globalization;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore.Migrations;
using ZimMarket.Domain.Entities.Users;
using ZimMarket.Domain.Enums;
using ZimMarket.Domain.ValueObjects;
using ZimMarket.Shared;

#nullable disable

namespace ZimMarket.Infrastructure.Persistence.Migrations;

/// <inheritdoc />
/// <summary>
/// Seed data applied when this migration runs (values are resolved at <c>database update</c> time).
/// <para><b>SuperAdmin</b> (required unless you remove this block): set <c>ZIMMARKET_SUPERADMIN_PASSWORD</c> before applying.
/// Optional: <c>ZIMMARKET_SUPERADMIN_EMAIL</c>, <c>ZIMMARKET_SUPERADMIN_PHONE</c> (E.164, e.g. +2637XXXXXXXX).</para>
/// <para><b>Exchange rate</b>: optional <c>ZIMMARKET_INITIAL_USD_ZWG_RATE</c> (decimal, invariant culture); default <c>26</c>.</para>
/// </summary>
public partial class SeedDefaultData : Migration
{
    private static readonly DateTimeOffset SeedTimestamp = new(2026, 1, 1, 0, 0, 0, TimeSpan.Zero);

    private static readonly Guid CategoryElectronicsId = Guid.Parse("a0000000-0000-4000-8000-000000000001");
    private static readonly Guid CategoryClothingId = Guid.Parse("a0000000-0000-4000-8000-000000000002");
    private static readonly Guid CategoryFoodId = Guid.Parse("a0000000-0000-4000-8000-000000000003");
    private static readonly Guid CategoryHomeGardenId = Guid.Parse("a0000000-0000-4000-8000-000000000004");
    private static readonly Guid CategoryAgricultureId = Guid.Parse("a0000000-0000-4000-8000-000000000005");
    private static readonly Guid CategoryOtherId = Guid.Parse("a0000000-0000-4000-8000-000000000006");

    private static readonly Guid SuperAdminUserId = Guid.Parse("b0000000-0000-4000-8000-000000000001");
    private static readonly Guid InitialExchangeRateId = Guid.Parse("c0000000-0000-4000-8000-000000000001");

    /// <inheritdoc />
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.InsertData(
            table: "categories",
            columns: ["Id", "Name", "Slug", "ParentCategoryId", "CreatedAt", "UpdatedAt"],
            values: new object[] { CategoryElectronicsId, "Electronics", "electronics", null, SeedTimestamp, SeedTimestamp });

        migrationBuilder.InsertData(
            table: "categories",
            columns: ["Id", "Name", "Slug", "ParentCategoryId", "CreatedAt", "UpdatedAt"],
            values: new object[] { CategoryClothingId, "Clothing", "clothing", null, SeedTimestamp, SeedTimestamp });

        migrationBuilder.InsertData(
            table: "categories",
            columns: ["Id", "Name", "Slug", "ParentCategoryId", "CreatedAt", "UpdatedAt"],
            values: new object[] { CategoryFoodId, "Food", "food", null, SeedTimestamp, SeedTimestamp });

        migrationBuilder.InsertData(
            table: "categories",
            columns: ["Id", "Name", "Slug", "ParentCategoryId", "CreatedAt", "UpdatedAt"],
            values: new object[] { CategoryHomeGardenId, "Home & Garden", "home-garden", null, SeedTimestamp, SeedTimestamp });

        migrationBuilder.InsertData(
            table: "categories",
            columns: ["Id", "Name", "Slug", "ParentCategoryId", "CreatedAt", "UpdatedAt"],
            values: new object[] { CategoryAgricultureId, "Agriculture", "agriculture", null, SeedTimestamp, SeedTimestamp });

        migrationBuilder.InsertData(
            table: "categories",
            columns: ["Id", "Name", "Slug", "ParentCategoryId", "CreatedAt", "UpdatedAt"],
            values: new object[] { CategoryOtherId, "Other", "other", null, SeedTimestamp, SeedTimestamp });

        string superAdminEmail = Environment.GetEnvironmentVariable("ZIMMARKET_SUPERADMIN_EMAIL");
        superAdminEmail = string.IsNullOrWhiteSpace(superAdminEmail)
            ? "superadmin@zimmarket.local"
            : superAdminEmail.Trim();

        string superAdminPhoneRaw = Environment.GetEnvironmentVariable("ZIMMARKET_SUPERADMIN_PHONE");
        superAdminPhoneRaw = string.IsNullOrWhiteSpace(superAdminPhoneRaw)
            ? "+263770000001"
            : superAdminPhoneRaw.Trim();

        Result<PhoneNumber> phoneResult = PhoneNumber.Create(superAdminPhoneRaw);
        if (phoneResult.IsFailure)
        {
            throw new InvalidOperationException(
                $"Invalid ZIMMARKET_SUPERADMIN_PHONE: {string.Join("; ", phoneResult.Errors)}");
        }

        string plainPassword = Environment.GetEnvironmentVariable("ZIMMARKET_SUPERADMIN_PASSWORD");
        if (string.IsNullOrWhiteSpace(plainPassword))
        {
            throw new InvalidOperationException(
                "Set environment variable ZIMMARKET_SUPERADMIN_PASSWORD before applying migration SeedDefaultData " +
                "(plain text is hashed once with ASP.NET Core PasswordHasher and never stored).");
        }

        var superAdminStub = new SuperAdminUser(
            SuperAdminUserId,
            superAdminEmail,
            phoneResult.Value!,
            passwordHash: "TEMP",
            KycStatus.Approved,
            isActive: true,
            refreshTokenHash: null,
            refreshTokenExpiry: null,
            SeedTimestamp,
            SeedTimestamp);

        string passwordHash = new PasswordHasher<SuperAdminUser>().HashPassword(superAdminStub, plainPassword.Trim());

        migrationBuilder.InsertData(
            table: "users",
            columns:
            [
                "Id",
                "Email",
                "phone_number",
                "PasswordHash",
                "Role",
                "KycStatus",
                "IsActive",
                "user_type",
                "CreatedAt",
                "UpdatedAt"
            ],
            values: new object[]
            {
                SuperAdminUserId,
                superAdminEmail,
                phoneResult.Value!.Value,
                passwordHash,
                (int)UserRole.SuperAdmin,
                (int)KycStatus.Approved,
                true,
                (int)UserRole.SuperAdmin,
                SeedTimestamp,
                SeedTimestamp
            });

        string rateEnv = Environment.GetEnvironmentVariable("ZIMMARKET_INITIAL_USD_ZWG_RATE");
        decimal usdToZwg = string.IsNullOrWhiteSpace(rateEnv)
            ? 26m
            : decimal.Parse(rateEnv, CultureInfo.InvariantCulture);

        migrationBuilder.InsertData(
            table: "exchange_rates",
            columns: ["Id", "BaseCurrency", "QuoteCurrency", "Rate", "EffectiveAt", "CreatedAt", "UpdatedAt"],
            values: new object[]
            {
                InitialExchangeRateId,
                "USD",
                "ZWG",
                usdToZwg,
                SeedTimestamp,
                SeedTimestamp,
                SeedTimestamp
            });
    }

    /// <inheritdoc />
    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.DeleteData(table: "exchange_rates", keyColumn: "Id", keyValue: InitialExchangeRateId);
        migrationBuilder.DeleteData(table: "users", keyColumn: "Id", keyValue: SuperAdminUserId);
        migrationBuilder.DeleteData(table: "categories", keyColumn: "Id", keyValue: CategoryElectronicsId);
        migrationBuilder.DeleteData(table: "categories", keyColumn: "Id", keyValue: CategoryClothingId);
        migrationBuilder.DeleteData(table: "categories", keyColumn: "Id", keyValue: CategoryFoodId);
        migrationBuilder.DeleteData(table: "categories", keyColumn: "Id", keyValue: CategoryHomeGardenId);
        migrationBuilder.DeleteData(table: "categories", keyColumn: "Id", keyValue: CategoryAgricultureId);
        migrationBuilder.DeleteData(table: "categories", keyColumn: "Id", keyValue: CategoryOtherId);
    }
}
