using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Pidp.Data.Migrations
{
    public partial class CourtMessageId : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<Guid>(
                name: "MessageId",
                table: "CourtLocationAccessRequest",
                type: "uuid",
                nullable: true);

            migrationBuilder.AlterColumn<string>(
                name: "Alias",
                table: "CourtLocation",
                type: "text",
                nullable: true,
                oldClrType: typeof(string),
                oldType: "text");
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "MessageId",
                table: "CourtLocationAccessRequest");

            migrationBuilder.AlterColumn<string>(
                name: "Alias",
                table: "CourtLocation",
                type: "text",
                nullable: false,
                defaultValue: "",
                oldClrType: typeof(string),
                oldType: "text",
                oldNullable: true);
        }
    }
}
