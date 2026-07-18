using System;
using System.Collections.Generic;
using System.Text.Json;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace YG.Modules.Workflow.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddRuntimeTables : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "workflow_instances",
                schema: "workflow",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    DefinitionKey = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    DefinitionVersion = table.Column<int>(type: "integer", nullable: false),
                    Status = table.Column<string>(type: "character varying(30)", maxLength: 30, nullable: false, comment: "Running | Completed"),
                    BusinessKey = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: true, comment: "Opaque reference to a business entity in some module. Never a FK."),
                    ParentInstanceId = table.Column<Guid>(type: "uuid", nullable: true),
                    Context = table.Column<Dictionary<string, JsonElement>>(type: "jsonb", nullable: false, comment: "Accumulated state, keyed by step id. Steps write it; transitions only read it."),
                    StartedBy = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    StartedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    CompletedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_workflow_instances", x => x.Id);
                },
                comment: "Executions. Definition version is pinned at start and never changes.");

            migrationBuilder.CreateTable(
                name: "workflow_history",
                schema: "workflow",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    InstanceId = table.Column<Guid>(type: "uuid", nullable: false),
                    StepId = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true),
                    Action = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false, comment: "instance-started | task-created | task-claimed | task-completed | instance-completed"),
                    Actor = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true),
                    Data = table.Column<JsonElement>(type: "jsonb", nullable: true),
                    OccurredAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_workflow_history", x => x.Id);
                    table.ForeignKey(
                        name: "FK_workflow_history_workflow_instances_InstanceId",
                        column: x => x.InstanceId,
                        principalSchema: "workflow",
                        principalTable: "workflow_instances",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                },
                comment: "Append-only audit trail. Never updated, never deleted.");

            migrationBuilder.CreateTable(
                name: "workflow_step_instances",
                schema: "workflow",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    InstanceId = table.Column<Guid>(type: "uuid", nullable: false),
                    StepId = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false, comment: "Step id inside the pinned definition document."),
                    Status = table.Column<string>(type: "character varying(30)", maxLength: 30, nullable: false),
                    CapturedData = table.Column<Dictionary<string, JsonElement>>(type: "jsonb", nullable: true),
                    StartedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    CompletedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    CompletedBy = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_workflow_step_instances", x => x.Id);
                    table.ForeignKey(
                        name: "FK_workflow_step_instances_workflow_instances_InstanceId",
                        column: x => x.InstanceId,
                        principalSchema: "workflow",
                        principalTable: "workflow_instances",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                },
                comment: "One row per activated step. captured_data is frozen once the step completes.");

            migrationBuilder.CreateTable(
                name: "workflow_tasks",
                schema: "workflow",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    InstanceId = table.Column<Guid>(type: "uuid", nullable: false),
                    StepInstanceId = table.Column<Guid>(type: "uuid", nullable: false),
                    DefinitionKey = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    StepId = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    Role = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false, comment: "Opaque role name; holders see the task in their inbox."),
                    FormRef = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: true),
                    Status = table.Column<string>(type: "character varying(30)", maxLength: 30, nullable: false, comment: "Open | Claimed | Completed"),
                    ClaimedBy = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true),
                    ClaimedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    CreatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    CompletedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_workflow_tasks", x => x.Id);
                    table.ForeignKey(
                        name: "FK_workflow_tasks_workflow_instances_InstanceId",
                        column: x => x.InstanceId,
                        principalSchema: "workflow",
                        principalTable: "workflow_instances",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_workflow_tasks_workflow_step_instances_StepInstanceId",
                        column: x => x.StepInstanceId,
                        principalSchema: "workflow",
                        principalTable: "workflow_step_instances",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                },
                comment: "The inbox. Open tasks are visible to all role holders; claiming takes ownership.");

            migrationBuilder.CreateIndex(
                name: "IX_workflow_history_InstanceId",
                schema: "workflow",
                table: "workflow_history",
                column: "InstanceId");

            migrationBuilder.CreateIndex(
                name: "IX_workflow_instances_BusinessKey",
                schema: "workflow",
                table: "workflow_instances",
                column: "BusinessKey");

            migrationBuilder.CreateIndex(
                name: "IX_workflow_instances_Status",
                schema: "workflow",
                table: "workflow_instances",
                column: "Status");

            migrationBuilder.CreateIndex(
                name: "IX_workflow_step_instances_InstanceId",
                schema: "workflow",
                table: "workflow_step_instances",
                column: "InstanceId");

            migrationBuilder.CreateIndex(
                name: "IX_workflow_tasks_ClaimedBy",
                schema: "workflow",
                table: "workflow_tasks",
                column: "ClaimedBy");

            migrationBuilder.CreateIndex(
                name: "IX_workflow_tasks_InstanceId",
                schema: "workflow",
                table: "workflow_tasks",
                column: "InstanceId");

            migrationBuilder.CreateIndex(
                name: "IX_workflow_tasks_Status_Role",
                schema: "workflow",
                table: "workflow_tasks",
                columns: new[] { "Status", "Role" });

            migrationBuilder.CreateIndex(
                name: "IX_workflow_tasks_StepInstanceId",
                schema: "workflow",
                table: "workflow_tasks",
                column: "StepInstanceId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "workflow_history",
                schema: "workflow");

            migrationBuilder.DropTable(
                name: "workflow_tasks",
                schema: "workflow");

            migrationBuilder.DropTable(
                name: "workflow_step_instances",
                schema: "workflow");

            migrationBuilder.DropTable(
                name: "workflow_instances",
                schema: "workflow");
        }
    }
}
