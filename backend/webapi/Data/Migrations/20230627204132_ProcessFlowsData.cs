using Microsoft.EntityFrameworkCore.Migrations;
using NodaTime;

#nullable disable

namespace Pidp.Data.Migrations
{
    public partial class ProcessFlowsData : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.InsertData(
                table: "ProcessSection",
                columns: new[] { "Id", "Created", "Modified", "Name" },
                values: new object[,]
                {
                    { -10, NodaTime.Instant.FromUnixTimeTicks(0L), NodaTime.Instant.FromUnixTimeTicks(0L), "admin" },
                    { -9, NodaTime.Instant.FromUnixTimeTicks(0L), NodaTime.Instant.FromUnixTimeTicks(0L), "administratorInfo" },
                    { -8, NodaTime.Instant.FromUnixTimeTicks(0L), NodaTime.Instant.FromUnixTimeTicks(0L), "uci" },
                    { -7, NodaTime.Instant.FromUnixTimeTicks(0L), NodaTime.Instant.FromUnixTimeTicks(0L), "submittingAgencyCaseManagement" },
                    { -6, NodaTime.Instant.FromUnixTimeTicks(0L), NodaTime.Instant.FromUnixTimeTicks(0L), "digitalEvidenceCounsel" },
                    { -5, NodaTime.Instant.FromUnixTimeTicks(0L), NodaTime.Instant.FromUnixTimeTicks(0L), "digitalEvidenceCaseManagement" },
                    { -4, NodaTime.Instant.FromUnixTimeTicks(0L), NodaTime.Instant.FromUnixTimeTicks(0L), "digitalEvidence" },
                    { -3, NodaTime.Instant.FromUnixTimeTicks(0L), NodaTime.Instant.FromUnixTimeTicks(0L), "driverFitness" },
                    { -2, NodaTime.Instant.FromUnixTimeTicks(0L), NodaTime.Instant.FromUnixTimeTicks(0L), "demographics" },
                    { -1, NodaTime.Instant.FromUnixTimeTicks(0L), NodaTime.Instant.FromUnixTimeTicks(0L), "organizationDetails" }
                });
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DeleteData(
                table: "ProcessSection",
                keyColumn: "Id",
                keyValue: -10);

            migrationBuilder.DeleteData(
                table: "ProcessSection",
                keyColumn: "Id",
                keyValue: -9);

            migrationBuilder.DeleteData(
                table: "ProcessSection",
                keyColumn: "Id",
                keyValue: -8);

            migrationBuilder.DeleteData(
                table: "ProcessSection",
                keyColumn: "Id",
                keyValue: -7);

            migrationBuilder.DeleteData(
                table: "ProcessSection",
                keyColumn: "Id",
                keyValue: -6);

            migrationBuilder.DeleteData(
                table: "ProcessSection",
                keyColumn: "Id",
                keyValue: -5);

            migrationBuilder.DeleteData(
                table: "ProcessSection",
                keyColumn: "Id",
                keyValue: -4);

            migrationBuilder.DeleteData(
                table: "ProcessSection",
                keyColumn: "Id",
                keyValue: -3);

            migrationBuilder.DeleteData(
                table: "ProcessSection",
                keyColumn: "Id",
                keyValue: -2);

            migrationBuilder.DeleteData(
                table: "ProcessSection",
                keyColumn: "Id",
                keyValue: -1);
        }
    }
}
