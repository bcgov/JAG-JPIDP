using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace edt.casemanagement.Migrations
{
    public partial class CaseMgmtAdditionalTracing : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "Details",
                schema: "casemgmt",
                table: "CaseRequest",
                type: "text",
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "Party",
                schema: "casemgmt",
                table: "CaseRequest",
                type: "text",
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "Status",
                schema: "casemgmt",
                table: "CaseRequest",
                type: "text",
                nullable: false,
                defaultValue: "");
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "Details",
                schema: "casemgmt",
                table: "CaseRequest");

            migrationBuilder.DropColumn(
                name: "Party",
                schema: "casemgmt",
                table: "CaseRequest");

            migrationBuilder.DropColumn(
                name: "Status",
                schema: "casemgmt",
                table: "CaseRequest");
        }
    }
}
