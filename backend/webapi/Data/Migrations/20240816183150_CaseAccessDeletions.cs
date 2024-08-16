using Microsoft.EntityFrameworkCore.Migrations;
using NodaTime;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

namespace Pidp.Data.Migrations
{
    /// <inheritdoc />
    public partial class CaseAccessDeletions : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.EnsureSchema(
                name: "diam");

            migrationBuilder.RenameTable(
                name: "UserTypeLookup",
                newName: "UserTypeLookup",
                newSchema: "diam");

            migrationBuilder.RenameTable(
                name: "UserAccountChange",
                newName: "UserAccountChange",
                newSchema: "diam");

            migrationBuilder.RenameTable(
                name: "SubmittingAgencyRequest",
                newName: "SubmittingAgencyRequest",
                newSchema: "diam");

            migrationBuilder.RenameTable(
                name: "SubmittingAgencyLookup",
                newName: "SubmittingAgencyLookup",
                newSchema: "diam");

            migrationBuilder.RenameTable(
                name: "PublicUserValidation",
                newName: "PublicUserValidation",
                newSchema: "diam");

            migrationBuilder.RenameTable(
                name: "ProvinceLookup",
                newName: "ProvinceLookup",
                newSchema: "diam");

            migrationBuilder.RenameTable(
                name: "ProcessSection",
                newName: "ProcessSection",
                newSchema: "diam");

            migrationBuilder.RenameTable(
                name: "ProcessFlowEvent",
                newName: "ProcessFlowEvent",
                newSchema: "diam");

            migrationBuilder.RenameTable(
                name: "ProcessFlow",
                newName: "ProcessFlow",
                newSchema: "diam");

            migrationBuilder.RenameTable(
                name: "PartyUserType",
                newName: "PartyUserType",
                newSchema: "diam");

            migrationBuilder.RenameTable(
                name: "PartyOrgainizationDetail",
                newName: "PartyOrgainizationDetail",
                newSchema: "diam");

            migrationBuilder.RenameTable(
                name: "PartyLicenceDeclaration",
                newName: "PartyLicenceDeclaration",
                newSchema: "diam");

            migrationBuilder.RenameTable(
                name: "PartyAlternateId",
                newName: "PartyAlternateId",
                newSchema: "diam");

            migrationBuilder.RenameTable(
                name: "PartyAccessAdministrator",
                newName: "PartyAccessAdministrator",
                newSchema: "diam");

            migrationBuilder.RenameTable(
                name: "Party",
                newName: "Party",
                newSchema: "diam");

            migrationBuilder.RenameTable(
                name: "OutBoxedExportedEvent",
                newName: "OutBoxedExportedEvent",
                newSchema: "diam");

            migrationBuilder.RenameTable(
                name: "OrganizationLookup",
                newName: "OrganizationLookup",
                newSchema: "diam");

            migrationBuilder.RenameTable(
                name: "LawSocietyLookup",
                newName: "LawSocietyLookup",
                newSchema: "diam");

            migrationBuilder.RenameTable(
                name: "LawEnforcementLookup",
                newName: "LawEnforcementLookup",
                newSchema: "diam");

            migrationBuilder.RenameTable(
                name: "JusticeSectorLookup",
                newName: "JusticeSectorLookup",
                newSchema: "diam");

            migrationBuilder.RenameTable(
                name: "JusticeSectorDetail",
                newName: "JusticeSectorDetail",
                newSchema: "diam");

            migrationBuilder.RenameTable(
                name: "IdempotentConsumers",
                newName: "IdempotentConsumers",
                newSchema: "diam");

            migrationBuilder.RenameTable(
                name: "HealthAuthorityLookup",
                newName: "HealthAuthorityLookup",
                newSchema: "diam");

            migrationBuilder.RenameTable(
                name: "HcimEnrolment",
                newName: "HcimEnrolment",
                newSchema: "diam");

            migrationBuilder.RenameTable(
                name: "HcimAccountTransfer",
                newName: "HcimAccountTransfer",
                newSchema: "diam");

            migrationBuilder.RenameTable(
                name: "FutureUserChangeEvent",
                newName: "FutureUserChangeEvent",
                newSchema: "diam");

            migrationBuilder.RenameTable(
                name: "Facility",
                newName: "Facility",
                newSchema: "diam");

            migrationBuilder.RenameTable(
                name: "EndorsementRequest",
                newName: "EndorsementRequest",
                newSchema: "diam");

            migrationBuilder.RenameTable(
                name: "EndorsementRelationship",
                newName: "EndorsementRelationship",
                newSchema: "diam");

            migrationBuilder.RenameTable(
                name: "Endorsement",
                newName: "Endorsement",
                newSchema: "diam");

            migrationBuilder.RenameTable(
                name: "EmailLog",
                newName: "EmailLog",
                newSchema: "diam");

            migrationBuilder.RenameTable(
                name: "DomainEventProcessStatus",
                newName: "DomainEventProcessStatus",
                newSchema: "diam");

            migrationBuilder.RenameTable(
                name: "DigitalEvidencePublicDisclosure",
                newName: "DigitalEvidencePublicDisclosure",
                newSchema: "diam");

