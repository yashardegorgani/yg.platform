using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using System.Text.Json;
using YG.BuildingBlocks.Messaging;
using YG.Modules.Workflow.Contracts;
using YG.Modules.Workflow.Domain.Definition;
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
        // Note this also guards Pended steps — a replayed work order won't resurrect them.
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

        // Failure is data, not an exception. A throw here would roll back the
        // attempt counter and the history with it — so exceptions become results.
        ActivityResult result;
        try
        {
            result = await activity.ExecuteAsync(new ActivityContext(
                instance.Id, step.Id, step.Activity.Settings, instance.Context), ct);
        }
        catch (Exception ex)
        {
            result = ActivityResult.Failure($"{ex.GetType().Name}: {ex.Message}");
        }

        var now = DateTimeOffset.UtcNow;

        if (!result.Succeeded)
        {
            var policy = step.OnFailure ?? new FailurePolicy();   // defaults: 3 tries, then Pend
            stepInstance.Attempts++;

            if (stepInstance.Attempts < policy.MaxRetries)
            {
                db.History.Add(new WorkflowHistoryEntry
                {
                    InstanceId = instance.Id,
                    StepId = step.Id,
                    Action = "activity-failed",
                    Data = JsonSerializer.SerializeToElement(
                        new { attempt = stepInstance.Attempts, error = result.Error }),
                });

                await bus.PublishAsync(new ExecuteActivityStep(instance.Id, stepInstance.Id));
                await db.SaveChangesAsync(ct);   // counter + history + retry order commit together
                return;
            }

            if (policy.OnExhausted == FailureAction.Pend)
            {
                stepInstance.Status = StepInstanceStatus.Pended;
                db.History.Add(new WorkflowHistoryEntry
                {
                    InstanceId = instance.Id,
                    StepId = step.Id,
                    Action = "activity-pended",
                    Data = JsonSerializer.SerializeToElement(
                        new { attempts = stepInstance.Attempts, error = result.Error }),
                });
                await db.SaveChangesAsync(ct);

                logger.LogWarning("Step {StepId} on instance {InstanceId} pended after {Attempts} attempts: {Error}",
                    step.Id, instance.Id, stepInstance.Attempts, result.Error);
                return;   // instance stays Running — a human resumes it later
            }

            // Continue: give up on the output, advance anyway. Recorded, not hidden.
            db.History.Add(new WorkflowHistoryEntry
            {
                InstanceId = instance.Id,
                StepId = step.Id,
                Action = "activity-continued",
                Data = JsonSerializer.SerializeToElement(
                    new { attempts = stepInstance.Attempts, error = result.Error }),
            });
        }

        stepInstance.Status = StepInstanceStatus.Completed;
        stepInstance.CompletedAt = now;
        // CompletedBy stays null: null actor = the system itself. Same in history.

        // Merge the result — REASSIGN, don't mutate (jsonb change-tracking rule).
        // On Continue there is no output, so the context passes through unchanged.
        var newContext = new Dictionary<string, JsonElement>(instance.Context);
        if (result.Succeeded && step.Activity.ResultKey is { } key && result.Output is { } output)
            newContext[key] = output;
        instance.Context = newContext;

        if (result.Succeeded)
        {
            db.History.Add(new WorkflowHistoryEntry
            {
                InstanceId = instance.Id,
                StepId = step.Id,
                Action = "activity-completed",
                Data = JsonSerializer.SerializeToElement(
                    new { activity = activity.Type, resultKey = step.Activity.ResultKey }),
            });
        }

        var (_, workOrder) = WorkflowEngine.Advance(db, instance, definition, step.Id, newContext, now);
        if (workOrder is not null)
            await bus.PublishAsync(workOrder);   // automatic → automatic chains ride the same rail

        await db.SaveChangesAsync(ct);   // handler = Wolverine turf: auto-transactions commit save + publish together
    }
}