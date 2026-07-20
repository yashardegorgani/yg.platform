using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using System.Text.Json;
using YG.BuildingBlocks.Messaging;
using YG.Modules.Workflow.Contracts;
using YG.Modules.Workflow.Domain.Runtime;
using YG.Modules.Workflow.Engine;
using YG.Modules.Workflow.Persistence;

namespace YG.Modules.Workflow.Features.Activities;

public static class ExecuteActivityStepHandler
{
    public static async Task Handle(ExecuteActivityStep message, WorkflowDbContext db,
        ActivityRegistry registry, IYGMessageBus bus, ILogger<WorkflowModule> logger,
        CancellationToken ct)
    {
        var stepInstance = await db.StepInstances
            .Include(s => s.Instance)
            .FirstOrDefaultAsync(s => s.Id == message.StepInstanceId, ct);

        // Replays and stale messages get a shrug, not an exception: idempotence first.
        if (stepInstance is null || stepInstance.Status != StepInstanceStatus.Active)
            return;

        var instance = stepInstance.Instance!;

        // Pinned version, as always.
        var definition = await db.Definitions.FirstAsync(
            d => d.Key == instance.DefinitionKey && d.Version == instance.DefinitionVersion, ct);

        var step = definition.Document.Steps.First(s => s.Id == stepInstance.StepId);

        var activity = registry.Find(step.Activity!.Type)
            ?? throw new InvalidOperationException(
                $"Activity '{step.Activity.Type}' vanished between scheduling and execution.");

        var result = await activity.ExecuteAsync(new ActivityContext(
            instance.Id, step.Id, step.Activity.Settings, instance.Context), ct);

        if (!result.Succeeded)
            throw new InvalidOperationException(
                $"Activity '{activity.Type}' failed on step '{step.Id}': {result.Error}");
        // ^ For now: rollback + slice ⑤ rails (backoff retries → dead-letter).
        //   Step 4 replaces this line with the step's own FailurePolicy.

        var now = DateTimeOffset.UtcNow;
        stepInstance.Status = StepInstanceStatus.Completed;
        stepInstance.CompletedAt = now;
        // CompletedBy stays null: null actor = the system itself. Same in history.

        // Merge the result — REASSIGN, don't mutate (jsonb change-tracking rule).
        var newContext = new Dictionary<string, JsonElement>(instance.Context);
        if (step.Activity.ResultKey is { } key && result.Output is { } output)
            newContext[key] = output;
        instance.Context = newContext;

        db.History.Add(new WorkflowHistoryEntry
        {
            InstanceId = instance.Id,
            StepId = step.Id,
            Action = "activity-completed",
            Data = JsonSerializer.SerializeToElement(
                new { activity = activity.Type, resultKey = step.Activity.ResultKey }),
        });

        var (_, workOrder) = WorkflowEngine.Advance(db, instance, definition, step.Id, newContext, now);
        if (workOrder is not null)
            await bus.PublishAsync(workOrder);   // automatic → automatic chains ride the same rail

        await db.SaveChangesAsync(ct);   // handler = Wolverine turf: auto-transactions commit save + publish together
    }
}