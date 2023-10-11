using Microsoft.EntityFrameworkCore.Migrations;
using NodaTime;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

namespace edt.casemanagement.Migrations
{
    public partial class CaseSearchRequests : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "CaseSearchRequest",
                schema: "casemgmt",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    Requested = table.Column<Instant>(type: "timestamp with time zone", nullable: true),
                    PartyId = table.Column<string>(type: "text", nullable: false),
                    AgencyFileNumber = table.Column<string>(type: "text", nullable: false),
                    SearchString = table.Column<string>(type: "text", nullable: false),
                    ResponseTime = table.Column<long>(type: "bigint", nullable: false),
                    ResponseStatus = table.Column<string>(type: "text", nullable: false),
                    ResponseError = table.Column<string>(type: "text", nullable: false),
                    Created = table.Column<Instant>(type: "timestamp with time zone", nullable: false),
                    Modified = table.Column<Instant>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_CaseSearchRequest", x => x.Id);
                });
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "CaseSearchRequest",
                schema: "casemgmt");
        }
    }
}
