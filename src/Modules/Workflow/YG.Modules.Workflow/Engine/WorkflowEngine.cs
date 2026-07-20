using System.Text.Json;
using YG.Modules.Workflow.Domain;
using YG.Modules.Workflow.Domain.Definition;
using YG.Modules.Workflow.Domain.Runtime;
using YG.Modules.Workflow.Persistence;

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
    /// The one advancement rule: pick the next step and activate it, or finish
    /// the instance. Returns what the caller must handle: the next step (for
    /// responses) and a work order to publish, when the next step is automatic.
    /// </summary>
    public static (StepDefinition? Next, ExecuteActivityStep? WorkOrder) Advance(
        WorkflowDbContext db, WorkflowInstance instance, WorkflowDefinition definition,
        string fromStepId, Dictionary<string, JsonElement> context, DateTimeOffset now)
    {
        var next = NextStep(definition.Document, fromStepId, context);
        if (next is not null)
            return (next, ActivateStep(db, instance, definition.Key, next));

        instance.Status = WorkflowInstanceStatus.Completed;
        instance.CompletedAt = now;
        db.History.Add(new WorkflowHistoryEntry
        {
            InstanceId = instance.Id,
            Action = "instance-completed",
        });
        return (null, null);
    }

    /// <summary>
    /// Transition selection: outgoing transitions are evaluated in DOCUMENT ORDER;
    /// the first satisfied one wins. An unconditional transition always matches —
    /// place it last as the "else" branch. Null means no outgoing transitions:
    /// the instance is done. ("No match" can't happen for published definitions —
    /// the validator requires an unconditional fallback wherever conditions branch.)
    /// </summary>
    public static StepDefinition? NextStep(DefinitionDocument doc, string fromStepId,
        Dictionary<string, JsonElement> context)
    {
        var transition = doc.Transitions
            .Where(t => t.From == fromStepId)
            .FirstOrDefault(t => t.Condition is null
                              || ConditionEvaluator.Evaluate(t.Condition, context));

        return transition is null ? null : doc.Steps.First(s => s.Id == transition.To);
    }
}