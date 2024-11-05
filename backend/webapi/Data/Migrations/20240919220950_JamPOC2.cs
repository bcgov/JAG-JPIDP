using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

#pragma warning disable CA1814 // Prefer jagged arrays over multidimensional

namespace Pidp.Data.Migrations
{
    /// <inheritdoc />
    public partial class JamPOC2 : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "JustingModernizationRequest",
                schema: "diam");

            migrationBuilder.CreateTable(
                name: "JustinAppAccessRequest",
                schema: "diam",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false),
                    OrganizationType = table.Column<string>(type: "text", nullable: false),
                    OrganizationName = table.Column<string>(type: "text", nullable: false),
                    ParticipantId = table.Column<string>(type: "text", nullable: false),
                    TargetApplication = table.Column<string>(type: "text", nullable: false),
                    JustinUserId = table.Column<string>(type: "text", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_JustinAppAccessRequest", x => x.Id);
                    table.ForeignKey(
                        name: "FK_JustinAppAccessRequest_AccessRequest_Id",
                        column: x => x.Id,
                        principalSchema: "diam",
                        principalTable: "AccessRequest",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.InsertData(
                schema: "diam",
                table: "AccessTypeLookup",
                columns: new[] { "Code", "Name" },
                values: new object[,]
                {
                    { 11, "JUSTIN Protection Order" },
                    { 12, "JUSTIN Request for Crown" },
                    { 13, "JUSTIN Law Enforcement" }
                });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "JustinAppAccessRequest",
                schema: "diam");

            migrationBuilder.DeleteData(
                schema: "diam",
                table: "AccessTypeLookup",
                keyColumn: "Code",
                keyValue: 11);

            migrationBuilder.DeleteData(
                schema: "diam",
                table: "AccessTypeLookup",
                keyColumn: "Code",
                keyValue: 12);

            migrationBuilder.DeleteData(
                schema: "diam",
                table: "AccessTypeLookup",
                keyColumn: "Code",
                keyValue: 13);

            migrationBuilder.CreateTable(
                name: "JustingModernizationRequest",
                schema: "diam",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false),
                    OrganizationName = table.Column<string>(type: "text", nullable: false),
                    OrganizationType = table.Column<string>(type: "text", nullable: false),
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
    }
}
