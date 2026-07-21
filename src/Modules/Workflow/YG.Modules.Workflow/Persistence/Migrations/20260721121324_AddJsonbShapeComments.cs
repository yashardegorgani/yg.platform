using System.Collections.Generic;
using System.Text.Json;
using Microsoft.EntityFrameworkCore.Migrations;
using YG.Modules.Workflow.Domain.Definition;

#nullable disable

namespace YG.Modules.Workflow.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddJsonbShapeComments : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AlterColumn<Dictionary<string, JsonElement>>(
                name: "CapturedData",
                schema: "workflow",
                table: "workflow_step_instances",
                type: "jsonb",
                nullable: true,
                comment: "Form data captured at step completion: {field: value}, shape defined by the step's formRef. Frozen once written.",
                oldClrType: typeof(Dictionary<string, JsonElement>),
                oldType: "jsonb",
                oldNullable: true);

            migrationBuilder.AlterColumn<Dictionary<string, JsonElement>>(
                name: "Context",
                schema: "workflow",
                table: "workflow_instances",
                type: "jsonb",
                nullable: false,
                comment: "Accumulated state keyed by step id: {stepId: {field: value}}. Field shapes vary per definition. Steps write it; transitions only read it.",
                oldClrType: typeof(Dictionary<string, JsonElement>),
                oldType: "jsonb",
                oldComment: "Accumulated state, keyed by step id. Steps write it; transitions only read it.");

            migrationBuilder.AlterColumn<JsonElement>(
                name: "Data",
                schema: "workflow",
                table: "workflow_history",
                type: "jsonb",
                nullable: true,
                comment: "Action payload, shape per action: task-reassigned {toSub?, toRole?} | task-completed {data} | activity-failed/pended {error, attempts} | note-added {text}.",
                oldClrType: typeof(JsonElement),
                oldType: "jsonb",
                oldNullable: true);

            migrationBuilder.AlterColumn<JsonElement>(
                name: "ActorSnapshot",
                schema: "workflow",
                table: "workflow_history",
                type: "jsonb",
                nullable: true,
                comment: "Actor identity frozen at write time: {sub, username, roles: [string]}. Null = the engine acted.",
                oldClrType: typeof(JsonElement),
                oldType: "jsonb",
                oldNullable: true);

            migrationBuilder.AlterColumn<DefinitionDocument>(
                name: "Document",
                schema: "workflow",
                table: "workflow_definitions",
                type: "jsonb",
                nullable: false,
                comment: "The definition graph: {startStepId, steps: [{id, kind: Human|Automatic, role?, formRef?, activity?: {ref, input}, onFailure?: {maxRetries, onExhausted: Pend|Continue}}], transitions: [{from, to, condition?: {field, op, value}}]}",
                oldClrType: typeof(DefinitionDocument),
                oldType: "jsonb",
                oldComment: "The definition graph: steps, transitions, activities, form references.");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AlterColumn<Dictionary<string, JsonElement>>(
                name: "CapturedData",
                schema: "workflow",
                table: "workflow_step_instances",
                type: "jsonb",
                nullable: true,
                oldClrType: typeof(Dictionary<string, JsonElement>),
                oldType: "jsonb",
                oldNullable: true,
                oldComment: "Form data captured at step completion: {field: value}, shape defined by the step's formRef. Frozen once written.");

            migrationBuilder.AlterColumn<Dictionary<string, JsonElement>>(
                name: "Context",
                schema: "workflow",
                table: "workflow_instances",
                type: "jsonb",
                nullable: false,
                comment: "Accumulated state, keyed by step id. Steps write it; transitions only read it.",
                oldClrType: typeof(Dictionary<string, JsonElement>),
                oldType: "jsonb",
                oldComment: "Accumulated state keyed by step id: {stepId: {field: value}}. Field shapes vary per definition. Steps write it; transitions only read it.");

            migrationBuilder.AlterColumn<JsonElement>(
                name: "Data",
                schema: "workflow",
                table: "workflow_history",
                type: "jsonb",
                nullable: true,
                oldClrType: typeof(JsonElement),
                oldType: "jsonb",
                oldNullable: true,
                oldComment: "Action payload, shape per action: task-reassigned {toSub?, toRole?} | task-completed {data} | activity-failed/pended {error, attempts} | note-added {text}.");

            migrationBuilder.AlterColumn<JsonElement>(
                name: "ActorSnapshot",
                schema: "workflow",
                table: "workflow_history",
                type: "jsonb",
                nullable: true,
                oldClrType: typeof(JsonElement),
                oldType: "jsonb",
                oldNullable: true,
                oldComment: "Actor identity frozen at write time: {sub, username, roles: [string]}. Null = the engine acted.");

            migrationBuilder.AlterColumn<DefinitionDocument>(
                name: "Document",
                schema: "workflow",
                table: "workflow_definitions",
                type: "jsonb",
                nullable: false,
                comment: "The definition graph: steps, transitions, activities, form references.",
                oldClrType: typeof(DefinitionDocument),
                oldType: "jsonb",
                oldComment: "The definition graph: {startStepId, steps: [{id, kind: Human|Automatic, role?, formRef?, activity?: {ref, input}, onFailure?: {maxRetries, onExhausted: Pend|Continue}}], transitions: [{from, to, condition?: {field, op, value}}]}");
        }
    }
}
