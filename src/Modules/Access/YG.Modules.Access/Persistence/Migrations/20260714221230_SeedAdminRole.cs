using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace YG.Modules.Access.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class SeedAdminRole : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.InsertData(
                schema: "access",
                table: "roles",
                columns: new[] { "Id", "Description", "Name" },
                values: new object[] { new Guid("a1b2c3d4-0000-4000-8000-000000000001"), "Access administration", "admin" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DeleteData(
                schema: "access",
                table: "roles",
                keyColumn: "Id",
                keyValue: new Guid("a1b2c3d4-0000-4000-8000-000000000001"));
        }
    }
}
