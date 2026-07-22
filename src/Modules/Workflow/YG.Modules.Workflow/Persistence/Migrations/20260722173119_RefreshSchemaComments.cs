using Microsoft.EntityFrameworkCore.Migrations;
using YG.Modules.Workflow.Domain.Definition;

#nullable disable

namespace YG.Modules.Workflow.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class RefreshSchemaComments : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AlterColumn<string>(
                name: "Action",
                schema: "workflow",
                table: "workflow_history",
                type: "character varying(50)",
                maxLength: 50,
                nullable: false,
                comment: "instance-started | task-created | task-claimed | task-completed | task-reassigned | note-added | activity-scheduled | activity-failed | activity-pended | activity-continued | activity-completed | activity-resumed | branch-completed | join-waiting | child-scheduled | child-started | child-failed | child-completed | instance-completed",
                oldClrType: typeof(string),
                oldType: "character varying(50)",
                oldMaxLength: 50,
                oldComment: "instance-started | task-created | task-claimed | task-completed | task-reassigned | note-added | activity-scheduled | activity-failed | activity-pended | activity-continued | activity-resumed | branch-completed | join-waiting | instance-completed");

            migrationBuilder.AlterColumn<DefinitionDocument>(
                name: "Document",
                schema: "workflow",
                table: "workflow_definitions",
                type: "jsonb",
                nullable: false,
                comment: "The definition graph: {startStepId, steps: [{id, kind: Human|Automatic|SubWorkflow, role?, formRef?, branching: Exclusive|Parallel, join: None|All, activity?: {type, settings?, resultKey?}, subWorkflow?: {key, resultKey?}, onFailure?: {maxRetries, onExhausted: Pend|Continue}}], transitions: [{from, to, condition?: {field, op, value}}]}",
                oldClrType: typeof(DefinitionDocument),
                oldType: "jsonb",
                oldComment: "The definition graph: {startStepId, steps: [{id, kind: Human|Automatic, role?, formRef?, branching: Exclusive|Parallel, join: None|All, activity?: {ref, input}, onFailure?: {maxRetries, onExhausted: Pend|Continue}}], transitions: [{from, to, condition?: {field, op, value}}]}");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AlterColumn<string>(
                name: "Action",
                schema: "workflow",
                table: "workflow_history",
                type: "character varying(50)",
                maxLength: 50,
                nullable: false,
                comment: "instance-started | task-created | task-claimed | task-completed | task-reassigned | note-added | activity-scheduled | activity-failed | activity-pended | activity-continued | activity-resumed | branch-completed | join-waiting | instance-completed",
                oldClrType: typeof(string),
                oldType: "character varying(50)",
                oldMaxLength: 50,
                oldComment: "instance-started | task-created | task-claimed | task-completed | task-reassigned | note-added | activity-scheduled | activity-failed | activity-pended | activity-continued | activity-completed | activity-resumed | branch-completed | join-waiting | child-scheduled | child-started | child-failed | child-completed | instance-completed");

            migrationBuilder.AlterColumn<DefinitionDocument>(
                name: "Document",
                schema: "workflow",
                table: "workflow_definitions",
                type: "jsonb",
                nullable: false,
                comment: "The definition graph: {startStepId, steps: [{id, kind: Human|Automatic, role?, formRef?, branching: Exclusive|Parallel, join: None|All, activity?: {ref, input}, onFailure?: {maxRetries, onExhausted: Pend|Continue}}], transitions: [{from, to, condition?: {field, op, value}}]}",
                oldClrType: typeof(DefinitionDocument),
                oldType: "jsonb",
                oldComment: "The definition graph: {startStepId, steps: [{id, kind: Human|Automatic|SubWorkflow, role?, formRef?, branching: Exclusive|Parallel, join: None|All, activity?: {type, settings?, resultKey?}, subWorkflow?: {key, resultKey?}, onFailure?: {maxRetries, onExhausted: Pend|Continue}}], transitions: [{from, to, condition?: {field, op, value}}]}");
        }
    }
}
