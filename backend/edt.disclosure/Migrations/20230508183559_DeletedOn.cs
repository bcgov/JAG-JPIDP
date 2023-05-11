using Microsoft.EntityFrameworkCore.Migrations;
using NodaTime;

#nullable disable

namespace edt.disclosure.Migrations
{
    public partial class DeletedOn : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<Instant>(
                name: "DeletedOn",
                schema: "disclosure",
                table: "CourtLocationRequest",
                type: "timestamp with time zone",
                nullable: true);
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "DeletedOn",
                schema: "disclosure",
                table: "CourtLocationRequest");
        }
    }
}
