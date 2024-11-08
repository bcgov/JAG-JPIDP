using System;
using Microsoft.EntityFrameworkCore.Migrations;
using NodaTime;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

namespace jumwebapi.Migrations
{
    /// <inheritdoc />
    public partial class ParticipantMerging : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_JustinAgencyAssignment_JustinAgency_AgencyId",
                table: "JustinAgencyAssignment");

            migrationBuilder.EnsureSchema(
                name: "jum");

            migrationBuilder.RenameTable(
                name: "Province",
                newName: "Province",
                newSchema: "jum");

            migrationBuilder.RenameTable(
                name: "PartyTypeLookup",
                newName: "PartyTypeLookup",
                newSchema: "jum");

            migrationBuilder.RenameTable(
                name: "JustinUserRole",
                newName: "JustinUserRole",
                newSchema: "jum");

            migrationBuilder.RenameTable(
                name: "JustinUserChangeTarget",
                newName: "JustinUserChangeTarget",
                newSchema: "jum");

            migrationBuilder.RenameTable(
                name: "JustinUserChange",
                newName: "JustinUserChange",
                newSchema: "jum");

            migrationBuilder.RenameTable(
                name: "JustinUser",
                newName: "JustinUser",
                newSchema: "jum");

            migrationBuilder.RenameTable(
                name: "JustinRole",
                newName: "JustinRole",
                newSchema: "jum");

            migrationBuilder.RenameTable(
                name: "JustinPerson",
                newName: "JustinPerson",
                newSchema: "jum");

            migrationBuilder.RenameTable(
                name: "JustinIdentityProvider",
                newName: "JustinIdentityProvider",
                newSchema: "jum");

            migrationBuilder.RenameTable(
                name: "JustinAgencyAssignment",
                newName: "JustinAgencyAssignment",
                newSchema: "jum");

            migrationBuilder.RenameTable(
                name: "JustinAgency",
                newName: "JustinAgency",
                newSchema: "jum");

            migrationBuilder.RenameTable(
                name: "JustinAddress",
                newName: "JustinAddress",
                newSchema: "jum");

            migrationBuilder.RenameTable(
                name: "CountryLookup",
                newName: "CountryLookup",
                newSchema: "jum");

            migrationBuilder.AlterColumn<long>(
                name: "AgencyId",
                schema: "jum",
                table: "JustinAgencyAssignment",
                type: "bigint",
                nullable: true,
                oldClrType: typeof(long),
                oldType: "bigint");

            migrationBuilder.CreateTable(
                name: "ParticipantMerge",
                schema: "jum",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    MergeEventTime = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    SourceParticipantId = table.Column<string>(type: "text", nullable: false),
                    TargetParticipantId = table.Column<string>(type: "text", nullable: false),
                    Created = table.Column<Instant>(type: "timestamp with time zone", nullable: false),
                    Modified = table.Column<Instant>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ParticipantMerge", x => x.Id);
                });

            migrationBuilder.AddForeignKey(
                name: "FK_JustinAgencyAssignment_JustinAgency_AgencyId",
                schema: "jum",
                table: "JustinAgencyAssignment",
                column: "AgencyId",
                principalSchema: "jum",
                principalTable: "JustinAgency",
                principalColumn: "AgencyId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_JustinAgencyAssignment_JustinAgency_AgencyId",
                schema: "jum",
                table: "JustinAgencyAssignment");

            migrationBuilder.DropTable(
                name: "ParticipantMerge",
                schema: "jum");

            migrationBuilder.RenameTable(
                name: "Province",
                schema: "jum",
                newName: "Province");

            migrationBuilder.RenameTable(
                name: "PartyTypeLookup",
                schema: "jum",
                newName: "PartyTypeLookup");

            migrationBuilder.RenameTable(
                name: "JustinUserRole",
                schema: "jum",
                newName: "JustinUserRole");

            migrationBuilder.RenameTable(
                name: "JustinUserChangeTarget",
                schema: "jum",
                newName: "JustinUserChangeTarget");

            migrationBuilder.RenameTable(
                name: "JustinUserChange",
                schema: "jum",
                newName: "JustinUserChange");

            migrationBuilder.RenameTable(
                name: "JustinUser",
                schema: "jum",
                newName: "JustinUser");

            migrationBuilder.RenameTable(
                name: "JustinRole",
                schema: "jum",
                newName: "JustinRole");

            migrationBuilder.RenameTable(
                name: "JustinPerson",
                schema: "jum",
                newName: "JustinPerson");

            migrationBuilder.RenameTable(
                name: "JustinIdentityProvider",
                schema: "jum",
                newName: "JustinIdentityProvider");

            migrationBuilder.RenameTable(
                name: "JustinAgencyAssignment",
                schema: "jum",
                newName: "JustinAgencyAssignment");

            migrationBuilder.RenameTable(
                name: "JustinAgency",
                schema: "jum",
                newName: "JustinAgency");

            migrationBuilder.RenameTable(
                name: "JustinAddress",
                schema: "jum",
                newName: "JustinAddress");

            migrationBuilder.RenameTable(
                name: "CountryLookup",
                schema: "jum",
                newName: "CountryLookup");

            migrationBuilder.AlterColumn<long>(
                name: "AgencyId",
                table: "JustinAgencyAssignment",
                type: "bigint",
                nullable: false,
                defaultValue: 0L,
                oldClrType: typeof(long),
                oldType: "bigint",
                oldNullable: true);

            migrationBuilder.AddForeignKey(
                name: "FK_JustinAgencyAssignment_JustinAgency_AgencyId",
                table: "JustinAgencyAssignment",
                column: "AgencyId",
                principalTable: "JustinAgency",
                principalColumn: "AgencyId",
                onDelete: ReferentialAction.Cascade);
        }
    }
}
