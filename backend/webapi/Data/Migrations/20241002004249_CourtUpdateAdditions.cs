using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

#pragma warning disable CA1814 // Prefer jagged arrays over multidimensional

namespace Pidp.Data.Migrations
{
    /// <inheritdoc />
    public partial class CourtUpdateAdditions : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.InsertData(
                schema: "diam",
                table: "CourtLocation",
                columns: new[] { "Code", "Active", "Name", "Staffed" },
                values: new object[,]
                {
                    { "2011", true, "North Vancouver Provincial Court", true },
                    { "2025", true, "Richmond Provincial Courts", true },
                    { "2040", true, "Vancouver Provincial Court ", true },
                    { "2042", true, "Downtown Community Court", true },
                    { "2045", true, "Robson Square Provincial Court-Youth", true },
                    { "3531", true, "Port Coquitlam Law Courts", true },
                    { "3561", true, "Abbotsford Law Courts", true },
                    { "3585-A", true, "Surrey Provincial Court-Adult", true },
                    { "3585-Y", true, "Surrey Provincial Court-IPV and Youth", true }
                });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DeleteData(
                schema: "diam",
                table: "CourtLocation",
                keyColumn: "Code",
                keyValue: "2011");

            migrationBuilder.DeleteData(
                schema: "diam",
                table: "CourtLocation",
                keyColumn: "Code",
                keyValue: "2025");

            migrationBuilder.DeleteData(
                schema: "diam",
                table: "CourtLocation",
                keyColumn: "Code",
                keyValue: "2040");

            migrationBuilder.DeleteData(
                schema: "diam",
                table: "CourtLocation",
                keyColumn: "Code",
                keyValue: "2042");

            migrationBuilder.DeleteData(
                schema: "diam",
                table: "CourtLocation",
                keyColumn: "Code",
                keyValue: "2045");

            migrationBuilder.DeleteData(
                schema: "diam",
                table: "CourtLocation",
                keyColumn: "Code",
                keyValue: "3531");

            migrationBuilder.DeleteData(
                schema: "diam",
                table: "CourtLocation",
                keyColumn: "Code",
                keyValue: "3561");

            migrationBuilder.DeleteData(
                schema: "diam",
                table: "CourtLocation",
                keyColumn: "Code",
                keyValue: "3585-A");

            migrationBuilder.DeleteData(
                schema: "diam",
                table: "CourtLocation",
                keyColumn: "Code",
                keyValue: "3585-Y");
        }
    }
}
