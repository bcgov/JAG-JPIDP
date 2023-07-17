using System;
using Microsoft.EntityFrameworkCore.Migrations;
using NodaTime;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

namespace edt.service.Migrations
{
    public partial class initial : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.EnsureSchema(
                name: "edt");

            migrationBuilder.CreateTable(
                name: "EmailLog",
                schema: "edt",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    SendType = table.Column<string>(type: "text", nullable: false),
                    MsgId = table.Column<Guid>(type: "uuid", nullable: true),
                    SentTo = table.Column<string>(type: "text", nullable: false),
                    Cc = table.Column<string>(type: "text", nullable: false),
                    Subject = table.Column<string>(type: "text", nullable: false),
                    Body = table.Column<string>(type: "text", nullable: false),
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
                name: "FailedEventLogs",
                schema: "edt",
                columns: table => new
                {
                    EventId = table.Column<string>(type: "text", nullable: false),
                    Producer = table.Column<string>(type: "text", nullable: true),
                    ConsumerGroupId = table.Column<string>(type: "text", nullable: true),
                    ConsumerId = table.Column<string>(type: "text", nullable: true),
                    Created = table.Column<Instant>(type: "timestamp with time zone", nullable: false),
                    Modified = table.Column<Instant>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_FailedEventLogs", x => x.EventId);
                });

            migrationBuilder.CreateTable(
                name: "IdempotentConsumers",
                schema: "edt",
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
                schema: "edt",
                columns: table => new
                {
                    NotificationId = table.Column<Guid>(type: "uuid", nullable: false),
                    EmailAddress = table.Column<string>(type: "text", nullable: false),
                    Status = table.Column<string>(type: "text", nullable: false),
                    PartId = table.Column<string>(type: "text", nullable: false),
                    Consumer = table.Column<string>(type: "text", nullable: false),
                    AccessRequestId = table.Column<int>(type: "integer", nullable: false),
                    Subject = table.Column<int>(type: "integer", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Notifications", x => new { x.NotificationId, x.EmailAddress });
                });
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "EmailLog",
                schema: "edt");

            migrationBuilder.DropTable(
                name: "FailedEventLogs",
                schema: "edt");

            migrationBuilder.DropTable(
                name: "IdempotentConsumers",
                schema: "edt");

            migrationBuilder.DropTable(
                name: "Notifications",
                schema: "edt");
        }
    }
}
