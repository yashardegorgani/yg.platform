using Microsoft.EntityFrameworkCore;
using System.Text.Json;
using YG.Modules.Workflow.Domain;
using YG.Modules.Workflow.Domain.Definition;
using YG.Modules.Workflow.Domain.Runtime;
using YG.Modules.Workflow.Persistence;
using static YG.Modules.Workflow.Engine.IWorkOrder;

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

        return errors;
    }

    /// <summary>
    /// Activates a step: creates the step instance (the process record), its human
    /// task (the work item), and the history row — all in the caller's unit of work.
    /// Nothing is saved here; the endpoint owns the transaction.
    /// </summary>
    public static IWorkOrder? ActivateStep(WorkflowDbContext db, WorkflowInstance instance,
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

        if (step.Kind == StepKind.SubWorkflow)
        {
            // No task, no activity — a whole child instance will do this step's work.
            db.History.Add(new WorkflowHistoryEntry
            {
                InstanceId = instance.Id,
                StepId = step.Id,
                Action = "child-scheduled",
                Data = JsonSerializer.SerializeToElement(new { definitionKey = step.SubWorkflow!.Key }),
            });

            return new StartChildInstance(instance.Id, stepInstance.Id, step.SubWorkflow.Key);
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
    /// branching, several for parallel) — except joins that are still waiting for
    /// other branches. A dead end only ends the BRANCH; the instance completes
    /// when no other step instance is still active or pended.
    /// </summary>
    public static async Task<(List<StepDefinition> Next, List<IWorkOrder> WorkOrders)> AdvanceAsync(
        WorkflowDbContext db, WorkflowInstance instance, WorkflowDefinition definition,
        WorkflowStepInstance completedStep, Dictionary<string, JsonElement> context,
        DateTimeOffset now, CancellationToken ct)
    {
        var candidates = NextSteps(definition.Document, completedStep.StepId, context);
        var activated = new List<StepDefinition>();
        var workOrders = new List<IWorkOrder>();

        foreach (var next in candidates)
        {
            if (next.Join == JoinMode.All
                && !await JoinSatisfiedAsync(db, instance, definition.Document, next, completedStep, ct))
            {
                db.History.Add(new WorkflowHistoryEntry
                {
                    InstanceId = instance.Id,
                    StepId = next.Id,
                    Action = "join-waiting",
                    Data = JsonSerializer.SerializeToElement(new { arrivedFrom = completedStep.StepId }),
                });
                continue;
            }

            if (ActivateStep(db, instance, definition.Key, next) is { } order)
                workOrders.Add(order);
            activated.Add(next);
        }

        if (activated.Count > 0)
            return (activated, workOrders);

        // Nothing activated. If a join is merely waiting, the branch pauses here —
        // the incomplete predecessor that made it wait IS the sibling work that
        // keeps the instance alive.
        if (candidates.Count > 0)
            return ([], workOrders);

        // True dead end for THIS branch. Any sibling still working? Then the instance lives on.
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

        // A child's completion is the parent's business — tell it on the same rail.
        if (instance.ParentInstanceId is { } parentId && instance.ParentStepInstanceId is { } parentStepId)
            workOrders.Add(new ChildInstanceCompleted(instance.Id, parentId, parentStepId));

        return ([], workOrders);
    }

    /// <summary>
    /// Transition selection, two-mode. Exclusive (default): document order,
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

    /// <summary>
    /// A join is satisfied when every predecessor step (every transition pointing
    /// at it) has a Completed step instance. The arrival that triggered this check
    /// counts as done — its Completed status is tracked but not yet saved.
    /// </summary>
    private static async Task<bool> JoinSatisfiedAsync(WorkflowDbContext db, WorkflowInstance instance,
        DefinitionDocument doc, StepDefinition join, WorkflowStepInstance completedStep, CancellationToken ct)
    {
        var predecessors = doc.Transitions.Where(t => t.To == join.Id).Select(t => t.From).ToHashSet();
        predecessors.Remove(completedStep.StepId);

        if (predecessors.Count == 0) return true;

        var completedCount = await db.StepInstances
            .Where(s => s.InstanceId == instance.Id
                && predecessors.Contains(s.StepId)
                && s.Status == StepInstanceStatus.Completed)
            .Select(s => s.StepId)
            .Distinct()
            .CountAsync(ct);

        return completedCount == predecessors.Count;
    }
}