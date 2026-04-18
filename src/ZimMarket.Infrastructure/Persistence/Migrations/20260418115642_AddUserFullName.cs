using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ZimMarket.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddUserFullName : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "FullName",
                table: "users",
                type: "character varying(200)",
                maxLength: 200,
                nullable: false,
                defaultValue: "");

            migrationBuilder.Sql(
                """
                UPDATE users SET "FullName" = 'Super Admin' WHERE "Id" = 'b0000000-0000-4000-8000-000000000001';
                """);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "FullName",
                table: "users");
        }
    }
}
