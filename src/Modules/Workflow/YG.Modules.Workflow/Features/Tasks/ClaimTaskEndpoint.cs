using FastEndpoints;
using Microsoft.EntityFrameworkCore;
using YG.BuildingBlocks.Auth;
using YG.Modules.Workflow.Domain.Runtime;
using YG.Modules.Workflow.Persistence;

namespace YG.Modules.Workflow.Features.Tasks;

public sealed record ClaimTaskRequest(Guid Id);

public sealed class ClaimTaskEndpoint(WorkflowDbContext db, IUserContext user)
    : Endpoint<ClaimTaskRequest>
{
    public override void Configure()
    {
        Post("/workflow/tasks/{id}/claim");
        Permissions("workflow.tasks.work");
    }

    public override async Task HandleAsync(ClaimTaskRequest req, CancellationToken ct)
    {
        var task = await db.Tasks.FindAsync([req.Id], ct);
        if (task is null)
        {
            await Send.NotFoundAsync(ct);
            return;
        }

        // Person-pin overrides role: a task referred to YOU is yours even if the
        // step's role isn't in your token. Unpinned tasks keep the role rule.
        var mayClaim = task.AssignedToSub is null
            ? user.HasRole(task.Role)
            : task.AssignedToSub == user.Sub;

        if (!mayClaim)
        {
            await Send.ForbiddenAsync(ct);   // right permission, wrong addressee -> not your work
            return;
        }

        // Atomic conditional UPDATE — the same race guard as Inventory's stock
        // reservation: if two role holders click Claim simultaneously, the WHERE
        // clause guarantees exactly one UPDATE finds an Open row. One winner,
        // no locks, no retries.
        var claimed = await db.Tasks
            .Where(t => t.Id == req.Id && t.Status == WorkflowTaskStatus.Open)
            .ExecuteUpdateAsync(s => s
                .SetProperty(t => t.Status, WorkflowTaskStatus.Claimed)
                .SetProperty(t => t.ClaimedBy, user.Sub)
                .SetProperty(t => t.ClaimedAt, DateTimeOffset.UtcNow), ct);

        if (claimed == 0)
        {
            AddError("Task is no longer open.");
            await Send.ErrorsAsync(409, ct);
            return;
        }

        db.History.Add(new WorkflowHistoryEntry
        {
            InstanceId = task.InstanceId,
            StepId = task.StepId,
            Action = "task-claimed",
            Actor = user.Sub,
            ActorSnapshot = WorkflowHistoryEntry.SnapshotOf(user.Sub, user.Username, user.Roles),
        });
        await db.SaveChangesAsync(ct);

        await Send.NoContentAsync(ct);
    }
}