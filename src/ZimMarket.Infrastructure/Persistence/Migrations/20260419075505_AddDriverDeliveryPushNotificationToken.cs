using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ZimMarket.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddDriverDeliveryPushNotificationToken : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "driver_push_notification_token",
                table: "users",
                type: "character varying(512)",
                maxLength: 512,
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "driver_push_notification_token",
                table: "users");
        }
    }
}
