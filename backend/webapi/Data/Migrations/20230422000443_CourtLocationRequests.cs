using Microsoft.EntityFrameworkCore.Migrations;
using NodaTime;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

namespace Pidp.Data.Migrations
{
  public partial class CourtLocationRequests : Migration
  {
    protected override void Up(MigrationBuilder migrationBuilder)
    {
      migrationBuilder.AddColumn<int>(
          name: "EventMessageId",
          table: "UserAccountChange",
          type: "integer",
          nullable: false,
          defaultValue: 0);

      migrationBuilder.CreateTable(
          name: "CourtLocation",
          columns: table => new
          {
            Code = table.Column<string>(type: "text", nullable: false),
            Name = table.Column<string>(type: "text", nullable: false),
            City = table.Column<string>(type: "text", nullable: false),
            Active = table.Column<bool>(type: "boolean", nullable: false),
            Staffed = table.Column<bool>(type: "boolean", nullable: false)
          },
          constraints: table =>
          {
            table.PrimaryKey("PK_CourtLocation", x => x.Code);
          });

      migrationBuilder.CreateTable(
          name: "CourtSubLocation",
          columns: table => new
          {
            CourtSubLocationId = table.Column<int>(type: "integer", nullable: false)
                  .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
            Name = table.Column<string>(type: "text", nullable: false),
            CourtLocationCode = table.Column<string>(type: "text", nullable: true)
          },
          constraints: table =>
          {
            table.PrimaryKey("PK_CourtSubLocation", x => x.CourtSubLocationId);
            table.ForeignKey(
                      name: "FK_CourtSubLocation_CourtLocation_CourtLocationCode",
                      column: x => x.CourtLocationCode,
                      principalTable: "CourtLocation",
                      principalColumn: "Code");
          });

      migrationBuilder.CreateTable(
          name: "CourtLocationAccessRequest",
          columns: table => new
          {
            RequestId = table.Column<int>(type: "integer", nullable: false)
                  .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
            PartyId = table.Column<int>(type: "integer", nullable: false),
            CourtLocationCode = table.Column<string>(type: "text", nullable: true),
            CourtSubLocationId = table.Column<int>(type: "integer", nullable: true),
            RequestedOn = table.Column<Instant>(type: "timestamp with time zone", nullable: false),
            RequestStatus = table.Column<string>(type: "text", nullable: false),
            DeletedOn = table.Column<Instant>(type: "timestamp with time zone", nullable: true),
            ValidFrom = table.Column<Instant>(type: "timestamp with time zone", nullable: false),
            ValidUntil = table.Column<Instant>(type: "timestamp with time zone", nullable: false),
            Created = table.Column<Instant>(type: "timestamp with time zone", nullable: false),
            Modified = table.Column<Instant>(type: "timestamp with time zone", nullable: false)
          },
          constraints: table =>
          {
            table.PrimaryKey("PK_CourtLocationAccessRequest", x => x.RequestId);
            table.ForeignKey(
                      name: "FK_CourtLocationAccessRequest_CourtLocation_CourtLocationCode",
                      column: x => x.CourtLocationCode,
                      principalTable: "CourtLocation",
                      principalColumn: "Code");
            table.ForeignKey(
                      name: "FK_CourtLocationAccessRequest_CourtSubLocation_CourtSubLocatio~",
                      column: x => x.CourtSubLocationId,
                      principalTable: "CourtSubLocation",
                      principalColumn: "CourtSubLocationId");
            table.ForeignKey(
                      name: "FK_CourtLocationAccessRequest_Party_PartyId",
                      column: x => x.PartyId,
                      principalTable: "Party",
                      principalColumn: "Id",
                      onDelete: ReferentialAction.Cascade);
          });

      migrationBuilder.InsertData(
           table: "CourtLocation",
           columns: new[] { "Code", "Active", "City", "Name", "Staffed" },
           values: new object[,]
           {
                    { "ABB", true, "Abbotsford", "Abbotsford Court", true },
                    { "ATL", true, "Atlin", "Atlin Court", true },
                    { "BURN", true, "Burns Lake", "Burns Lake Court", true },
                    { "CAMP", true, "Campbell River", "Campbell River Court", true },
                    { "CHIL", true, "Chiliwack", "Chiliwack Court", true },
                    { "COUR", true, "Courtenay", "Courtenay Court", true },
                    { "CRAN", true, "Cranbrook", "Cranbrook Court", true },
                    { "DAAJ", true, "Daajing Giids", "Courtenay Court", true },
                    { "DAWS", true, "Dawson Creek", "Dawson Creek Court", true },
                    { "DUNC", true, "Duncan", "Duncan Court", true },
                    { "FORT", true, "Fort Nelson", "Fort Nelson Court", true },
                    { "FTSJ", true, "Fort St. John", "Fort St. John Court", true },
                    { "GOLD", true, "Golden", "Golden Court", true },
                    { "KAM", true, "Kamloops", "Kamloops Court", true },
                    { "KEL", true, "Kelowna", "Kelowna Court", true },
                    { "MACK", true, "Mackenzie", "Mackenzie Court", true },
                    { "NANA", true, "Nanaimo", "Nanaimo Court", true },
                    { "NELS", true, "Nelson", "Nelson Court", true },
                    { "NEWWEST", true, "New Westminster", "New Westminster Court", true },
                    { "NVAN", true, "Vancouver", "North Vancouver Court", true },
                    { "PEMB", true, "Pemberton", "Pemberton Court", true },
                    { "PENT", true, "Penticton", "Penticton Court", true },
                    { "POCO", true, "Port Coquitlam", "Port Coquitlam Court", true },
                    { "POHA", true, "Port Hardy", "Port Hardy Court", true },
                    { "PORTA", true, "Port Alberni", "Port Alberni Court", true },
                    { "POWE", true, "Powell River", "Powell River Court", true },
                    { "PRGEO", true, "Prince George", "Prince George Court", true },
                    { "PRRUP", true, "Prince Rupert", "Prince Rupert Court", true },
                    { "QUES", true, "Quesnel", "Quesnel Court", true },
                    { "RICH", true, "Richmond", "Richmond Court", true },
                    { "ROSS", true, "Rossland", "Rossland Court", true },
                    { "SALM", true, "Salmon Arm", "Salmon Arm Court", true },
                    { "SECH", true, "Sechelt", "Sechelt Court", true },
                    { "SMITH", true, "Smithers", "Smithers Court", true },
                    { "SURR", true, "Surrey", "Surrey Court", true },
                    { "TERR", true, "Terrace", "Terrace Court", true },
                    { "VALE", true, "Valemount", "Valemount Court", true },
                    { "VANCIV", true, "Vancouver", "Vancouver Criminal Court", true },
                    { "VANCRIM", true, "Vancouver", "Vancouver Civil Court", true },
                    { "VERN", true, "Vernon", "Vernon Court", true },
                    { "VIC", true, "Victoria", "Victoria Court", true },
                    { "WESTCOM", true, "Victoria", "Western Communities Court", true },
                    { "WILL", true, "Williams Lake", "Williams Lake Court", true }
           });

      //migrationBuilder.CreateIndex(
      //          name: "IX_RCCNumber",
      //          table: "SubmittingAgencyRequest",
      //          column: "RCCNumber");

      migrationBuilder.CreateIndex(
          name: "IX_CourtLocationAccessRequest_CourtLocationCode",
          table: "CourtLocationAccessRequest",
          column: "CourtLocationCode");

      migrationBuilder.CreateIndex(
          name: "IX_CourtLocationAccessRequest_CourtSubLocationId",
          table: "CourtLocationAccessRequest",
          column: "CourtSubLocationId");

      migrationBuilder.CreateIndex(
          name: "IX_CourtLocationAccessRequest_PartyId",
          table: "CourtLocationAccessRequest",
          column: "PartyId");

      migrationBuilder.CreateIndex(
          name: "IX_CourtSubLocation_CourtLocationCode",
          table: "CourtSubLocation",
          column: "CourtLocationCode");
    }

    protected override void Down(MigrationBuilder migrationBuilder)
    {
      migrationBuilder.DropTable(
          name: "CourtLocationAccessRequest");

      migrationBuilder.DropTable(
          name: "CourtSubLocation");

      migrationBuilder.DropTable(
          name: "CourtLocation");

      migrationBuilder.DropIndex(
          name: "IX_RCCNumber",
          table: "SubmittingAgencyRequest");

      migrationBuilder.DropColumn(
          name: "EventMessageId",
          table: "UserAccountChange");
    }
  }
}
