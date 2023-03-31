using Microsoft.EntityFrameworkCore.Migrations;
using NodaTime;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

namespace Pidp.Data.Migrations
{
    public partial class RequestErrorDetails : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropPrimaryKey(
                name: "PK_OutBoxedExportedEvent",
                table: "OutBoxedExportedEvent");

            migrationBuilder.DeleteData(
                table: "OrganizationLookup",
                keyColumn: "Code",
                keyValue: 1);

            migrationBuilder.DeleteData(
                table: "OrganizationLookup",
                keyColumn: "Code",
                keyValue: 2);

            migrationBuilder.DeleteData(
                table: "OrganizationLookup",
                keyColumn: "Code",
                keyValue: 3);

            migrationBuilder.DeleteData(
                table: "OrganizationLookup",
                keyColumn: "Code",
                keyValue: 4);

            migrationBuilder.DeleteData(
                table: "OrganizationLookup",
                keyColumn: "Code",
                keyValue: 5);

            migrationBuilder.DeleteData(
                table: "OrganizationLookup",
                keyColumn: "Code",
                keyValue: 6);

            migrationBuilder.DeleteData(
                table: "OrganizationLookup",
                keyColumn: "Code",
                keyValue: 7);

            migrationBuilder.DeleteData(
                table: "OrganizationLookup",
                keyColumn: "Code",
                keyValue: 8);

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

            migrationBuilder.DropColumn(
                name: "AgencyCode",
                table: "SubmittingAgencyRequest");

            migrationBuilder.RenameColumn(
                name: "CaseNumber",
                table: "SubmittingAgencyRequest",
                newName: "Details");

            migrationBuilder.RenameColumn(
                name: "CaseGroup",
                table: "SubmittingAgencyRequest",
                newName: "AgencyFileNumber");

            migrationBuilder.AddColumn<int>(
                name: "CaseId",
                table: "SubmittingAgencyRequest",
                type: "integer",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AlterColumn<int>(
                name: "EventId",
                table: "OutBoxedExportedEvent",
                type: "integer",
                nullable: false,
                oldClrType: typeof(int),
                oldType: "integer")
                .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn);

            migrationBuilder.AddColumn<Instant>(
                name: "DateOccurred",
                table: "OutBoxedExportedEvent",
                type: "timestamp with time zone",
                nullable: false,
                defaultValue: NodaTime.Instant.FromUnixTimeTicks(0L));

            migrationBuilder.AddColumn<string>(
                name: "Details",
                table: "AccessRequest",
                type: "text",
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddPrimaryKey(
                name: "PK_OutBoxedExportedEvent",
                table: "OutBoxedExportedEvent",
                column: "EventId");

            migrationBuilder.UpdateData(
                table: "AccessTypeLookup",
                keyColumn: "Code",
                keyValue: 6,
                column: "Name",
                value: "Digital Evidence Case Management");

            migrationBuilder.UpdateData(
                table: "AccessTypeLookup",
                keyColumn: "Code",
                keyValue: 7,
                column: "Name",
                value: "Fraser Health UCI");

            migrationBuilder.InsertData(
                table: "AccessTypeLookup",
                columns: new[] { "Code", "Name" },
                values: new object[] { 8, "MS Teams for Clinical Use" });

            migrationBuilder.CreateIndex(
                name: "IX_OrganizationLookup_IdpHint",
                table: "OrganizationLookup",
                column: "IdpHint",
                unique: true);
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropPrimaryKey(
                name: "PK_OutBoxedExportedEvent",
                table: "OutBoxedExportedEvent");

            migrationBuilder.DropIndex(
                name: "IX_OrganizationLookup_IdpHint",
                table: "OrganizationLookup");

            migrationBuilder.DeleteData(
                table: "AccessTypeLookup",
                keyColumn: "Code",
                keyValue: 8);

            migrationBuilder.DropColumn(
                name: "CaseId",
                table: "SubmittingAgencyRequest");

            migrationBuilder.DropColumn(
                name: "DateOccurred",
                table: "OutBoxedExportedEvent");

            migrationBuilder.DropColumn(
                name: "Details",
                table: "AccessRequest");

            migrationBuilder.RenameColumn(
                name: "Details",
                table: "SubmittingAgencyRequest",
                newName: "CaseNumber");

            migrationBuilder.RenameColumn(
                name: "AgencyFileNumber",
                table: "SubmittingAgencyRequest",
                newName: "CaseGroup");

            migrationBuilder.AddColumn<string>(
                name: "AgencyCode",
                table: "SubmittingAgencyRequest",
                type: "text",
                nullable: false,
                defaultValue: "");

            migrationBuilder.AlterColumn<int>(
                name: "EventId",
                table: "OutBoxedExportedEvent",
                type: "integer",
                nullable: false,
                oldClrType: typeof(int),
                oldType: "integer")
                .OldAnnotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn);

            migrationBuilder.AddPrimaryKey(
                name: "PK_OutBoxedExportedEvent",
                table: "OutBoxedExportedEvent",
                columns: new[] { "EventId", "AggregateId" });

            migrationBuilder.UpdateData(
                table: "AccessTypeLookup",
                keyColumn: "Code",
                keyValue: 6,
                column: "Name",
                value: "Fraser Health UCI");

            migrationBuilder.UpdateData(
                table: "AccessTypeLookup",
                keyColumn: "Code",
                keyValue: 7,
                column: "Name",
                value: "MS Teams for Clinical Use");

            migrationBuilder.InsertData(
                table: "OrganizationLookup",
                columns: new[] { "Code", "IdpHint", "Name" },
                values: new object[,]
                {
                    { 1, "ADFS", "Justice Sector" },
                    { 2, "", "BC Law Enforcement" },
                    { 3, "vcc", "BC Law Society" },
                    { 4, "", "BC Corrections Service" },
                    { 5, "", "Health Authority" },
                    { 6, "idir", "BC Government Ministry" },
                    { 7, "icbc", "ICBC" },
                    { 8, "", "Other" },
                    { 9, "vicpd", "Victoria Police Department" },
                    { 10, "deltapd", "Delta Police Department" },
                    { 11, "saanichpd", "Saanich Police Department" }
                });
        }
    }
}
