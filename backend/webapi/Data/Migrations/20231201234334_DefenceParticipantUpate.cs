using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Pidp.Data.Migrations
{
    public partial class DefenceParticipantUpate : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "EdtExternalIdentifier",
                table: "DigitalEvidenceDefence",
                type: "text",
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<int>(
                name: "ManuallyAddedParticipantId",
                table: "DigitalEvidenceDefence",
                type: "integer",
                nullable: false,
                defaultValue: 0);
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "EdtExternalIdentifier",
                table: "DigitalEvidenceDefence");

            migrationBuilder.DropColumn(
                name: "ManuallyAddedParticipantId",
                table: "DigitalEvidenceDefence");
        }
    }
}
