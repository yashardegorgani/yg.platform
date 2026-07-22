using FastEndpoints;
using Microsoft.EntityFrameworkCore;
using System.Text.Json;
using YG.BuildingBlocks.Auth;
using YG.BuildingBlocks.Messaging;
using YG.Modules.Workflow.Domain.Runtime;
using YG.Modules.Workflow.Engine;
using YG.Modules.Workflow.Persistence;

namespace YG.Modules.Workflow.Features.Tasks;

public sealed record CompleteTaskRequest(Guid Id, Dictionary<string, JsonElement>? Data);
public sealed record CompleteTaskResponse(string InstanceStatus, string? NextStepId);

public sealed class CompleteTaskEndpoint(WorkflowDbContext db, IUserContext user, IYGOutbox outbox)
    : Endpoint<CompleteTaskRequest, CompleteTaskResponse>
{
    public override void Configure()
    {
        Post("/workflow/tasks/{id}/complete");
        Permissions("workflow.tasks.work");
    }

    public override async Task HandleAsync(CompleteTaskRequest req, CancellationToken ct)
    {
        // Your navigation style: one PK lookup, tracked graph of three rows.
        var task = await db.Tasks
            .Include(t => t.Instance)
            .Include(t => t.StepInstance)
            .FirstOrDefaultAsync(t => t.Id == req.Id, ct);

        if (task is null)
        {
            await Send.NotFoundAsync(ct);
            return;
        }

        if (task.Status != WorkflowTaskStatus.Claimed)
        {
            AddError("Claim the task before completing it.");
            await Send.ErrorsAsync(409, ct);
            return;
        }

        if (task.ClaimedBy != user.Sub)
        {
            await Send.ForbiddenAsync(ct);   // ownership: only the claimer completes
            return;
        }

        var instance = task.Instance!;
        var stepInstance = task.StepInstance!;

        // The definition is a different aggregate — loaded by the PINNED version,
        // never "latest". This is version pinning paying rent.
        var definition = await db.Definitions.FirstAsync(
            d => d.Key == instance.DefinitionKey && d.Version == instance.DefinitionVersion, ct);

        var now = DateTimeOffset.UtcNow;
        var data = req.Data ?? new();

        // 1. Freeze the evidence on the step instance.
        stepInstance.CapturedData = data;
        stepInstance.Status = StepInstanceStatus.Completed;
        stepInstance.CompletedAt = now;
        stepInstance.CompletedBy = user.Sub;

        // 2. Close the task.
        task.Status = WorkflowTaskStatus.Completed;
        task.CompletedAt = now;

        // 3. Merge into instance context — REASSIGN, don't mutate: EF change
        //    tracking cannot see in-place edits inside a jsonb-mapped object.
        var newContext = new Dictionary<string, JsonElement>(instance.Context)
        {
            [task.StepId] = JsonSerializer.SerializeToElement(data),
        };
        instance.Context = newContext;

        // 4. Audit.
        db.History.Add(new WorkflowHistoryEntry
        {
            InstanceId = instance.Id,
            StepId = task.StepId,
            Action = "task-completed",
            Actor = user.Sub,
            ActorSnapshot = WorkflowHistoryEntry.SnapshotOf(user.Sub, user.Username, user.Roles),
            Data = JsonSerializer.SerializeToElement(data),
        });

        // 5. Advance: next human tasks, next automatic work orders, or the finish line.
        var (next, workOrders) = await WorkflowEngine.AdvanceAsync(db, instance, definition, stepInstance, newContext, now, ct);

        outbox.Enroll(db);
        foreach (var order in workOrders)
            await outbox.PublishAsync(order);

        await outbox.SaveChangesAndPublishAsync(ct);   // evidence + closure + advance: one atomic unit

        await Send.OkAsync(new(instance.Status.ToString(),
            next.Count == 0 ? null : string.Join(", ", next.Select(n => n.Id))), ct);
    }
}