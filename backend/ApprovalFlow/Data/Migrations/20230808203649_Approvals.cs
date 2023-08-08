using Microsoft.EntityFrameworkCore.Migrations;
using NodaTime;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

namespace ApprovalFlow.Data.Migrations
{
    public partial class Approvals : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.EnsureSchema(
                name: "approval");

            migrationBuilder.CreateTable(
                name: "ApprovalRequest",
                schema: "approval",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    AccessRequestId = table.Column<int>(type: "integer", nullable: false),
                    Reason = table.Column<string>(type: "text", nullable: false),
                    Approved = table.Column<Instant>(type: "timestamp with time zone", nullable: true),
                    Requested = table.Column<Instant>(type: "timestamp with time zone", nullable: true),
                    Created = table.Column<Instant>(type: "timestamp with time zone", nullable: false),
                    Modified = table.Column<Instant>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ApprovalRequest", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "ApprovalHistory",
                schema: "approval",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    DecisionNote = table.Column<string>(type: "text", nullable: false),
                    Approver = table.Column<string>(type: "text", nullable: false),
                    ApprovalRequestId = table.Column<int>(type: "integer", nullable: false),
                    Deleted = table.Column<Instant>(type: "timestamp with time zone", nullable: true),
                    Status = table.Column<int>(type: "integer", nullable: false),
                    Created = table.Column<Instant>(type: "timestamp with time zone", nullable: false),
                    Modified = table.Column<Instant>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ApprovalHistory", x => x.Id);
                    table.ForeignKey(
                        name: "FK_ApprovalHistory_ApprovalRequest_ApprovalRequestId",
                        column: x => x.ApprovalRequestId,
                        principalSchema: "approval",
                        principalTable: "ApprovalRequest",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_ApprovalHistory_ApprovalRequestId",
                schema: "approval",
                table: "ApprovalHistory",
                column: "ApprovalRequestId");
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "ApprovalHistory",
                schema: "approval");

            migrationBuilder.DropTable(
                name: "ApprovalRequest",
                schema: "approval");

        }
    }
}
