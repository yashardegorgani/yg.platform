using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace YG.Modules.Access.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AuditedRoleAssignments : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<DateTimeOffset>(
                name: "GrantedAt",
                schema: "access",
                table: "user_roles",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "GrantedBy",
                schema: "access",
                table: "user_roles",
                type: "character varying(64)",
                maxLength: 64,
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_user_roles_RoleId",
                schema: "access",
                table: "user_roles",
                column: "RoleId");

            migrationBuilder.AddForeignKey(
                name: "FK_user_roles_roles_RoleId",
                schema: "access",
                table: "user_roles",
                column: "RoleId",
                principalSchema: "access",
                principalTable: "roles",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_user_roles_roles_RoleId",
                schema: "access",
                table: "user_roles");

            migrationBuilder.DropIndex(
                name: "IX_user_roles_RoleId",
                schema: "access",
                table: "user_roles");

            migrationBuilder.DropColumn(
                name: "GrantedAt",
                schema: "access",
                table: "user_roles");

            migrationBuilder.DropColumn(
                name: "GrantedBy",
                schema: "access",
                table: "user_roles");
        }
    }
}
