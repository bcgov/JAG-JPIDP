using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Pidp.Data.Migrations
{
    public partial class ProcessFlowsDataChange : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AlterColumn<decimal>(
                name: "Sequence",
                table: "ProcessFlow",
                type: "numeric(3,2)",
                nullable: false,
                oldClrType: typeof(int),
                oldType: "integer");
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AlterColumn<int>(
                name: "Sequence",
                table: "ProcessFlow",
                type: "integer",
                nullable: false,
                oldClrType: typeof(decimal),
                oldType: "numeric(3,2)");
        }
    }
}
