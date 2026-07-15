using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

#pragma warning disable CA1814 // Prefer jagged arrays over multidimensional

namespace YG.Modules.Access.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddRolePermissions : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "role_permissions",
                schema: "access",
                columns: table => new
                {
                    RoleId = table.Column<Guid>(type: "uuid", nullable: false),
                    Permission = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_role_permissions", x => new { x.RoleId, x.Permission });
                });

            migrationBuilder.InsertData(
                schema: "access",
                table: "role_permissions",
                columns: new[] { "Permission", "RoleId" },
                values: new object[,]
                {
                    { "catalog.products.create", new Guid("6c9e0f5a-2b71-4b8e-9f3d-1a2b3c4d5e6f") },
                    { "catalog.products.list", new Guid("6c9e0f5a-2b71-4b8e-9f3d-1a2b3c4d5e6f") },
                    { "inventory.stock.read", new Guid("6c9e0f5a-2b71-4b8e-9f3d-1a2b3c4d5e6f") },
                    { "purchase.orders.list", new Guid("6c9e0f5a-2b71-4b8e-9f3d-1a2b3c4d5e6f") },
                    { "purchase.orders.place", new Guid("6c9e0f5a-2b71-4b8e-9f3d-1a2b3c4d5e6f") },
                    { "access.grants.manage", new Guid("a1b2c3d4-0000-4000-8000-000000000001") },
                    { "access.roles.manage", new Guid("a1b2c3d4-0000-4000-8000-000000000001") },
                    { "inventory.stock.set", new Guid("a1b2c3d4-0000-4000-8000-000000000001") }
                });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "role_permissions",
                schema: "access");
        }
    }
}
