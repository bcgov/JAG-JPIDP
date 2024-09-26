using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Pidp.Data.Migrations
{
    /// <inheritdoc />
    public partial class JamPOC : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "JustingModernizationRequest",
                schema: "diam",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false),
                    OrganizationType = table.Column<string>(type: "text", nullable: false),
                    OrganizationName = table.Column<string>(type: "text", nullable: false),
                    ParticipantId = table.Column<string>(type: "text", nullable: false),
                    TargetApplication = table.Column<string>(type: "text", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_JustingModernizationRequest", x => x.Id);
                    table.ForeignKey(
                        name: "FK_JustingModernizationRequest_AccessRequest_Id",
                        column: x => x.Id,
                        principalSchema: "diam",
                        principalTable: "AccessRequest",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "JustingModernizationRequest",
                schema: "diam");
        }
    }
}
