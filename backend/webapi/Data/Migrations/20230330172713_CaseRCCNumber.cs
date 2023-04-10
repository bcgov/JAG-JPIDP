using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Pidp.Data.Migrations
{
    public partial class CaseRCCNumber : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "RCCNumber",
                table: "SubmittingAgencyRequest",
                type: "text",
                nullable: false,
                defaultValue: "");

            migrationBuilder.CreateIndex(
                name: "IX_RCCNumber",
                table: "SubmittingAgencyRequest",
                column: "RCCNumber",
                unique: false);
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_RCCNumber",
                table: "SubmittingAgencyRequest");

            migrationBuilder.DropColumn(
                name: "RCCNumber",
                table: "SubmittingAgencyRequest");
        }
    }
}
