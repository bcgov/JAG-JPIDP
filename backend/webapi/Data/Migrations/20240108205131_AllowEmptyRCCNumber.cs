using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Pidp.Data.Migrations
{
    public partial class AllowEmptyRCCNumber : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AlterColumn<string>(
                name: "RCCNumber",
                table: "SubmittingAgencyRequest",
                type: "text",
                nullable: true,
                oldClrType: typeof(string),
                oldType: "text");
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AlterColumn<string>(
                name: "RCCNumber",
                table: "SubmittingAgencyRequest",
                type: "text",
                nullable: false,
                defaultValue: "",
                oldClrType: typeof(string),
                oldType: "text",
                oldNullable: true);
        }
    }
}
