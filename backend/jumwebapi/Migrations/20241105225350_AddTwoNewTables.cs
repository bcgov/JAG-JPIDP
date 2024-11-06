using System;
using Microsoft.EntityFrameworkCore.Migrations;
using NodaTime;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

namespace jumwebapi.Migrations
{
    /// <inheritdoc />
    public partial class AddTwoNewTables : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            
            migrationBuilder.DropTable(
                name: "ParticipantMerge",
                schema: "jum");

            migrationBuilder.CreateTable(
                name: "IdempotentConsumers",
                schema: "jum",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    MessageId = table.Column<string>(type: "text", nullable: false),
                    Consumer = table.Column<string>(type: "text", nullable: false),
                    ConsumeDate = table.Column<Instant>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_IdempotentConsumers", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "ParticipantMerges",
                schema: "jum",
                columns: table => new
                {
                    Id = table.Column<string>(type: "text", nullable: false),
                    MergeEventTime = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    SourceParticipantId = table.Column<string>(type: "text", nullable: false),
                    TargetParticipantId = table.Column<string>(type: "text", nullable: false),
                    Created = table.Column<Instant>(type: "timestamp with time zone", nullable: false),
                    Modified = table.Column<Instant>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ParticipantMerges", x => x.Id);
                });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "IdempotentConsumers",
                schema: "jum");

            migrationBuilder.DropTable(
                name: "ParticipantMerges",
                schema: "jum");

            migrationBuilder.CreateTable(
                name: "IdempotentConsumer",
                schema: "jum",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    ConsumeDate = table.Column<Instant>(type: "timestamp with time zone", nullable: false),
                    Consumer = table.Column<string>(type: "text", nullable: false),
                    MessageId = table.Column<string>(type: "text", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_IdempotentConsumer", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "ParticipantMerge",
                schema: "jum",
                columns: table => new
                {
                    Id = table.Column<string>(type: "text", nullable: false),
                    Created = table.Column<Instant>(type: "timestamp with time zone", nullable: false),
                    MergeEventTime = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    Modified = table.Column<Instant>(type: "timestamp with time zone", nullable: false),
                    SourceParticipantId = table.Column<string>(type: "text", nullable: false),
                    TargetParticipantId = table.Column<string>(type: "text", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ParticipantMerge", x => x.Id);
                });
        }
    }
}
