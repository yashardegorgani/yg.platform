using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace YG.Modules.Workflow.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddTaskAssignment : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "AssignedToSub",
                schema: "workflow",
                table: "workflow_tasks",
                type: "text",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "AssignedToSub",
                schema: "workflow",
                table: "workflow_tasks");
        }
    }
}
