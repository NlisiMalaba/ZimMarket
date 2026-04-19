using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ZimMarket.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddProductSuspensionReason : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "suspension_reason",
                table: "products",
                type: "character varying(2000)",
                maxLength: 2000,
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "suspension_reason",
                table: "products");
        }
    }
}
