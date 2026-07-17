namespace YG.Modules.Workflow.Domain.Definition;

public static class DefinitionGraphValidator
{
    /// <summary>Structural integrity — the FK-substitute we owe the document design.</summary>
    public static List<string> Validate(DefinitionDocument doc)
    {
        var errors = new List<string>();
        var stepIds = new HashSet<string>();

        foreach (var step in doc.Steps)
        {
            if (string.IsNullOrWhiteSpace(step.Id))
                errors.Add("Every step needs an id.");
            else if (!stepIds.Add(step.Id))
                errors.Add($"Duplicate step id '{step.Id}'.");

            if (step.Kind == StepKind.Human && string.IsNullOrWhiteSpace(step.FormRef))
                errors.Add($"Human step '{step.Id}' needs a formRef.");
            if (step.Kind == StepKind.Automatic && step.Activity is null)
                errors.Add($"Automatic step '{step.Id}' needs an activity.");
        }

        if (doc.Steps.Count == 0)
            errors.Add("A workflow needs at least one step.");

        if (string.IsNullOrWhiteSpace(doc.StartStepId))
            errors.Add("startStepId is required.");
        else if (!stepIds.Contains(doc.StartStepId))
            errors.Add($"startStepId '{doc.StartStepId}' is not a known step.");

        foreach (var t in doc.Transitions)
        {
            if (!stepIds.Contains(t.From))
                errors.Add($"Transition references unknown step '{t.From}'.");
            if (!stepIds.Contains(t.To))
                errors.Add($"Transition references unknown step '{t.To}'.");
        }

        return errors;
    }
}