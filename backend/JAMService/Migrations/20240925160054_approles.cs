using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

#pragma warning disable CA1814 // Prefer jagged arrays over multidimensional

namespace JAMService.Migrations
{
    /// <inheritdoc />
    public partial class approles : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "Applications",
                schema: "jamservice",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    Name = table.Column<string>(type: "text", nullable: false),
                    Description = table.Column<string>(type: "text", nullable: false),
                    GroupPath = table.Column<string>(type: "text", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Applications", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "AppRoleMappings",
                schema: "jamservice",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    ApplicationId = table.Column<int>(type: "integer", nullable: false),
                    Role = table.Column<string>(type: "text", nullable: false),
                    IsRealmGroup = table.Column<bool>(type: "boolean", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AppRoleMappings", x => x.Id);
                    table.ForeignKey(
                        name: "FK_AppRoleMappings_Applications_ApplicationId",
                        column: x => x.ApplicationId,
                        principalSchema: "jamservice",
                        principalTable: "Applications",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.InsertData(
                schema: "jamservice",
                table: "Applications",
                columns: new[] { "Id", "Description", "GroupPath", "Name" },
                values: new object[] { 1, "JUSTIN Protection Order Registry", "/JAM/POR", "JAM_POR" });

            migrationBuilder.InsertData(
                schema: "jamservice",
                table: "AppRoleMappings",
                columns: new[] { "Id", "ApplicationId", "IsRealmGroup", "Role" },
                values: new object[,]
                {
                    { 1, 1, true, "POR_READ_ONLY" },
                    { 2, 1, true, "POR_READ_WRITE" },
                    { 3, 1, true, "POR_DELETE_ORDER" }
                });

            migrationBuilder.CreateIndex(
                name: "IX_AppRoleMappings_ApplicationId",
                schema: "jamservice",
                table: "AppRoleMappings",
                column: "ApplicationId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "AppRoleMappings",
                schema: "jamservice");

            migrationBuilder.DropTable(
                name: "Applications",
                schema: "jamservice");
        }
    }
}
