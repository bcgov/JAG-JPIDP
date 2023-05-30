using Microsoft.EntityFrameworkCore.Migrations;
using NodaTime;

#nullable disable

namespace Pidp.Data.Migrations
{
    public partial class SAClientExpiry : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<LocalDate>(
                name: "ClientCertExpiry",
                table: "SubmittingAgencyLookup",
                type: "date",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "LevelOfAssurance",
                table: "SubmittingAgencyLookup",
                type: "integer",
                nullable: true);
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "ClientCertExpiry",
                table: "SubmittingAgencyLookup");

            migrationBuilder.DropColumn(
                name: "LevelOfAssurance",
                table: "SubmittingAgencyLookup");
        }
    }
}
