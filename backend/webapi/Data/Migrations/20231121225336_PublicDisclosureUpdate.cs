using Microsoft.EntityFrameworkCore.Migrations;
using NodaTime;

#nullable disable

namespace Pidp.Data.Migrations
{
    public partial class PublicDisclosureUpdate : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_DigitalEvidencePublicDisclosure_DigitalEvidenceDisclosure_D~",
                table: "DigitalEvidencePublicDisclosure");

            migrationBuilder.DropIndex(
                name: "IX_DigitalEvidencePublicDisclosure_DigitalEvidenceDisclosureId",
                table: "DigitalEvidencePublicDisclosure");

            migrationBuilder.DropColumn(
                name: "CompletedOn",
                table: "DigitalEvidencePublicDisclosure");

            migrationBuilder.DropColumn(
                name: "DigitalEvidenceDisclosureId",
                table: "DigitalEvidencePublicDisclosure");
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<Instant>(
                name: "CompletedOn",
                table: "DigitalEvidencePublicDisclosure",
                type: "timestamp with time zone",
                nullable: false,
                defaultValue: NodaTime.Instant.FromUnixTimeTicks(0L));

            migrationBuilder.AddColumn<int>(
                name: "DigitalEvidenceDisclosureId",
                table: "DigitalEvidencePublicDisclosure",
                type: "integer",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.CreateIndex(
                name: "IX_DigitalEvidencePublicDisclosure_DigitalEvidenceDisclosureId",
                table: "DigitalEvidencePublicDisclosure",
                column: "DigitalEvidenceDisclosureId");

            migrationBuilder.AddForeignKey(
                name: "FK_DigitalEvidencePublicDisclosure_DigitalEvidenceDisclosure_D~",
                table: "DigitalEvidencePublicDisclosure",
                column: "DigitalEvidenceDisclosureId",
                principalTable: "DigitalEvidenceDisclosure",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);
        }
    }
}
