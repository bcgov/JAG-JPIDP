using Microsoft.EntityFrameworkCore.Migrations;
using NodaTime;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

namespace Pidp.Data.Migrations
{
    public partial class SubmittingAgencyAttachment : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropPrimaryKey(
                name: "PK_SubmittingAgencyRequests",
                table: "SubmittingAgencyRequests");

            migrationBuilder.RenameTable(
                name: "SubmittingAgencyRequests",
                newName: "SubmittingAgencyRequest");

            migrationBuilder.AddColumn<int>(
                name: "AgencyRequestAttachmentAttachmentId",
                table: "SubmittingAgencyRequest",
                type: "integer",
                nullable: true);

            migrationBuilder.AddPrimaryKey(
                name: "PK_SubmittingAgencyRequest",
                table: "SubmittingAgencyRequest",
                column: "RequestId");

            migrationBuilder.CreateTable(
                name: "AgencyRequestAttachment",
                columns: table => new
                {
                    AttachmentId = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    AttachmentName = table.Column<string>(type: "text", nullable: false),
                    AttachmentType = table.Column<string>(type: "text", nullable: false),
                    UploadStatus = table.Column<string>(type: "text", nullable: false),
                    Created = table.Column<Instant>(type: "timestamp with time zone", nullable: false),
                    Modified = table.Column<Instant>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AgencyRequestAttachment", x => x.AttachmentId);
                });

            migrationBuilder.CreateIndex(
                name: "IX_SubmittingAgencyRequest_AgencyRequestAttachmentAttachmentId",
                table: "SubmittingAgencyRequest",
                column: "AgencyRequestAttachmentAttachmentId");

            migrationBuilder.CreateIndex(
                name: "IX_SubmittingAgencyRequest_PartyId",
                table: "SubmittingAgencyRequest",
                column: "PartyId");

            migrationBuilder.AddForeignKey(
                name: "FK_SubmittingAgencyRequest_AgencyRequestAttachment_AgencyReque~",
                table: "SubmittingAgencyRequest",
                column: "AgencyRequestAttachmentAttachmentId",
                principalTable: "AgencyRequestAttachment",
                principalColumn: "AttachmentId");

            migrationBuilder.AddForeignKey(
                name: "FK_SubmittingAgencyRequest_Party_PartyId",
                table: "SubmittingAgencyRequest",
                column: "PartyId",
                principalTable: "Party",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_SubmittingAgencyRequest_AgencyRequestAttachment_AgencyReque~",
                table: "SubmittingAgencyRequest");

            migrationBuilder.DropForeignKey(
                name: "FK_SubmittingAgencyRequest_Party_PartyId",
                table: "SubmittingAgencyRequest");

            migrationBuilder.DropTable(
                name: "AgencyRequestAttachment");

            migrationBuilder.DropPrimaryKey(
                name: "PK_SubmittingAgencyRequest",
                table: "SubmittingAgencyRequest");

            migrationBuilder.DropIndex(
                name: "IX_SubmittingAgencyRequest_AgencyRequestAttachmentAttachmentId",
                table: "SubmittingAgencyRequest");

            migrationBuilder.DropIndex(
                name: "IX_SubmittingAgencyRequest_PartyId",
                table: "SubmittingAgencyRequest");

            migrationBuilder.DropColumn(
                name: "AgencyRequestAttachmentAttachmentId",
                table: "SubmittingAgencyRequest");

            migrationBuilder.RenameTable(
                name: "SubmittingAgencyRequest",
                newName: "SubmittingAgencyRequests");

            migrationBuilder.AddPrimaryKey(
                name: "PK_SubmittingAgencyRequests",
                table: "SubmittingAgencyRequests",
                column: "RequestId");
        }
    }
}
