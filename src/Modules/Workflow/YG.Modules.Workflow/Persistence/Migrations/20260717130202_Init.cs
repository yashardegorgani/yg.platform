using System;
using Microsoft.EntityFrameworkCore.Migrations;
using YG.Modules.Workflow.Domain.Definition;

#nullable disable

namespace YG.Modules.Workflow.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class Init : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.EnsureSchema(
                name: "workflow");

            migrationBuilder.CreateTable(
                name: "workflow_definitions",
                schema: "workflow",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    Key = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false, comment: "Stable workflow identity across versions (kebab-case)."),
                    Version = table.Column<int>(type: "integer", nullable: false),
                    Name = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    Status = table.Column<string>(type: "character varying(30)", maxLength: 30, nullable: false, comment: "Draft | PendingApproval | Published"),
                    Document = table.Column<DefinitionDocument>(type: "jsonb", nullable: false, comment: "The definition graph: steps, transitions, activities, form references."),
                    CreatedBy = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    CreatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    SubmittedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    PublishedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    PublishedBy = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true),
                    ReviewNote = table.Column<string>(type: "character varying(2000)", maxLength: 2000, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_workflow_definitions", x => x.Id);
                },
                comment: "Designed workflow definitions. One row per version; Published rows are immutable.");

            migrationBuilder.CreateIndex(
                name: "IX_workflow_definitions_Key_Version",
                schema: "workflow",
                table: "workflow_definitions",
                columns: new[] { "Key", "Version" },
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "workflow_definitions",
                schema: "workflow");
        }
    }
}
