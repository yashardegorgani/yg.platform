using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace YG.Modules.Access.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class SeedMemberRole : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.InsertData(
                schema: "access",
                table: "roles",
                columns: new[] { "Id", "Description", "Name" },
                values: new object[] { new Guid("6c9e0f5a-2b71-4b8e-9f3d-1a2b3c4d5e6f"), "Default role granted to every registered user", "member" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DeleteData(
                schema: "access",
                table: "roles",
                keyColumn: "Id",
                keyValue: new Guid("6c9e0f5a-2b71-4b8e-9f3d-1a2b3c4d5e6f"));
        }
    }
}
