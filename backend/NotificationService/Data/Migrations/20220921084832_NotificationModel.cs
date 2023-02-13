using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace NotificationService.Data.Migrations
{
    public partial class NotificationModel : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.EnsureSchema(
                name: "notification");

            migrationBuilder.RenameTable(
                name: "Notifications",
                newName: "Notifications",
                newSchema: "notification");

            migrationBuilder.RenameTable(
                name: "IdempotentConsumers",
                newName: "IdempotentConsumers",
                newSchema: "notification");

            migrationBuilder.RenameTable(
                name: "EmailLog",
                newName: "EmailLog",
                newSchema: "notification");

            migrationBuilder.AlterColumn<string>(
                name: "PartId",
                schema: "notification",
                table: "Notifications",
                type: "nvarchar(max)",
                nullable: false,
                oldClrType: typeof(long),
                oldType: "bigint");

            migrationBuilder.AddColumn<string>(
                name: "Tag",
                schema: "notification",
                table: "EmailLog",
                type: "nvarchar(max)",
                nullable: false,
                defaultValue: "");
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "Tag",
                schema: "notification",
                table: "EmailLog");

            migrationBuilder.RenameTable(
                name: "Notifications",
                schema: "notification",
                newName: "Notifications");

            migrationBuilder.RenameTable(
                name: "IdempotentConsumers",
                schema: "notification",
                newName: "IdempotentConsumers");

            migrationBuilder.RenameTable(
                name: "EmailLog",
                schema: "notification",
                newName: "EmailLog");

            migrationBuilder.AlterColumn<long>(
                name: "PartId",
                table: "Notifications",
                type: "bigint",
                nullable: false,
                oldClrType: typeof(string),
                oldType: "nvarchar(max)");
        }
    }
}
