using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Pidp.Data.Migrations
{
    public partial class AddDigitalEvidenceCase : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {

            migrationBuilder.CreateTable(
                  name: "DigitalEvidenceCase",
                  columns: table => new
                  {
                      Id = table.Column<int>(type: "integer", nullable: false),
                      CaseId = table.Column<int>(type: "integer", nullable: false),
                      AgencyFileNumber = table.Column<string>(type: "text", nullable: false, defaultValue: string.Empty),
                      RemoveRequested = table.Column<bool>( type: "bool",    nullable: false,    defaultValue: false)
        },
                  constraints: table =>
                  {
                      table.PrimaryKey("PK_DigitalEvidenceCase", x => x.Id);
                      table.ForeignKey(
                          name: "FK_DigitalEvidenceCase_AccessRequest_Id",
                          column: x => x.Id,
                          principalTable: "AccessRequest",
                          principalColumn: "Id",
                          onDelete: ReferentialAction.Cascade);
                  });

        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
         name: "DigitalEvidenceCase");
        }
    }
}
