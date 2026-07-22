using FastEndpoints;
using Microsoft.EntityFrameworkCore;
using YG.BuildingBlocks.Auth;
using YG.BuildingBlocks.Messaging;
using YG.Modules.Workflow.Domain.Definition;
using YG.Modules.Workflow.Domain.Runtime;
using YG.Modules.Workflow.Engine;
using YG.Modules.Workflow.Persistence;
using static YG.Modules.Workflow.Engine.IWorkOrder;

namespace YG.Modules.Workflow.Features.Instances;

public sealed record ResumeStepRequest(Guid Id);   // step instance id

public sealed class ResumeStepEndpoint(WorkflowDbContext db, IUserContext user, IYGOutbox outbox)
    : Endpoint<ResumeStepRequest>
{
    public override void Configure()
    {
        Post("/workflow/steps/{id}/resume");
        Permissions("workflow.tasks.work");
    }

    public override async Task HandleAsync(ResumeStepRequest req, CancellationToken ct)
    {
        var stepInstance = await db.StepInstances
            .Include(s => s.Instance)
            .FirstOrDefaultAsync(s => s.Id == req.Id, ct);

        if (stepInstance is null)
        {
            await Send.NotFoundAsync(ct);
            return;
        }

        if (stepInstance.Status != StepInstanceStatus.Pended)
        {
            AddError("Only pended steps can be resumed.");
            await Send.ErrorsAsync(409, ct);
            return;
        }

        var instance = stepInstance.Instance!;

        // Pinned version, as always.
        var definition = await db.Definitions.FirstAsync(
            d => d.Key == instance.DefinitionKey && d.Version == instance.DefinitionVersion, ct);

        var step = definition.Document.Steps.First(s => s.Id == stepInstance.StepId);

        // The step's own role wins: an automatic step may declare its supervisor
        // (Inventory's steps rescued by inventory people, not catalog people).
        // Steps that don't declare one fall back to the definition's human-step
        // roles — the people already working this workflow.
        var rescueRoles = step.Role is not null
            ? [step.Role]
            : definition.Document.Steps
                .Where(s => s.Kind == StepKind.Human && s.Role is not null)
                .Select(s => s.Role!)
                .ToList();

        if (!rescueRoles.Any(user.HasRole))
        {
            await Send.ForbiddenAsync(ct);
            return;
        }

        stepInstance.Status = StepInstanceStatus.Active;
        stepInstance.Attempts = 0;               // fresh patience

        db.History.Add(new WorkflowHistoryEntry
        {
            InstanceId = instance.Id,
            StepId = stepInstance.StepId,
            Action = "activity-resumed",
            Actor = user.Sub,
            ActorSnapshot = WorkflowHistoryEntry.SnapshotOf(user.Sub, user.Username, user.Roles),
        });

        // Endpoint turf -> outbox rails: the wake-up and the work order commit together.
        outbox.Enroll(db);
        IWorkOrder order = step.Kind == StepKind.SubWorkflow
            ? new StartChildInstance(instance.Id, stepInstance.Id, step.SubWorkflow!.Key)
            : new ExecuteActivityStep(instance.Id, stepInstance.Id);
        await outbox.PublishAsync(order);
        await outbox.SaveChangesAndPublishAsync(ct);

        await Send.NoContentAsync(ct);
    }
}