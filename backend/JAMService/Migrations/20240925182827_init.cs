using System.Collections.Generic;
using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

#pragma warning disable CA1814 // Prefer jagged arrays over multidimensional

namespace JAMService.Migrations
{
    /// <inheritdoc />
    public partial class init : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.EnsureSchema(
                name: "jamservice");

            migrationBuilder.CreateTable(
                name: "Applications",
                schema: "jamservice",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    Name = table.Column<string>(type: "text", nullable: false),
                    Description = table.Column<string>(type: "text", nullable: false),
                    GroupPath = table.Column<string>(type: "text", nullable: false),
                    ValidIDPs = table.Column<List<string>>(type: "text[]", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Applications", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "AppRequests",
                schema: "jamservice",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    Username = table.Column<string>(type: "text", nullable: false),
                    AppRequestType = table.Column<string>(type: "text", nullable: false),
                    AppRequestId = table.Column<int>(type: "integer", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AppRequests", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "IdempotentConsumers",
                schema: "jamservice",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    MessageId = table.Column<string>(type: "text", nullable: false),
                    Consumer = table.Column<string>(type: "text", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_IdempotentConsumers", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "IDPMappers",
                schema: "jamservice",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    SourceRealm = table.Column<string>(type: "text", nullable: false),
                    SourceIdp = table.Column<string>(type: "text", nullable: false),
                    TargetRealm = table.Column<string>(type: "text", nullable: false),
                    TargetIdp = table.Column<string>(type: "text", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_IDPMappers", x => x.Id);
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
                columns: new[] { "Id", "Description", "GroupPath", "Name", "ValidIDPs" },
                values: new object[] { 1, "JUSTIN Protection Order Registry", "/JAM/POR", "JAM_POR", new List<string> { "azuread" } });

            migrationBuilder.InsertData(
                schema: "jamservice",
                table: "IDPMappers",
                columns: new[] { "Id", "SourceIdp", "SourceRealm", "TargetIdp", "TargetRealm" },
                values: new object[] { 1, "azuread", "BCPS", "azure-idir", "ISB" });

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
                name: "AppRequests",
                schema: "jamservice");

            migrationBuilder.DropTable(
                name: "AppRoleMappings",
                schema: "jamservice");

            migrationBuilder.DropTable(
                name: "IdempotentConsumers",
                schema: "jamservice");

            migrationBuilder.DropTable(
                name: "IDPMappers",
                schema: "jamservice");

            migrationBuilder.DropTable(
                name: "Applications",
                schema: "jamservice");
        }
    }
}
