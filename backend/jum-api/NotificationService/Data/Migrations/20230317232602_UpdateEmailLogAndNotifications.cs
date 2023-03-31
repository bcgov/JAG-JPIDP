using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace NotificationService.Data.Migrations
{
    public partial class UpdateEmailLogAndNotifications : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "DomainEvent",
                schema: "notification",
                table: "Notifications",
                type: "nvarchar(255)",
                nullable: true);

            migrationBuilder.Sql("UPDATE [notification].[Notifications] SET [DomainEvent] = 'unset'");

            migrationBuilder.AlterColumn<string>(
                name: "DomainEvent",
                schema: "notification",
                table: "Notifications",
                type: "nvarchar(255)",
                nullable: false);

            migrationBuilder.AddColumn<string>(
                name: "EventData",
                schema: "notification",
                table: "Notifications",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "DeliveryMethod",
                schema: "notification",
                table: "Notifications",
                type: "nvarchar(100)",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "NotificationId",
                schema: "notification",
                table: "EmailLog",
                type: "uniqueidentifier",
                nullable: true);


            migrationBuilder.AddColumn<Guid>(
                name: "MessageKey",
                schema: "notification",
                table: "Notifications",
                nullable: true);

            migrationBuilder.DropColumn(
                name: "Body",
                schema: "notification",
                table: "EmailLog");

            migrationBuilder.DropColumn(
                name: "Subject",
                schema: "notification",
                table: "EmailLog");

            migrationBuilder.DropColumn(
                name: "Tag",
                schema: "notification",
                table: "EmailLog");

            migrationBuilder.AlterColumn<string>(
                name: "NotificationId",
                schema: "notification",
                table: "Notifications",
                type: "uniqueidentifier",
                nullable: false);


            migrationBuilder.RenameColumn(
                name: "MsgId",
                schema: "notification",
                table: "EmailLog",
                newName: "SentResponseId");

        }


        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AlterColumn<int>(
                name: "DomainEvent",
                schema: "notification",
                table: "Notifications",
                type: "nvarchar(255)",
                nullable: true);

            migrationBuilder.DropColumn(
                name: "DomainEvent",
                schema: "notification",
                table: "Notifications");

            migrationBuilder.DropColumn(
                name: "EventData",
                schema: "notification",
                table: "Notifications");

            migrationBuilder.DropColumn(
                name: "MessageKey",
                schema: "notification",
                table: "Notifications");

            migrationBuilder.DropColumn(
                name: "NotificationId",
                schema: "notification",
                table: "EmailLog");

            migrationBuilder.AddColumn<string>(
                name: "Body",
                schema: "notification",
                table: "EmailLog",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "Subject",
                schema: "notification",
                table: "EmailLog",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                  name: "Tag",
                  schema: "notification",
                  table: "EmailLog",
                  type: "nvarchar(max)",
                  nullable: true);


            migrationBuilder.RenameColumn(
                name: "SentResponseId",
                schema: "notification",
                table: "EmailLog",
                newName: "MsgId");
        }
    }
}
