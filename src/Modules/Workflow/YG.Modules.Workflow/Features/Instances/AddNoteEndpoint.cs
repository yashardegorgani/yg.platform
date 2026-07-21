using FastEndpoints;
using Microsoft.EntityFrameworkCore;
using System.Text.Json;
using YG.BuildingBlocks.Auth;
using YG.Modules.Workflow.Domain.Runtime;
using YG.Modules.Workflow.Persistence;

namespace YG.Modules.Workflow.Features.Instances;

public sealed record AddNoteRequest(Guid Id, string Text, string? StepId);

public sealed class AddNoteEndpoint(WorkflowDbContext db, IUserContext user)
    : Endpoint<AddNoteRequest>
{
    public override void Configure()
    {
        Post("/workflow/instances/{id}/notes");
        Permissions("workflow.instances.read");   // may see it -> may annotate it
    }

    public override async Task HandleAsync(AddNoteRequest req, CancellationToken ct)
    {
        if (string.IsNullOrWhiteSpace(req.Text) || req.Text.Length > 2000)
        {
            AddError("Note text is required, max 2000 characters.");
            await Send.ErrorsAsync(400, ct);
            return;
        }

        var exists = await db.Instances.AnyAsync(i => i.Id == req.Id, ct);
        if (!exists)
        {
            await Send.NotFoundAsync(ct);
            return;
        }

        // A note is not an event — nothing advances, nothing changes state.
        // It's a human remark pinned to the timeline, immutable like the rest of it.
        db.History.Add(new WorkflowHistoryEntry
        {
            InstanceId = req.Id,
            StepId = req.StepId,
            Action = "note-added",
            Actor = user.Sub,
            ActorSnapshot = WorkflowHistoryEntry.SnapshotOf(user.Sub, user.Username, user.Roles),
            Data = JsonSerializer.SerializeToElement(new { text = req.Text.Trim() }),
        });
        await db.SaveChangesAsync(ct);

        await Send.NoContentAsync(ct);
    }
}