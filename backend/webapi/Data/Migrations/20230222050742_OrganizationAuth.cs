using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Pidp.Data.Migrations
{
    public partial class OrganizationAuth : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "CaseGroup",
                table: "SubmittingAgencyRequest",
                type: "text",
                nullable: false,
                defaultValue: "");

            //migrationBuilder.AlterColumn<string>(
            //    name: "EventPayload",
            //    table: "OutBoxedExportedEvent",
            //    type: "jsonb",
            //    nullable: false,
            //    oldClrType: typeof(string),
            //    oldType: "text");

            migrationBuilder.Sql(
                @"
                    ALTER TABLE ""OutBoxedExportedEvent""
                        ALTER COLUMN ""EventPayload"" TYPE jsonb
                        USING ""EventPayload""::jsonb;
                ");

            migrationBuilder.AddColumn<string>(
                name: "IdpHint",
                table: "OrganizationLookup",
                type: "text",
                nullable: false,
                defaultValue: "");

            migrationBuilder.UpdateData(
                table: "OrganizationLookup",
                keyColumn: "Code",
                keyValue: 1,
                column: "IdpHint",
                value: "ADFS");

            migrationBuilder.UpdateData(
                table: "OrganizationLookup",
                keyColumn: "Code",
                keyValue: 2,
                column: "IdpHint",
                value: "");

            migrationBuilder.UpdateData(
                table: "OrganizationLookup",
                keyColumn: "Code",
                keyValue: 3,
                column: "IdpHint",
                value: "vcc");

            migrationBuilder.UpdateData(
                table: "OrganizationLookup",
                keyColumn: "Code",
                keyValue: 4,
                column: "IdpHint",
                value: "");

            migrationBuilder.UpdateData(
                table: "OrganizationLookup",
                keyColumn: "Code",
                keyValue: 5,
                column: "IdpHint",
                value: "");

            migrationBuilder.UpdateData(
                table: "OrganizationLookup",
                keyColumn: "Code",
                keyValue: 6,
                column: "IdpHint",
                value: "idir");

            migrationBuilder.UpdateData(
                table: "OrganizationLookup",
                keyColumn: "Code",
                keyValue: 7,
                column: "IdpHint",
                value: "icbc");

            migrationBuilder.UpdateData(
                table: "OrganizationLookup",
                keyColumn: "Code",
                keyValue: 8,
                column: "IdpHint",
                value: "");

            migrationBuilder.UpdateData(
                table: "OrganizationLookup",
                keyColumn: "Code",
                keyValue: 9,
                column: "IdpHint",
                value: "vicpd");

            migrationBuilder.UpdateData(
                table: "OrganizationLookup",
                keyColumn: "Code",
                keyValue: 10,
                column: "IdpHint",
                value: "deltapd");

            migrationBuilder.UpdateData(
                table: "OrganizationLookup",
                keyColumn: "Code",
                keyValue: 11,
                column: "IdpHint",
                value: "saanichpd");
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "CaseGroup",
                table: "SubmittingAgencyRequest");

            migrationBuilder.DropColumn(
                name: "IdpHint",
                table: "OrganizationLookup");

            migrationBuilder.AlterColumn<string>(
                name: "EventPayload",
                table: "OutBoxedExportedEvent",
                type: "text",
                nullable: false,
                oldClrType: typeof(string),
                oldType: "jsonb");
        }
    }
}
