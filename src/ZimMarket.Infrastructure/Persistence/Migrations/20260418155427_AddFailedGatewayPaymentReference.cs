using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ZimMarket.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddFailedGatewayPaymentReference : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "FailedGatewayPaymentReference",
                table: "orders",
                type: "character varying(200)",
                maxLength: 200,
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "FailedGatewayPaymentReference",
                table: "orders");
        }
    }
}
