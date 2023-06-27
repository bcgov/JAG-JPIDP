using Microsoft.EntityFrameworkCore.Migrations;
using NodaTime;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

namespace Pidp.Data.Migrations
{
    public partial class ProcessFlows : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "ProcessSection",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    Name = table.Column<string>(type: "text", nullable: false),
                    Created = table.Column<Instant>(type: "timestamp with time zone", nullable: false),
                    Modified = table.Column<Instant>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ProcessSection", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "ProcessFlow",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    Sequence = table.Column<int>(type: "integer", nullable: false),
                    ProcessSectionId = table.Column<int>(type: "integer", nullable: true),
                    IsLocked = table.Column<bool>(type: "boolean", nullable: false),
                    IdentityProvider = table.Column<string>(type: "text", nullable: false),
                    AccessTypeCode = table.Column<int>(type: "integer", nullable: false),
                    Created = table.Column<Instant>(type: "timestamp with time zone", nullable: false),
                    Modified = table.Column<Instant>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ProcessFlow", x => x.Id);
                    table.ForeignKey(
                        name: "FK_ProcessFlow_ProcessSection_ProcessSectionId",
                        column: x => x.ProcessSectionId,
                        principalTable: "ProcessSection",
                        principalColumn: "Id");
                });

            migrationBuilder.CreateTable(
                name: "ProcessFlowEvent",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    ProcessFlowId = table.Column<int>(type: "integer", nullable: true),
                    FromDomainEvent = table.Column<string>(type: "text", nullable: false),
                    ToDomainEvent = table.Column<string>(type: "text", nullable: false),
                    Created = table.Column<Instant>(type: "timestamp with time zone", nullable: false),
                    Modified = table.Column<Instant>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ProcessFlowEvent", x => x.Id);
                    table.ForeignKey(
                        name: "FK_ProcessFlowEvent_ProcessFlow_ProcessFlowId",
                        column: x => x.ProcessFlowId,
                        principalTable: "ProcessFlow",
                        principalColumn: "Id");
                });

            migrationBuilder.CreateTable(
                name: "DomainEventProcessStatus",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    RequestId = table.Column<int>(type: "integer", nullable: false),
                    ProcessFlowEventId = table.Column<int>(type: "integer", nullable: true),
                    Status = table.Column<int>(type: "integer", nullable: true),
                    Errors = table.Column<string>(type: "text", nullable: false),
                    Created = table.Column<Instant>(type: "timestamp with time zone", nullable: false),
                    Modified = table.Column<Instant>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_DomainEventProcessStatus", x => x.Id);
                    table.ForeignKey(
                        name: "FK_DomainEventProcessStatus_ProcessFlowEvent_ProcessFlowEventId",
                        column: x => x.ProcessFlowEventId,
                        principalTable: "ProcessFlowEvent",
                        principalColumn: "Id");
                });

            migrationBuilder.CreateIndex(
                name: "IX_DomainEventProcessStatus_ProcessFlowEventId",
                table: "DomainEventProcessStatus",
                column: "ProcessFlowEventId");

            migrationBuilder.CreateIndex(
                name: "IX_ProcessFlow_ProcessSectionId",
                table: "ProcessFlow",
                column: "ProcessSectionId");

            migrationBuilder.CreateIndex(
                name: "IX_ProcessFlowEvent_ProcessFlowId",
                table: "ProcessFlowEvent",
                column: "ProcessFlowId");
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "DomainEventProcessStatus");

            migrationBuilder.DropTable(
                name: "ProcessFlowEvent");

            migrationBuilder.DropTable(
                name: "ProcessFlow");

            migrationBuilder.DropTable(
                name: "ProcessSection");
        }
    }
}
