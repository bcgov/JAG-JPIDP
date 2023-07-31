using System;
using Microsoft.EntityFrameworkCore.Migrations;
using NodaTime;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

namespace NotificationService.Migrations
{
    public partial class initial : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.EnsureSchema(
                name: "notification");

            migrationBuilder.CreateTable(
                name: "EmailLog",
                schema: "notification",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    SendType = table.Column<string>(type: "text", nullable: false),
                    NotificationId = table.Column<Guid>(type: "uuid", nullable: true),
                    SentResponseId = table.Column<Guid>(type: "uuid", nullable: true),
                    SentTo = table.Column<string>(type: "text", nullable: false),
                    Cc = table.Column<string>(type: "text", nullable: false),
                    DateSent = table.Column<Instant>(type: "timestamp with time zone", nullable: true),
                    LatestStatus = table.Column<string>(type: "text", nullable: true),
                    StatusMessage = table.Column<string>(type: "text", nullable: true),
                    UpdateCount = table.Column<int>(type: "integer", nullable: false),
                    Created = table.Column<Instant>(type: "timestamp with time zone", nullable: false),
                    Modified = table.Column<Instant>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_EmailLog", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "IdempotentConsumers",
                schema: "notification",
                columns: table => new
                {
                    MessageId = table.Column<string>(type: "text", nullable: false),
                    Consumer = table.Column<string>(type: "text", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_IdempotentConsumers", x => new { x.MessageId, x.Consumer });
                });

            migrationBuilder.CreateTable(
                name: "Notifications",
                schema: "notification",
                columns: table => new
                {
                    NotificationId = table.Column<Guid>(type: "uuid", nullable: false),
                    EmailAddress = table.Column<string>(type: "text", nullable: false),
                    Status = table.Column<string>(type: "text", nullable: false),
                    PartId = table.Column<string>(type: "text", nullable: false),
                    Consumer = table.Column<string>(type: "text", nullable: false),
                    AccessRequestId = table.Column<int>(type: "integer", nullable: false),
                    DomainEvent = table.Column<string>(type: "text", nullable: false),
                    Details = table.Column<string>(type: "text", nullable: false),
                    EventData = table.Column<string>(type: "text", nullable: false),
                    Subject = table.Column<int>(type: "integer", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Notifications", x => new { x.NotificationId, x.EmailAddress });
                });

            migrationBuilder.CreateTable(
                name: "TemplateGenerator",
                schema: "notification",
                columns: table => new
                {
                    TemplateName = table.Column<string>(type: "text", nullable: false),
                    Type = table.Column<string>(type: "text", nullable: false),
                    TemplateId = table.Column<Guid>(type: "uuid", nullable: false),
                    ServiceType = table.Column<string>(type: "text", nullable: false),
                    Extension = table.Column<string>(type: "text", nullable: false),
                    Description = table.Column<string>(type: "text", nullable: true),
                    TemplateUrl = table.Column<string>(type: "text", nullable: false),
                    Created = table.Column<Instant>(type: "timestamp with time zone", nullable: false),
                    Modified = table.Column<Instant>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_TemplateGenerator", x => new { x.TemplateName, x.Type });
                });
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "EmailLog",
                schema: "notification");

            migrationBuilder.DropTable(
                name: "IdempotentConsumers",
                schema: "notification");

            migrationBuilder.DropTable(
                name: "Notifications",
                schema: "notification");

            migrationBuilder.DropTable(
                name: "TemplateGenerator",
                schema: "notification");
        }
    }
}