            migrationBuilder.RenameTable(
                name: "DigitalEvidenceDisclosure",
                newName: "DigitalEvidenceDisclosure",
                newSchema: "diam");

            migrationBuilder.RenameTable(
                name: "DigitalEvidenceDefence",
                newName: "DigitalEvidenceDefence",
                newSchema: "diam");

            migrationBuilder.RenameTable(
                name: "DigitalEvidence",
                newName: "DigitalEvidence",
                newSchema: "diam");

            migrationBuilder.RenameTable(
                name: "DeferredEvent",
                newName: "DeferredEvent",
                newSchema: "diam");

            migrationBuilder.RenameTable(
                name: "CrownRegionLookup",
                newName: "CrownRegionLookup",
                newSchema: "diam");

            migrationBuilder.RenameTable(
                name: "CourtSubLocation",
                newName: "CourtSubLocation",
                newSchema: "diam");

            migrationBuilder.RenameTable(
                name: "CourtLocationAccessRequest",
                newName: "CourtLocationAccessRequest",
                newSchema: "diam");

            migrationBuilder.RenameTable(
                name: "CourtLocation",
                newName: "CourtLocation",
                newSchema: "diam");

            migrationBuilder.RenameTable(
                name: "CountryLookup",
                newName: "CountryLookup",
                newSchema: "diam");

            migrationBuilder.RenameTable(
                name: "CorrectionServiceLookup",
                newName: "CorrectionServiceLookup",
                newSchema: "diam");

            migrationBuilder.RenameTable(
                name: "CorrectionServiceDetails",
                newName: "CorrectionServiceDetails",
                newSchema: "diam");

            migrationBuilder.RenameTable(
                name: "CollegeLookup",
                newName: "CollegeLookup",
                newSchema: "diam");

            migrationBuilder.RenameTable(
                name: "ClientLog",
                newName: "ClientLog",
                newSchema: "diam");

            migrationBuilder.RenameTable(
                name: "AgencyRequestAttachment",
                newName: "AgencyRequestAttachment",
                newSchema: "diam");

            migrationBuilder.RenameTable(
                name: "Address",
                newName: "Address",
                newSchema: "diam");

            migrationBuilder.RenameTable(
                name: "AccessTypeLookup",
                newName: "AccessTypeLookup",
                newSchema: "diam");

            migrationBuilder.RenameTable(
                name: "AccessRequest",
                newName: "AccessRequest",
                newSchema: "diam");

            migrationBuilder.AddColumn<Instant>(
                name: "DeletedOn",
                schema: "diam",
                table: "SubmittingAgencyRequest",
                type: "timestamp with time zone",
                nullable: false,
                defaultValue: NodaTime.Instant.FromUnixTimeTicks(0L));

