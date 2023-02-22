using Microsoft.EntityFrameworkCore.Migrations;
using NodaTime;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

namespace Pidp.Data.Migrations
{
    public partial class SubmittingAgency : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "SubmittingAgencyLookup",
                columns: table => new
                {
                    Code = table.Column<int>(type: "integer", nullable: false),
                    Name = table.Column<string>(type: "text", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_SubmittingAgencyLookup", x => x.Code);
                });

            migrationBuilder.CreateTable(
                name: "SubmittingAgencyRequests",
                columns: table => new
                {
                    RequestId = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    PartyId = table.Column<int>(type: "integer", nullable: false),
                    CaseNumber = table.Column<string>(type: "text", nullable: false),
                    AgencyCode = table.Column<string>(type: "text", nullable: false),
                    RequestedOn = table.Column<Instant>(type: "timestamp with time zone", nullable: false),
                    RequestStatus = table.Column<string>(type: "text", nullable: false),
                    Created = table.Column<Instant>(type: "timestamp with time zone", nullable: false),
                    Modified = table.Column<Instant>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_SubmittingAgencyRequests", x => x.RequestId);
                });

            migrationBuilder.InsertData(
                table: "OrganizationLookup",
                columns: new[] { "Code", "Name" },
                values: new object[,]
                {
                    { 9, "Victoria Police Department" },
                    { 10, "Delta Police Department" },
                    { 11, "Saanich Police Department" }
                });

            migrationBuilder.InsertData(
                table: "SubmittingAgencyLookup",
                columns: new[] { "Code", "Name" },
                values: new object[,]
                {
                    { 0, "Victoria Police Department" },
                    { 1, "Delta Police Department" },
                    { 2, "Saanich Police Department" }
                });
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "SubmittingAgencyLookup");

            migrationBuilder.DropTable(
                name: "SubmittingAgencyRequests");

            migrationBuilder.DeleteData(
                table: "OrganizationLookup",
                keyColumn: "Code",
                keyValue: 9);

            migrationBuilder.DeleteData(
                table: "OrganizationLookup",
                keyColumn: "Code",
                keyValue: 10);

            migrationBuilder.DeleteData(
                table: "OrganizationLookup",
                keyColumn: "Code",
                keyValue: 11);
        }
    }
}
