using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

#pragma warning disable CA1814 // Prefer jagged arrays over multidimensional

namespace Pidp.Data.Migrations
{
    /// <inheritdoc />
    public partial class bailcourts : Migration
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
                    { "In-VR3", true, "Interior - VR3", true },
                    { "In-VR4", true, "Interior - VR4", true },
                    { "Is-VR8", true, "Island - VR8", true },
                    { "Is-VR9", true, "Island - VR9", true },
                    { "No-VR1", true, "North - VR1", true },
                    { "No-VR2", true, "North - VR2", true }
                });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DeleteData(
                schema: "diam",
                table: "CourtLocation",
                keyColumn: "Code",
                keyValue: "In-VR3");

            migrationBuilder.DeleteData(
                schema: "diam",
                table: "CourtLocation",
                keyColumn: "Code",
                keyValue: "In-VR4");

            migrationBuilder.DeleteData(
                schema: "diam",
                table: "CourtLocation",
                keyColumn: "Code",
                keyValue: "Is-VR8");

            migrationBuilder.DeleteData(
                schema: "diam",
                table: "CourtLocation",
                keyColumn: "Code",
                keyValue: "Is-VR9");

            migrationBuilder.DeleteData(
                schema: "diam",
                table: "CourtLocation",
                keyColumn: "Code",
                keyValue: "No-VR1");

            migrationBuilder.DeleteData(
                schema: "diam",
                table: "CourtLocation",
                keyColumn: "Code",
                keyValue: "No-VR2");
        }
    }
}
