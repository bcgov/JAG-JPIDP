using System;
using Microsoft.EntityFrameworkCore.Migrations;
using NodaTime;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

#pragma warning disable CA1814 // Prefer jagged arrays over multidimensional

namespace DIAMConfiguration.Migrations
{
    /// <inheritdoc />
    public partial class Initial : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "HostConfig",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    Hostname = table.Column<string>(type: "text", nullable: false),
                    Deleted = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    Created = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    Modified = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_HostConfig", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "PageContent",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    ContentKey = table.Column<string>(type: "text", nullable: false),
                    Resource = table.Column<string>(type: "text", nullable: false),
                    Content = table.Column<string>(type: "text", nullable: true),
                    Created = table.Column<Instant>(type: "timestamp with time zone", nullable: false),
                    Modified = table.Column<Instant>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_PageContent", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "UserPreference",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    UserId = table.Column<Guid>(type: "uuid", nullable: false),
                    Preference = table.Column<string>(type: "jsonb", nullable: false),
                    Deleted = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    Created = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    Modified = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_UserPreference", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "LoginConfig",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    Idp = table.Column<string>(type: "text", nullable: false),
                    Name = table.Column<string>(type: "text", nullable: false),
                    Type = table.Column<string>(type: "text", nullable: false),
                    Notification = table.Column<string>(type: "text", nullable: true),
                    FormControl = table.Column<string>(type: "text", nullable: true),
                    FormList = table.Column<string>(type: "text", nullable: true),
                    HostConfigId = table.Column<int>(type: "integer", nullable: true),
                    Deleted = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    Created = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    Modified = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_LoginConfig", x => x.Id);
                    table.ForeignKey(
                        name: "FK_LoginConfig_HostConfig_HostConfigId",
                        column: x => x.HostConfigId,
                        principalTable: "HostConfig",
                        principalColumn: "Id");
                });

            migrationBuilder.CreateTable(
                name: "HostConfigPageContent",
                columns: table => new
                {
                    HostsId = table.Column<int>(type: "integer", nullable: false),
                    PageContentsId = table.Column<int>(type: "integer", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_HostConfigPageContent", x => new { x.HostsId, x.PageContentsId });
                    table.ForeignKey(
                        name: "FK_HostConfigPageContent_HostConfig_HostsId",
                        column: x => x.HostsId,
                        principalTable: "HostConfig",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_HostConfigPageContent_PageContent_PageContentsId",
                        column: x => x.PageContentsId,
                        principalTable: "PageContent",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.InsertData(
                table: "HostConfig",
                columns: new[] { "Id", "Created", "Deleted", "Hostname", "Modified" },
                values: new object[] { 1, new DateTimeOffset(new DateTime(2024, 2, 14, 12, 53, 34, 470, DateTimeKind.Unspecified).AddTicks(3290), new TimeSpan(0, -8, 0, 0, 0)), null, "locahost", null });

            migrationBuilder.InsertData(
                table: "LoginConfig",
                columns: new[] { "Id", "Created", "Deleted", "FormControl", "FormList", "HostConfigId", "Idp", "Modified", "Name", "Notification", "Type" },
                values: new object[,]
                {
                    { 1, new DateTimeOffset(new DateTime(2024, 2, 14, 12, 53, 34, 470, DateTimeKind.Unspecified).AddTicks(2766), new TimeSpan(0, -8, 0, 0, 0)), null, "", "", null, "ADFS", null, "BCPS iKey", "", "BUTTON" },
                    { 2, new DateTimeOffset(new DateTime(2024, 2, 14, 12, 53, 34, 470, DateTimeKind.Unspecified).AddTicks(2910), new TimeSpan(0, -8, 0, 0, 0)), null, "", "", null, "oidcazure", null, "BCPS IDIR", "", "BUTTON" },
                    { 3, new DateTimeOffset(new DateTime(2024, 2, 14, 12, 53, 34, 470, DateTimeKind.Unspecified).AddTicks(2922), new TimeSpan(0, -8, 0, 0, 0)), null, "", "", null, "verified", null, "Verifiable Credentials", "", "BUTTON" },
                    { 4, new DateTimeOffset(new DateTime(2024, 2, 14, 12, 53, 34, 470, DateTimeKind.Unspecified).AddTicks(2931), new TimeSpan(0, -8, 0, 0, 0)), null, "", "", null, "bcsc", null, "BC Services Card", "", "BUTTON" },
                    { 5, new DateTimeOffset(new DateTime(2024, 2, 14, 12, 53, 34, 470, DateTimeKind.Unspecified).AddTicks(2938), new TimeSpan(0, -8, 0, 0, 0)), null, "selectedAgency", "filteredAgencies", null, "submitting_agencies", null, "BCPS IDIR", "", "AUTOCOMPLETE" },
                    { 6, new DateTimeOffset(new DateTime(2024, 2, 14, 12, 53, 34, 470, DateTimeKind.Unspecified).AddTicks(2948), new TimeSpan(0, -8, 0, 0, 0)), null, "", "", null, "azuread", null, "BCPS Azure MFA", "", "BUTTON" }
                });

            migrationBuilder.CreateIndex(
                name: "IX_HostConfigPageContent_PageContentsId",
                table: "HostConfigPageContent",
                column: "PageContentsId");

            migrationBuilder.CreateIndex(
                name: "IX_LoginConfig_HostConfigId",
                table: "LoginConfig",
                column: "HostConfigId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "HostConfigPageContent");

            migrationBuilder.DropTable(
                name: "LoginConfig");

            migrationBuilder.DropTable(
                name: "UserPreference");

            migrationBuilder.DropTable(
                name: "PageContent");

            migrationBuilder.DropTable(
                name: "HostConfig");
        }
    }
}
