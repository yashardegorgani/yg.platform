using FastEndpoints;
using Microsoft.EntityFrameworkCore;
using System.Text.Json;
using YG.BuildingBlocks.Auth;
using YG.Modules.Workflow.Domain.Runtime;
using YG.Modules.Workflow.Persistence;

namespace YG.Modules.Workflow.Features.Tasks;

/// <summary>Exactly one of ToSub / ToRole must be provided.</summary>
public sealed record ReassignTaskRequest(Guid Id, string? ToSub, string? ToRole);

public sealed class ReassignTaskEndpoint(WorkflowDbContext db, IUserContext user)
    : Endpoint<ReassignTaskRequest>
{
    public override void Configure()
    {
        Post("/workflow/tasks/{id}/reassign");
        Permissions("workflow.tasks.work");
    }

    public override async Task HandleAsync(ReassignTaskRequest req, CancellationToken ct)
    {
        if ((req.ToSub is null) == (req.ToRole is null))
        {
            AddError("Provide exactly one of toSub or toRole.");
            await Send.ErrorsAsync(400, ct);
            return;
        }

        var task = await db.Tasks.FindAsync([req.Id], ct);
        if (task is null)
        {
            await Send.NotFoundAsync(ct);
            return;
        }

        // Atomic hand-off: only the current claimer may give the task away, and
        // the WHERE clause makes the check-and-release one statement — the same
        // race family as claiming. Reassign REOPENS the task: the new addressee
        // claims it personally, so ClaimedBy never lies in the audit trail.
        var reassigned = req.ToSub is not null
            ? await db.Tasks
                .Where(t => t.Id == req.Id
                         && t.Status == WorkflowTaskStatus.Claimed
                         && t.ClaimedBy == user.Sub)
                .ExecuteUpdateAsync(s => s
                    .SetProperty(t => t.AssignedToSub, req.ToSub)
                    .SetProperty(t => t.Status, WorkflowTaskStatus.Open)
                    .SetProperty(t => t.ClaimedBy, (string?)null)
                    .SetProperty(t => t.ClaimedAt, (DateTimeOffset?)null), ct)
            : await db.Tasks
                .Where(t => t.Id == req.Id
                         && t.Status == WorkflowTaskStatus.Claimed
                         && t.ClaimedBy == user.Sub)
                .ExecuteUpdateAsync(s => s
                    .SetProperty(t => t.Role, req.ToRole!)
                    .SetProperty(t => t.AssignedToSub, (string?)null)
                    .SetProperty(t => t.Status, WorkflowTaskStatus.Open)
                    .SetProperty(t => t.ClaimedBy, (string?)null)
                    .SetProperty(t => t.ClaimedAt, (DateTimeOffset?)null), ct);

        if (reassigned == 0)
        {
            AddError("Task is not claimed by you.");
            await Send.ErrorsAsync(409, ct);
            return;
        }

        db.History.Add(new WorkflowHistoryEntry
        {
            InstanceId = task.InstanceId,
            StepId = task.StepId,
            Action = "task-reassigned",
            Actor = user.Sub,
            Data = JsonSerializer.SerializeToElement(new { toSub = req.ToSub, toRole = req.ToRole }),
        });
        await db.SaveChangesAsync(ct);

        await Send.NoContentAsync(ct);
    }
}