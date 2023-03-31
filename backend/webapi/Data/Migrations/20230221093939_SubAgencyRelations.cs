using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Pidp.Data.Migrations
{
    public partial class SubAgencyRelations : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_SubmittingAgencyRequest_AgencyRequestAttachment_AgencyReque~",
                table: "SubmittingAgencyRequest");

            migrationBuilder.DropIndex(
                name: "IX_SubmittingAgencyRequest_AgencyRequestAttachmentAttachmentId",
                table: "SubmittingAgencyRequest");

            migrationBuilder.DropColumn(
                name: "AgencyRequestAttachmentAttachmentId",
                table: "SubmittingAgencyRequest");

            migrationBuilder.AddColumn<int>(
                name: "SubmittingAgencyRequestRequestId",
                table: "AgencyRequestAttachment",
                type: "integer",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.CreateIndex(
                name: "IX_AgencyRequestAttachment_SubmittingAgencyRequestRequestId",
                table: "AgencyRequestAttachment",
                column: "SubmittingAgencyRequestRequestId");

            migrationBuilder.AddForeignKey(
                name: "FK_AgencyRequestAttachment_SubmittingAgencyRequest_SubmittingA~",
                table: "AgencyRequestAttachment",
                column: "SubmittingAgencyRequestRequestId",
                principalTable: "SubmittingAgencyRequest",
                principalColumn: "RequestId",
                onDelete: ReferentialAction.Cascade);
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_AgencyRequestAttachment_SubmittingAgencyRequest_SubmittingA~",
                table: "AgencyRequestAttachment");

            migrationBuilder.DropIndex(
                name: "IX_AgencyRequestAttachment_SubmittingAgencyRequestRequestId",
                table: "AgencyRequestAttachment");

            migrationBuilder.DropColumn(
                name: "SubmittingAgencyRequestRequestId",
                table: "AgencyRequestAttachment");

            migrationBuilder.AddColumn<int>(
                name: "AgencyRequestAttachmentAttachmentId",
                table: "SubmittingAgencyRequest",
                type: "integer",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_SubmittingAgencyRequest_AgencyRequestAttachmentAttachmentId",
                table: "SubmittingAgencyRequest",
                column: "AgencyRequestAttachmentAttachmentId");

            migrationBuilder.AddForeignKey(
                name: "FK_SubmittingAgencyRequest_AgencyRequestAttachment_AgencyReque~",
                table: "SubmittingAgencyRequest",
                column: "AgencyRequestAttachmentAttachmentId",
                principalTable: "AgencyRequestAttachment",
                principalColumn: "AttachmentId");
        }
    }
}
