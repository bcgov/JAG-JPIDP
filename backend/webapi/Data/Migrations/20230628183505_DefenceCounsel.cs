using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Pidp.Data.Migrations
{
    public partial class DefenceCounsel : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.UpdateData(
                table: "AccessTypeLookup",
                keyColumn: "Code",
                keyValue: 8,
                column: "Name",
                value: "Digital Evidence Defence");

            migrationBuilder.UpdateData(
                table: "AccessTypeLookup",
                keyColumn: "Code",
                keyValue: 9,
                column: "Name",
                value: "Fraser Health UCI");

            migrationBuilder.InsertData(
                table: "AccessTypeLookup",
                columns: new[] { "Code", "Name" },
                values: new object[] { 10, "MS Teams for Clinical Use" });
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DeleteData(
                table: "AccessTypeLookup",
                keyColumn: "Code",
                keyValue: 10);

            migrationBuilder.UpdateData(
                table: "AccessTypeLookup",
                keyColumn: "Code",
                keyValue: 8,
                column: "Name",
                value: "Fraser Health UCI");

            migrationBuilder.UpdateData(
                table: "AccessTypeLookup",
                keyColumn: "Code",
                keyValue: 9,
                column: "Name",
                value: "MS Teams for Clinical Use");
        }
    }
}
