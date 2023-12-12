using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace edt.service.Migrations
{
    public partial class FolioEdtExternalId : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "EdtExternalId",
                schema: "edt",
                table: "PersonFolioLinkage",
                type: "text",
                nullable: false,
                defaultValue: "");
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "EdtExternalId",
                schema: "edt",
                table: "PersonFolioLinkage");
        }
    }
}
