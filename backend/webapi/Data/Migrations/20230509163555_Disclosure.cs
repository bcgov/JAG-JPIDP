using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Pidp.Data.Migrations
{
    public partial class Disclosure : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.UpdateData(
                table: "AccessTypeLookup",
                keyColumn: "Code",
                keyValue: 7,
                column: "Name",
                value: "Digital Evidence Disclosure");

            migrationBuilder.UpdateData(
                table: "AccessTypeLookup",
                keyColumn: "Code",
                keyValue: 8,
                column: "Name",
                value: "Fraser Health UCI");

            migrationBuilder.InsertData(
                table: "AccessTypeLookup",
                columns: new[] { "Code", "Name" },
                values: new object[] { 9, "MS Teams for Clinical Use" });
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DeleteData(
                table: "AccessTypeLookup",
                keyColumn: "Code",
                keyValue: 9);

            migrationBuilder.UpdateData(
                table: "AccessTypeLookup",
                keyColumn: "Code",
                keyValue: 7,
                column: "Name",
                value: "Fraser Health UCI");

            migrationBuilder.UpdateData(
                table: "AccessTypeLookup",
                keyColumn: "Code",
                keyValue: 8,
                column: "Name",
                value: "MS Teams for Clinical Use");
        }
    }
}
