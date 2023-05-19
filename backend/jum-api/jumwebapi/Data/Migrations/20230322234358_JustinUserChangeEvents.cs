using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace jumwebapi.Data.Migrations
{
    public partial class JustinUserChangeEvents : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "JustinUserChange",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    EventMessageId = table.Column<int>(type: "int", nullable: false),
                    PartId = table.Column<string>(type: "nvarchar(50)", nullable: false),
                    EventTime = table.Column<DateTime>(type: "datetime2", nullable: false),
                    EventType = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    Created = table.Column<DateTime>(type: "datetime2", nullable: false),
                    Completed = table.Column<DateTime>(type: "datetime2", nullable: true),
                    Modified = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_JustinUserChange", x => x.Id);
                });

            migrationBuilder.CreateIndex(
                name: "IX_JustinUserChange_EventMessageId",
                table: "JustinUserChange",
                column: "EventMessageId",
                unique: true);

            migrationBuilder.CreateTable(
                name: "JustinUserChangeTarget",
                columns: table => new
                {
                    ChangeTargetId = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    ServiceName = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    ChangeStatus = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    ErrorDetails = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    CompletedTime = table.Column<DateTime>(type: "datetime2", nullable: false),
                    JustinUserChangeId = table.Column<int>(type: "int", nullable: false),
                    Created = table.Column<DateTime>(type: "datetime2", nullable: false),
                    Modified = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_JustinUserChangeTarget", x => x.ChangeTargetId);
                    table.ForeignKey(
                        name: "FK_JustinUserChangeTarget_JustinUserChange_JustinUserChangeId",
                        column: x => x.JustinUserChangeId,
                        principalTable: "JustinUserChange",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_JustinUserChangeTarget_JustinUserChangeId",
                table: "JustinUserChangeTarget",
                column: "JustinUserChangeId");
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "JustinUserChangeTarget");

            migrationBuilder.DropTable(
                name: "JustinUserChange");
        }
    }
}
