using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Pidp.Data.Migrations
{
    public partial class Defence : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "FolioCaseId",
                table: "DigitalEvidenceDisclosure");

            migrationBuilder.DropColumn(
                name: "FolioId",
                table: "DigitalEvidenceDisclosure");

            migrationBuilder.CreateTable(
                name: "DigitalEvidenceDefence",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false),
                    OrganizationType = table.Column<string>(type: "text", nullable: false),
                    OrganizationName = table.Column<string>(type: "text", nullable: false),
                    ParticipantId = table.Column<string>(type: "text", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_DigitalEvidenceDefence", x => x.Id);
                    table.ForeignKey(
                        name: "FK_DigitalEvidenceDefence_AccessRequest_Id",
                        column: x => x.Id,
                        principalTable: "AccessRequest",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "DigitalEvidenceDefence");

            migrationBuilder.AddColumn<int>(
                name: "FolioCaseId",
                table: "DigitalEvidenceDisclosure",
                type: "integer",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<string>(
                name: "FolioId",
                table: "DigitalEvidenceDisclosure",
                type: "text",
                nullable: false,
                defaultValue: "");
        }
    }
}
