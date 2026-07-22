using System.Text.Json;
using YG.Modules.Workflow.Domain;
using YG.Modules.Workflow.Domain.Definition;
using YG.Modules.Workflow.Domain.Runtime;
using YG.Modules.Workflow.Persistence;
using Microsoft.EntityFrameworkCore;

namespace YG.Modules.Workflow.Engine;

internal static class WorkflowEngine
{
    /// <summary>
    /// The capability guard: the engine refuses to START what it cannot yet execute.
    /// Publish validates structure; start validates executability against the
    /// engine's CURRENT capabilities. These are different questions.
    /// </summary>
    public static List<string> CheckExecutable(DefinitionDocument doc, ActivityRegistry registry)
    {
        var errors = new List<string>();

        foreach (var s in doc.Steps.Where(s => s.Kind == StepKind.Automatic))
        {
            if (s.Activity is null)
                errors.Add($"Step '{s.Id}': automatic steps need an activity.");
            else if (!registry.Contains(s.Activity.Type))
                errors.Add($"Step '{s.Id}': no activity '{s.Activity.Type}' is installed on this engine.");
        }

        foreach (var s in doc.Steps.Where(s => s.Kind == StepKind.Human && string.IsNullOrWhiteSpace(s.Role)))
            errors.Add($"Step '{s.Id}': human steps need a role for task assignment.");

        // Slice 10 in progress: fan-out runs; join arrives next.
        if (doc.Steps.Any(s => s.Join == JoinMode.All))
            errors.Add("This engine build cannot execute join steps yet.");

        return errors;
    }

    /// <summary>
    /// Activates a step: creates the step instance (the process record), its human
    /// task (the work item), and the history row — all in the caller's unit of work.
    /// Nothing is saved here; the endpoint owns the transaction.
    /// </summary>
    public static ExecuteActivityStep? ActivateStep(WorkflowDbContext db, WorkflowInstance instance,
        string definitionKey, StepDefinition step)
    {
        var stepInstance = new WorkflowStepInstance
        {
            InstanceId = instance.Id,
            StepId = step.Id,
        };
        db.StepInstances.Add(stepInstance);

        if (step.Kind == StepKind.Human)
        {
            db.Tasks.Add(new WorkflowTask
            {
                InstanceId = instance.Id,
                StepInstanceId = stepInstance.Id,
                DefinitionKey = definitionKey,
                StepId = step.Id,
                Role = step.Role!,               // guaranteed by CheckExecutable
                FormRef = step.FormRef,
            });

            db.History.Add(new WorkflowHistoryEntry
            {
                InstanceId = instance.Id,
                StepId = step.Id,
                Action = "task-created",
                Data = JsonSerializer.SerializeToElement(new { role = step.Role, formRef = step.FormRef }),
            });

            return null;
        }

        // Automatic: no task, no human — a durable work order instead. The caller
        // publishes it through ITS transactional channel; the engine only decides.
        db.History.Add(new WorkflowHistoryEntry
        {
            InstanceId = instance.Id,
            StepId = step.Id,
            Action = "activity-scheduled",
            Data = JsonSerializer.SerializeToElement(new { activity = step.Activity!.Type }),
        });

        return new ExecuteActivityStep(instance.Id, stepInstance.Id);
    }

    /// <summary>
    /// The one advancement rule: activate every next step (one for exclusive
    /// branching, several for parallel), or settle what a dead end means. With
    /// parallelism, "no outgoing transition" only ends the BRANCH — the instance
    /// completes when no other step instance is still active or pended.
    /// </summary>
    public static async Task<(List<StepDefinition> Next, List<ExecuteActivityStep> WorkOrders)> AdvanceAsync(
        WorkflowDbContext db, WorkflowInstance instance, WorkflowDefinition definition,
        WorkflowStepInstance completedStep, Dictionary<string, JsonElement> context,
        DateTimeOffset now, CancellationToken ct)
    {
        var nextSteps = NextSteps(definition.Document, completedStep.StepId, context);
        var workOrders = new List<ExecuteActivityStep>();

        foreach (var next in nextSteps)
            if (ActivateStep(db, instance, definition.Key, next) is { } order)
                workOrders.Add(order);

        if (nextSteps.Count > 0)
            return (nextSteps, workOrders);

        // Dead end for THIS branch. Any sibling still working? Then the instance lives on.
        // completedStep is excluded by id — its Completed status is tracked but unsaved,
        // so the database would still report it as Active.
        var siblingWork = await db.StepInstances.AnyAsync(s =>
            s.InstanceId == instance.Id
            && s.Id != completedStep.Id
            && (s.Status == StepInstanceStatus.Active || s.Status == StepInstanceStatus.Pended), ct);

        if (siblingWork)
        {
            db.History.Add(new WorkflowHistoryEntry
            {
                InstanceId = instance.Id,
                StepId = completedStep.StepId,
                Action = "branch-completed",
            });
            return ([], workOrders);
        }

        instance.Status = WorkflowInstanceStatus.Completed;
        instance.CompletedAt = now;
        db.History.Add(new WorkflowHistoryEntry
        {
            InstanceId = instance.Id,
            Action = "instance-completed",
        });
        return ([], workOrders);
    }

    /// <summary>
    /// Transition selection, now two-mode. Exclusive (default): document order,
    /// first satisfied wins, unconditional = else. Parallel: ALL transitions fire —
    /// the validator already guaranteed they are unconditional.
    /// </summary>
    public static List<StepDefinition> NextSteps(DefinitionDocument doc, string fromStepId,
        Dictionary<string, JsonElement> context)
    {
        var from = doc.Steps.First(s => s.Id == fromStepId);
        var outgoing = doc.Transitions.Where(t => t.From == fromStepId).ToList();

        if (from.Branching == BranchingMode.Parallel)
            return outgoing.Select(t => doc.Steps.First(s => s.Id == t.To)).ToList();

        var match = outgoing.FirstOrDefault(t => t.Condition is null
                                              || ConditionEvaluator.Evaluate(t.Condition, context));

        return match is null ? [] : [doc.Steps.First(s => s.Id == match.To)];
    }
}