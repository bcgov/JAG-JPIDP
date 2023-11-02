using Microsoft.EntityFrameworkCore.Migrations;
using NodaTime;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

namespace Pidp.Data.Migrations
{
    public partial class PublicDisclosure : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "DigitalEvidencePublicDisclosure",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    KeyData = table.Column<string>(type: "text", nullable: false),
                    RequestedOn = table.Column<Instant>(type: "timestamp with time zone", nullable: false),
                    CompletedOn = table.Column<Instant>(type: "timestamp with time zone", nullable: false),
                    DigitalEvidenceDisclosureId = table.Column<int>(type: "integer", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_DigitalEvidencePublicDisclosure", x => x.Id);
                    table.ForeignKey(
                        name: "FK_DigitalEvidencePublicDisclosure_DigitalEvidenceDisclosure_D~",
                        column: x => x.DigitalEvidenceDisclosureId,
                        principalTable: "DigitalEvidenceDisclosure",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_DigitalEvidencePublicDisclosure_DigitalEvidenceDisclosureId",
                table: "DigitalEvidencePublicDisclosure",
                column: "DigitalEvidenceDisclosureId");
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "DigitalEvidencePublicDisclosure");
        }
    }
}
