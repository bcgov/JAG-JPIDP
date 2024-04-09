using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Pidp.Data.Migrations
{
  public partial class AddOrganizationLookups : Migration
  {
    protected override void Up(MigrationBuilder migrationBuilder)
    {
      migrationBuilder.DropColumn(
          name: "AgencyCode",
          table: "SubmittingAgencyRequest");

      //migrationBuilder.InsertData(
      // table: "OrganizationLookup",
      // columns: new[] { "Code", "IdpHint", "Name" },
      //values: new object[,]
      //{
      //              { 1, "ADFS", "Justice Sector" },
      //              { 2, "", "BC Law Enforcement" },
      //              { 3, "vcc", "BC Law Society" },
      //              { 4, "", "BC Corrections Service" },
      //              { 5, "", "Health Authority" },
      //              { 6, "idir", "BC Government Ministry" },
      //              { 7, "icbc", "ICBC" },
      //              { 8, "", "Other" },
      //              { 9, "vicpd", "Victoria Police Department" },
      //              { 10, "deltapd", "Delta Police Department" },
      //              { 11, "saanichpd", "Saanich Police Department" },
      //                   { 12, "", "Test Police Department" }
      //});
    }

    protected override void Down(MigrationBuilder migrationBuilder)
    {
      migrationBuilder.AddColumn<string>(
          name: "AgencyCode",
          table: "SubmittingAgencyRequest",
          type: "text",
          nullable: false,
          defaultValue: "");
    }
  }
}
