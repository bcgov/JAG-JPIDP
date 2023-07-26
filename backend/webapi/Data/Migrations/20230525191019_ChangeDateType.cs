using Microsoft.EntityFrameworkCore.Migrations;
using NodaTime;

#nullable disable

namespace Pidp.Data.Migrations
{
    public partial class ChangeDateType : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AlterColumn<Instant>(
                name: "ClientCertExpiry",
                table: "SubmittingAgencyLookup",
                type: "timestamp with time zone",
                nullable: true,
                oldClrType: typeof(LocalDate),
                oldType: "date",
                oldNullable: true);
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AlterColumn<LocalDate>(
                name: "ClientCertExpiry",
                table: "SubmittingAgencyLookup",
                type: "date",
                nullable: true,
                oldClrType: typeof(Instant),
                oldType: "timestamp with time zone",
                oldNullable: true);
        }
    }
}
