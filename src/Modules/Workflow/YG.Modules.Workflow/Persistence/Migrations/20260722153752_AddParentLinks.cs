using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace YG.Modules.Workflow.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddParentLinks : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AlterColumn<Guid>(
                name: "ParentInstanceId",
                schema: "workflow",
                table: "workflow_instances",
                type: "uuid",
                nullable: true,
                comment: "Spawning instance, when this is a child. Null = top-level.",
                oldClrType: typeof(Guid),
                oldType: "uuid",
                oldNullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "ParentStepInstanceId",
                schema: "workflow",
                table: "workflow_instances",
                type: "uuid",
                nullable: true,
                comment: "The parent's SubWorkflow step instance awaiting this child's completion.");

            migrationBuilder.CreateIndex(
                name: "IX_workflow_instances_ParentInstanceId",
                schema: "workflow",
                table: "workflow_instances",
                column: "ParentInstanceId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_workflow_instances_ParentInstanceId",
                schema: "workflow",
                table: "workflow_instances");

            migrationBuilder.DropColumn(
                name: "ParentStepInstanceId",
                schema: "workflow",
                table: "workflow_instances");

            migrationBuilder.AlterColumn<Guid>(
                name: "ParentInstanceId",
                schema: "workflow",
                table: "workflow_instances",
                type: "uuid",
                nullable: true,
                oldClrType: typeof(Guid),
                oldType: "uuid",
                oldNullable: true,
                oldComment: "Spawning instance, when this is a child. Null = top-level.");
        }
    }
}
