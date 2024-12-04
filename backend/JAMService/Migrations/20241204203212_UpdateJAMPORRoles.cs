using System.Collections.Generic;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace JAMService.Migrations
{
    /// <inheritdoc />
    public partial class UpdateJAMPORRoles : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.UpdateData(
                schema: "jamservice",
                table: "AppRoleMappings",
                keyColumn: "Id",
                keyValue: -5,
                columns: new[] { "Description", "SourceRoles", "TargetRoles" },
                values: new object[] { "Ability to seal protection orders and mark as removed", new List<string> { "POS_USER", "POS_REMOVE_USER" }, new List<string> { "POR_ADMIN_WITH_SEALING" } });

            migrationBuilder.UpdateData(
                schema: "jamservice",
                table: "AppRoleMappings",
                keyColumn: "Id",
                keyValue: -4,
                columns: new[] { "SourceRoles", "TargetRoles" },
                values: new object[] { new List<string> { "POS_USER", "POS_DEL_USER" }, new List<string> { "POR_ADMIN_WITH_SEALING" } });

            migrationBuilder.UpdateData(
                schema: "jamservice",
                table: "AppRoleMappings",
                keyColumn: "Id",
                keyValue: -3,
                columns: new[] { "SourceRoles", "TargetRoles" },
                values: new object[] { new List<string> { "POS_USER" }, new List<string> { "POR_READ_WRITE" } });

            migrationBuilder.UpdateData(
                schema: "jamservice",
                table: "AppRoleMappings",
                keyColumn: "Id",
                keyValue: -2,
                columns: new[] { "Description", "SourceRoles", "TargetRoles" },
                values: new object[] { "Read-only: Current protection orders only", new List<string> { "POS_SEL_USER", "POS_USER" }, new List<string> { "POR_READ_VALID_ONLY" } });

            migrationBuilder.UpdateData(
                schema: "jamservice",
                table: "AppRoleMappings",
                keyColumn: "Id",
                keyValue: -1,
                columns: new[] { "SourceRoles", "TargetRoles" },
                values: new object[] { new List<string> { "POS_VIEW_ALL_USER", "POS_USER" }, new List<string> { "POR_READ_EXPIRED_ORDERS" } });

            migrationBuilder.UpdateData(
                schema: "jamservice",
                table: "Applications",
                keyColumn: "Id",
                keyValue: -1,
                column: "ValidIDPs",
                value: new List<string> { "azuread" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.UpdateData(
                schema: "jamservice",
                table: "AppRoleMappings",
                keyColumn: "Id",
                keyValue: -5,
                columns: new[] { "Description", "SourceRoles", "TargetRoles" },
                values: new object[] { "BAE Roles, ability to see results on sealed orders queries", new List<string> { "POS_USER", "POS_REMOVE_USER", "POS_JUSTIN" }, new List<string> { "POR_READ_WRITE", "POR_DELETE_ORDER" } });

            migrationBuilder.UpdateData(
                schema: "jamservice",
                table: "AppRoleMappings",
                keyColumn: "Id",
                keyValue: -4,
                columns: new[] { "SourceRoles", "TargetRoles" },
                values: new object[] { new List<string> { "POS_USER", "POS_REMOVE_USER" }, new List<string> { "POR_READ_WRITE", "POR_DELETE_ORDER" } });

            migrationBuilder.UpdateData(
                schema: "jamservice",
                table: "AppRoleMappings",
                keyColumn: "Id",
                keyValue: -3,
                columns: new[] { "SourceRoles", "TargetRoles" },
                values: new object[] { new List<string> { "POS_USER" }, new List<string> { "POR_READ_WRITE" } });

            migrationBuilder.UpdateData(
                schema: "jamservice",
                table: "AppRoleMappings",
                keyColumn: "Id",
                keyValue: -2,
                columns: new[] { "Description", "SourceRoles", "TargetRoles" },
                values: new object[] { "Read-only: Current protection orders and expired", new List<string> { "POS_SEL_USER", "POS_USER" }, new List<string> { "POR_READ_ONLY" } });

            migrationBuilder.UpdateData(
                schema: "jamservice",
                table: "AppRoleMappings",
                keyColumn: "Id",
                keyValue: -1,
                columns: new[] { "SourceRoles", "TargetRoles" },
                values: new object[] { new List<string> { "POS_VIEW_ALL_USER", "POS_USER" }, new List<string> { "POR_READ_ONLY" } });

            migrationBuilder.UpdateData(
                schema: "jamservice",
                table: "Applications",
                keyColumn: "Id",
                keyValue: -1,
                column: "ValidIDPs",
                value: new List<string> { "azuread" });
        }
    }
}
