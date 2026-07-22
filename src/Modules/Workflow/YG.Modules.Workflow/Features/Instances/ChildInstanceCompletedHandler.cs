using Microsoft.EntityFrameworkCore;
using System.Text.Json;
using YG.BuildingBlocks.Messaging;
using YG.Modules.Workflow.Domain.Runtime;
using YG.Modules.Workflow.Engine;
using YG.Modules.Workflow.Persistence;
using static YG.Modules.Workflow.Engine.IWorkOrder;

namespace YG.Modules.Workflow.Features.Instances;

public static class ChildInstanceCompletedHandler
{
    public static async Task Handle(ChildInstanceCompleted message, WorkflowDbContext db,
        IYGMessageBus bus, CancellationToken ct)
    {
        var parentStep = await db.StepInstances
            .Include(s => s.Instance)
            .FirstOrDefaultAsync(s => s.Id == message.ParentStepInstanceId, ct);

        if (parentStep is null || parentStep.Status != StepInstanceStatus.Active)
            return;   // replay shrug

        var parent = parentStep.Instance!;
        var child = await db.Instances.FirstAsync(i => i.Id == message.ChildInstanceId, ct);

        // Pinned version, as always.
        var definition = await db.Definitions.FirstAsync(
            d => d.Key == parent.DefinitionKey && d.Version == parent.DefinitionVersion, ct);
        var step = definition.Document.Steps.First(s => s.Id == parentStep.StepId);

        var now = DateTimeOffset.UtcNow;
        parentStep.Status = StepInstanceStatus.Completed;
        parentStep.CompletedAt = now;
        // CompletedBy stays null: the engine completes this step, not a person.

        // Merge home — REASSIGN, don't mutate (jsonb change-tracking rule).
        var newContext = new Dictionary<string, JsonElement>(parent.Context);
        if (step.SubWorkflow?.ResultKey is { } key)
            newContext[key] = JsonSerializer.SerializeToElement(child.Context);
        parent.Context = newContext;

        db.History.Add(new WorkflowHistoryEntry
        {
            InstanceId = parent.Id,
            StepId = parentStep.StepId,
            Action = "child-completed",
            Data = JsonSerializer.SerializeToElement(new { childInstanceId = child.Id }),
        });

        var (_, workOrders) = await WorkflowEngine.AdvanceAsync(db, parent, definition, parentStep, newContext, now, ct);
        foreach (var order in workOrders)
            await bus.PublishAsync(order);

        await db.SaveChangesAsync(ct);
    }
}