using System.Text.Json;
using YG.Modules.Workflow.Domain.Definition;
using YG.Modules.Workflow.Domain.Runtime;
using YG.Modules.Workflow.Persistence;

namespace YG.Modules.Workflow.Domain.Engine;

internal static class WorkflowEngine
{
    /// <summary>
    /// The capability guard: the engine refuses to START what it cannot yet execute.
    /// Publish validates structure; start validates executability against the
    /// engine's CURRENT capabilities. These are different questions.
    /// </summary>
    public static List<string> CheckExecutable(DefinitionDocument doc)
    {
        var errors = new List<string>();

        foreach (var s in doc.Steps.Where(s => s.Kind != StepKind.Human))
            errors.Add($"Step '{s.Id}': automatic steps arrive in slice 6.");

        foreach (var s in doc.Steps.Where(s => s.Kind == StepKind.Human && string.IsNullOrWhiteSpace(s.Role)))
            errors.Add($"Step '{s.Id}': human steps need a role for task assignment.");

        foreach (var t in doc.Transitions.Where(t => t.Condition is not null))
            errors.Add($"Transition {t.From} -> {t.To}: conditions arrive in slice 3.");

        foreach (var g in doc.Transitions.GroupBy(t => t.From).Where(g => g.Count() > 1))
            errors.Add($"Step '{g.Key}' has multiple outgoing transitions: branching arrives in slice 3.");

        return errors;
    }

    /// <summary>
    /// Activates a step: creates the step instance (the process record), its human
    /// task (the work item), and the history row — all in the caller's unit of work.
    /// Nothing is saved here; the endpoint owns the transaction.
    /// </summary>
    public static void ActivateStep(WorkflowDbContext db, WorkflowInstance instance,
        string definitionKey, StepDefinition step)
    {
        var stepInstance = new WorkflowStepInstance
        {
            InstanceId = instance.Id,
            StepId = step.Id,
        };
        db.StepInstances.Add(stepInstance);

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
    }

    /// <summary>
    /// Slice-2 routing: at most one unconditional transition per step.
    /// Null means the instance is done. Slice 3 replaces the first line
    /// with the condition evaluator — nothing else will move.
    /// </summary>
    public static StepDefinition? NextStep(DefinitionDocument doc, string fromStepId)
    {
        var transition = doc.Transitions.FirstOrDefault(t => t.From == fromStepId);
        return transition is null ? null : doc.Steps.First(s => s.Id == transition.To);
    }
}