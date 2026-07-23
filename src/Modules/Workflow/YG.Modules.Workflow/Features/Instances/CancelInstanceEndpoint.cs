using FastEndpoints;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;
using System.Text.Json;
using YG.Modules.Workflow.Domain.Runtime;
using YG.Modules.Workflow.Persistence;

namespace YG.Modules.Workflow.Features.Instances;

public sealed record CancelInstanceRequest(Guid Id, string? Reason);

public sealed class CancelInstanceEndpoint(WorkflowDbContext db)
    : Endpoint<CancelInstanceRequest>
{
    public override void Configure()
    {
        Post("/workflow/instances/{id}/cancel");
        Permissions("workflow.instances.cancel");
    }

    public override async Task HandleAsync(CancelInstanceRequest req, CancellationToken ct)
    {
        var instance = await db.Instances.FirstOrDefaultAsync(i => i.Id == req.Id, ct);
        if (instance is null) { await Send.NotFoundAsync(ct); return; }

        if (instance.Status != WorkflowInstanceStatus.Running)
        {
            AddError("Only running instances can be cancelled.");
            await Send.ErrorsAsync(409, ct);
            return;
        }

        var actor = User.FindFirstValue("sub") ?? "unknown";
        var now = DateTimeOffset.UtcNow;

        // Downward cascade: breadth-first through the children, one transaction.
        var queue = new Queue<WorkflowInstance>();
        queue.Enqueue(instance);
        while (queue.Count > 0)
        {
            var current = queue.Dequeue();
            await CancelOneAsync(current, actor, req.Reason, now, ct);

            var children = await db.Instances
                .Where(i => i.ParentInstanceId == current.Id
                         && i.Status == WorkflowInstanceStatus.Running)
                .ToListAsync(ct);
            foreach (var child in children) queue.Enqueue(child);
        }

        // Upward courtesy: our parent's SubWorkflow step now waits for a
        // completion that will never come. Pend it — a human decides.
        if (instance.ParentStepInstanceId is { } parentStepId)
        {
            var parentStep = await db.StepInstances.FirstOrDefaultAsync(
                s => s.Id == parentStepId && s.Status == StepInstanceStatus.Active, ct);
            if (parentStep is not null)
            {
                parentStep.Status = StepInstanceStatus.Pended;
                db.History.Add(new WorkflowHistoryEntry
                {
                    InstanceId = parentStep.InstanceId,
                    StepId = parentStep.StepId,
                    Action = "child-cancelled",
                    Actor = actor,
                    Data = JsonSerializer.SerializeToElement(
                        new { childInstanceId = instance.Id, reason = req.Reason }),
                });
            }
        }

        await db.SaveChangesAsync(ct);
        await Send.OkAsync(new { cancelled = true }, ct);
    }

    private async Task CancelOneAsync(WorkflowInstance instance, string actor,
        string? reason, DateTimeOffset now, CancellationToken ct)
    {
        instance.Status = WorkflowInstanceStatus.Cancelled;
        instance.CompletedAt = now;

        var steps = await db.StepInstances
            .Where(s => s.InstanceId == instance.Id &&
                (s.Status == StepInstanceStatus.Active || s.Status == StepInstanceStatus.Pended))
            .ToListAsync(ct);
        foreach (var s in steps) s.Status = StepInstanceStatus.Cancelled;

        var tasks = await db.Tasks
            .Where(t => t.InstanceId == instance.Id &&
                (t.Status == WorkflowTaskStatus.Open || t.Status == WorkflowTaskStatus.Claimed))
            .ToListAsync(ct);
        foreach (var t in tasks) t.Status = WorkflowTaskStatus.Cancelled;

        db.History.Add(new WorkflowHistoryEntry
        {
            InstanceId = instance.Id,
            Action = "instance-cancelled",
            Actor = actor,
            Data = JsonSerializer.SerializeToElement(new { reason }),
        });
    }
}