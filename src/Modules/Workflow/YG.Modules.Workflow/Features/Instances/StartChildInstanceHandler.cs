using Microsoft.EntityFrameworkCore;
using System.Text.Json;
using YG.BuildingBlocks.Messaging;
using YG.Modules.Workflow.Domain;
using YG.Modules.Workflow.Domain.Runtime;
using YG.Modules.Workflow.Engine;
using YG.Modules.Workflow.Persistence;
using static YG.Modules.Workflow.Engine.IWorkOrder;

namespace YG.Modules.Workflow.Features.Instances;

public static class StartChildInstanceHandler
{
    public static async Task Handle(StartChildInstance message, WorkflowDbContext db,
        ActivityRegistry registry, IYGMessageBus bus, CancellationToken ct)
    {
        var parentStep = await db.StepInstances
            .Include(s => s.Instance)
            .FirstOrDefaultAsync(s => s.Id == message.ParentStepInstanceId, ct);

        // Replays get a shrug. Active is this step's normal WAITING state though,
        // so the real idempotence guard is: one child per parent step, ever.
        if (parentStep is null || parentStep.Status != StepInstanceStatus.Active)
            return;
        if (await db.Instances.AnyAsync(i => i.ParentStepInstanceId == parentStep.Id, ct))
            return;

        var parent = parentStep.Instance!;

        // Children resolve LATEST published — the same rule as humans starting instances.
        var definition = await db.Definitions
            .Where(d => d.Key == message.DefinitionKey && d.Status == WorkflowDefinitionStatus.Published)
            .OrderByDescending(d => d.Version)
            .FirstOrDefaultAsync(ct);

        var errors = definition is null
            ? [$"No published definition '{message.DefinitionKey}' to spawn."]
            : WorkflowEngine.CheckExecutable(definition.Document, registry);

        if (definition is null || errors.Count > 0)
        {
            // Spawn failure pends the parent step: rescueable, never silent.
            parentStep.Status = StepInstanceStatus.Pended;
            db.History.Add(new WorkflowHistoryEntry
            {
                InstanceId = parent.Id,
                StepId = parentStep.StepId,
                Action = "child-failed",
                Data = JsonSerializer.SerializeToElement(new { error = string.Join(" ", errors) }),
            });
            await db.SaveChangesAsync(ct);
            return;
        }

        var child = new WorkflowInstance
        {
            DefinitionKey = definition.Key,
            DefinitionVersion = definition.Version,
            ParentInstanceId = parent.Id,
            ParentStepInstanceId = parentStep.Id,
            StartedBy = parent.StartedBy,   // provenance: the human who set the ROOT in motion
        };
        db.Instances.Add(child);

        db.History.Add(new WorkflowHistoryEntry
        {
            InstanceId = child.Id,
            Action = "instance-started",
            // Actor stays null: the engine spawned this, not a human.
            Data = JsonSerializer.SerializeToElement(new
            {
                definitionVersion = definition.Version,
                parentInstanceId = parent.Id,
                parentStepId = parentStep.StepId,
            }),
        });

        db.History.Add(new WorkflowHistoryEntry
        {
            InstanceId = parent.Id,
            StepId = parentStep.StepId,
            Action = "child-started",
            Data = JsonSerializer.SerializeToElement(new
            {
                childInstanceId = child.Id,
                definitionKey = definition.Key,
                definitionVersion = definition.Version,
            }),
        });

        var startStep = definition.Document.Steps.First(s => s.Id == definition.Document.StartStepId);
        if (WorkflowEngine.ActivateStep(db, child, definition.Key, startStep) is { } order)
            await bus.PublishAsync(order);

        await db.SaveChangesAsync(ct);   // handler = Wolverine turf: spawn + publish commit together
    }
}