using Microsoft.EntityFrameworkCore.Migrations;
using NodaTime;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

namespace Pidp.Data.Migrations
{
  public partial class AddUserChange : Migration
  {
    protected override void Up(MigrationBuilder migrationBuilder)
    {
      migrationBuilder.CreateTable(
          name: "UserAccountChange",
          columns: table => new
          {
            Id = table.Column<int>(type: "integer", nullable: false)
                  .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
            PartyId = table.Column<int>(type: "integer", nullable: false),
            // EventMessageId = table.Column<int>(type: "integer", nullable: false),
            Deactivated = table.Column<bool>(type: "boolean", nullable: false),
            Reason = table.Column<string>(type: "text", nullable: false),
            ChangeData = table.Column<string>(type: "text", nullable: false),
            Completed = table.Column<Instant>(type: "timestamp with time zone", nullable: false),
            TraceId = table.Column<string>(type: "text", nullable: false),
            Status = table.Column<string>(type: "text", nullable: false),
            Created = table.Column<Instant>(type: "timestamp with time zone", nullable: false),
            Modified = table.Column<Instant>(type: "timestamp with time zone", nullable: false)
          },
          constraints: table =>
          {
            table.PrimaryKey("PK_UserAccountChange", x => x.Id);
            table.ForeignKey(
                      name: "FK_UserAccountChange_Party_PartyId",
                      column: x => x.PartyId,
                      principalTable: "Party",
                      principalColumn: "Id",
                      onDelete: ReferentialAction.Cascade);
          });

      migrationBuilder.CreateIndex(
          name: "IX_UserAccountChange_PartyId",
          table: "UserAccountChange",
          column: "PartyId");
    }

    protected override void Down(MigrationBuilder migrationBuilder)
    {
      migrationBuilder.DropTable(
          name: "UserAccountChange");
    }
  }
}