            migrationBuilder.CreateTable(
                name: "PlrRecord",
                schema: "diam",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    Created = table.Column<Instant>(type: "timestamp with time zone", nullable: false),
                    Modified = table.Column<Instant>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_PlrRecord", x => x.Id);
                });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "PlrRecord",
                schema: "diam");

            migrationBuilder.DropColumn(
                name: "DeletedOn",
                schema: "diam",
                table: "SubmittingAgencyRequest");

            migrationBuilder.RenameTable(
                name: "UserTypeLookup",
                schema: "diam",
                newName: "UserTypeLookup");

            migrationBuilder.RenameTable(
                name: "UserAccountChange",
                schema: "diam",
                newName: "UserAccountChange");

            migrationBuilder.RenameTable(
                name: "SubmittingAgencyRequest",
                schema: "diam",
                newName: "SubmittingAgencyRequest");

            migrationBuilder.RenameTable(
                name: "SubmittingAgencyLookup",
                schema: "diam",
                newName: "SubmittingAgencyLookup");

            migrationBuilder.RenameTable(
                name: "PublicUserValidation",
                schema: "diam",
                newName: "PublicUserValidation");

            migrationBuilder.RenameTable(
                name: "ProvinceLookup",
                schema: "diam",
                newName: "ProvinceLookup");

            migrationBuilder.RenameTable(
                name: "ProcessSection",
                schema: "diam",
                newName: "ProcessSection");

            migrationBuilder.RenameTable(
                name: "ProcessFlowEvent",
                schema: "diam",
                newName: "ProcessFlowEvent");

            migrationBuilder.RenameTable(
                name: "ProcessFlow",
                schema: "diam",
                newName: "ProcessFlow");

            migrationBuilder.RenameTable(
                name: "PartyUserType",
                schema: "diam",
                newName: "PartyUserType");

            migrationBuilder.RenameTable(
                name: "PartyOrgainizationDetail",
                schema: "diam",
                newName: "PartyOrgainizationDetail");

            migrationBuilder.RenameTable(
                name: "PartyLicenceDeclaration",
                schema: "diam",
                newName: "PartyLicenceDeclaration");

            migrationBuilder.RenameTable(
                name: "PartyAlternateId",
                schema: "diam",
                newName: "PartyAlternateId");

            migrationBuilder.RenameTable(
                name: "PartyAccessAdministrator",
                schema: "diam",
                newName: "PartyAccessAdministrator");

            migrationBuilder.RenameTable(
                name: "Party",
                schema: "diam",
                newName: "Party");

            migrationBuilder.RenameTable(
                name: "OutBoxedExportedEvent",
                schema: "diam",
                newName: "OutBoxedExportedEvent");

            migrationBuilder.RenameTable(
                name: "OrganizationLookup",
                schema: "diam",
                newName: "OrganizationLookup");

            migrationBuilder.RenameTable(
                name: "LawSocietyLookup",
                schema: "diam",
                newName: "LawSocietyLookup");

            migrationBuilder.RenameTable(
                name: "LawEnforcementLookup",
                schema: "diam",
                newName: "LawEnforcementLookup");

            migrationBuilder.RenameTable(
                name: "JusticeSectorLookup",
                schema: "diam",
                newName: "JusticeSectorLookup");

            migrationBuilder.RenameTable(
                name: "JusticeSectorDetail",
                schema: "diam",
                newName: "JusticeSectorDetail");

            migrationBuilder.RenameTable(
                name: "IdempotentConsumers",
                schema: "diam",
                newName: "IdempotentConsumers");

            migrationBuilder.RenameTable(
                name: "HealthAuthorityLookup",
                schema: "diam",
                newName: "HealthAuthorityLookup");

            migrationBuilder.RenameTable(
                name: "HcimEnrolment",
                schema: "diam",
                newName: "HcimEnrolment");

            migrationBuilder.RenameTable(
                name: "HcimAccountTransfer",
                schema: "diam",
                newName: "HcimAccountTransfer");

            migrationBuilder.RenameTable(
                name: "FutureUserChangeEvent",
                schema: "diam",
                newName: "FutureUserChangeEvent");

            migrationBuilder.RenameTable(
                name: "Facility",
                schema: "diam",
                newName: "Facility");

            migrationBuilder.RenameTable(
                name: "EndorsementRequest",
                schema: "diam",
                newName: "EndorsementRequest");

            migrationBuilder.RenameTable(
                name: "EndorsementRelationship",
                schema: "diam",
                newName: "EndorsementRelationship");

            migrationBuilder.RenameTable(
                name: "Endorsement",
                schema: "diam",
                newName: "Endorsement");

            migrationBuilder.RenameTable(
                name: "EmailLog",
                schema: "diam",
                newName: "EmailLog");

            migrationBuilder.RenameTable(
                name: "DomainEventProcessStatus",
                schema: "diam",
                newName: "DomainEventProcessStatus");

            migrationBuilder.RenameTable(
                name: "DigitalEvidencePublicDisclosure",
                schema: "diam",
                newName: "DigitalEvidencePublicDisclosure");

            migrationBuilder.RenameTable(
                name: "DigitalEvidenceDisclosure",
                schema: "diam",
                newName: "DigitalEvidenceDisclosure");

            migrationBuilder.RenameTable(
                name: "DigitalEvidenceDefence",
                schema: "diam",
                newName: "DigitalEvidenceDefence");

            migrationBuilder.RenameTable(
                name: "DigitalEvidence",
                schema: "diam",
                newName: "DigitalEvidence");

            migrationBuilder.RenameTable(
                name: "DeferredEvent",
                schema: "diam",
                newName: "DeferredEvent");

            migrationBuilder.RenameTable(
                name: "CrownRegionLookup",
                schema: "diam",
                newName: "CrownRegionLookup");

            migrationBuilder.RenameTable(
                name: "CourtSubLocation",
                schema: "diam",
                newName: "CourtSubLocation");

            migrationBuilder.RenameTable(
                name: "CourtLocationAccessRequest",
                schema: "diam",
                newName: "CourtLocationAccessRequest");

            migrationBuilder.RenameTable(
                name: "CourtLocation",
                schema: "diam",
                newName: "CourtLocation");

            migrationBuilder.RenameTable(
                name: "CountryLookup",
                schema: "diam",
                newName: "CountryLookup");

            migrationBuilder.RenameTable(
                name: "CorrectionServiceLookup",
                schema: "diam",
                newName: "CorrectionServiceLookup");

            migrationBuilder.RenameTable(
                name: "CorrectionServiceDetails",
                schema: "diam",
                newName: "CorrectionServiceDetails");

            migrationBuilder.RenameTable(
                name: "CollegeLookup",
                schema: "diam",
                newName: "CollegeLookup");

            migrationBuilder.RenameTable(
                name: "ClientLog",
                schema: "diam",
                newName: "ClientLog");

            migrationBuilder.RenameTable(
                name: "AgencyRequestAttachment",
                schema: "diam",
                newName: "AgencyRequestAttachment");

            migrationBuilder.RenameTable(
                name: "Address",
                schema: "diam",
                newName: "Address");

            migrationBuilder.RenameTable(
                name: "AccessTypeLookup",
                schema: "diam",
                newName: "AccessTypeLookup");

            migrationBuilder.RenameTable(
                name: "AccessRequest",
                schema: "diam",
                newName: "AccessRequest");
        }
    }
}
