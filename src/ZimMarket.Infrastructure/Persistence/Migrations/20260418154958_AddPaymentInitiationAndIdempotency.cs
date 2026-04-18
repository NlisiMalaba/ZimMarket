using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ZimMarket.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddPaymentInitiationAndIdempotency : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "InitiatedPaymentMethod",
                table: "orders",
                type: "integer",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "PaymentGatewayReference",
                table: "orders",
                type: "character varying(500)",
                maxLength: 500,
                nullable: true);

            migrationBuilder.CreateTable(
                name: "payment_idempotency_records",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    IdempotencyKey = table.Column<string>(type: "character varying(128)", maxLength: 128, nullable: false),
                    OrderId = table.Column<Guid>(type: "uuid", nullable: false),
                    CustomerId = table.Column<Guid>(type: "uuid", nullable: false),
                    GatewayReference = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: false),
                    PaymentUrl = table.Column<string>(type: "character varying(2000)", maxLength: 2000, nullable: false),
                    PaymentMethod = table.Column<int>(type: "integer", nullable: false),
                    CreatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_payment_idempotency_records", x => x.Id);
                });

            migrationBuilder.CreateIndex(
                name: "IX_payment_idempotency_records_IdempotencyKey",
                table: "payment_idempotency_records",
                column: "IdempotencyKey",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_payment_idempotency_records_OrderId",
                table: "payment_idempotency_records",
                column: "OrderId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "payment_idempotency_records");

            migrationBuilder.DropColumn(
                name: "InitiatedPaymentMethod",
                table: "orders");

            migrationBuilder.DropColumn(
                name: "PaymentGatewayReference",
                table: "orders");
        }
    }
}
