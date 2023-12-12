using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace edt.service.Migrations
{
    public partial class RetryColumnAdd : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "RetryCount",
                schema: "edt",
                table: "PersonFolioLinkage",
                type: "integer",
                nullable: false,
                defaultValue: 0);
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "RetryCount",
                schema: "edt",
                table: "PersonFolioLinkage");
        }
    }
}
