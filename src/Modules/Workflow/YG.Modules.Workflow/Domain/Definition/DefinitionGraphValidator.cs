namespace YG.Modules.Workflow.Domain.Definition;

public static class DefinitionGraphValidator
{
    private static readonly HashSet<string> KnownOps = ["eq", "ne", "gt", "lt", "gte", "lte"];

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

            if (step.Kind == StepKind.SubWorkflow && string.IsNullOrWhiteSpace(step.SubWorkflow?.Key))
                errors.Add($"Sub-workflow step '{step.Id}' needs a subWorkflow.key.");
        }

        if (doc.Steps.Count == 0)
            errors.Add("A workflow needs at least one step.");

        if (string.IsNullOrWhiteSpace(doc.StartStepId))
            errors.Add("startStepId is required.");
        else if (!stepIds.Contains(doc.StartStepId))
            errors.Add($"startStepId '{doc.StartStepId}' is not a known step.");

        // Referential integrity: transitions may only connect steps that exist.
        foreach (var t in doc.Transitions)
        {
            if (!stepIds.Contains(t.From))
                errors.Add($"Transition references unknown step '{t.From}'.");
            if (!stepIds.Contains(t.To))
                errors.Add($"Transition references unknown step '{t.To}'.");
        }

        // --- Routing rules ---

        // Conditions must be well-formed. The runtime treats nonsense as "false";
        // publish treats nonsense as a design error. Both are correct.
        foreach (var t in doc.Transitions.Where(t => t.Condition is not null))
        {
            var c = t.Condition!;
            if (string.IsNullOrWhiteSpace(c.Field))
                errors.Add($"Transition {t.From} -> {t.To}: condition needs a field path.");
            if (!KnownOps.Contains(c.Op))
                errors.Add($"Transition {t.From} -> {t.To}: unknown operator '{c.Op}'. Known: eq, ne, gt, lt, gte, lte.");
            if (c.Value is null)
                errors.Add($"Transition {t.From} -> {t.To}: condition needs a value to compare against.");
        }

        // Branching sanity per step: document order is semantics, unconditional = else.
        // These rules are EXCLUSIVE-mode semantics — parallel steps have their own rules below.
        foreach (var group in doc.Transitions.GroupBy(t => t.From))
        {
            var fromStep = doc.Steps.FirstOrDefault(s => s.Id == group.Key);
            if (fromStep is not null && fromStep.Branching == BranchingMode.Parallel)
                continue;

            var outgoing = group.ToList();
            var elseIndex = outgoing.FindIndex(t => t.Condition is null);

            // Dead rules: anything after the unconditional transition can never fire.
            if (elseIndex >= 0 && elseIndex < outgoing.Count - 1)
                errors.Add($"Step '{group.Key}': transitions after the unconditional one can never fire. Put the unconditional transition last.");

            // Mandatory else: conditional branching without a fallback can stall an instance.
            if (elseIndex < 0 && outgoing.Any(t => t.Condition is not null))
                errors.Add($"Step '{group.Key}': conditional branching needs an unconditional fallback transition.");
        }

        // --- Parallel routing sanity ---

        foreach (var step in doc.Steps.Where(s => s.Branching == BranchingMode.Parallel))
        {
            var outgoing = doc.Transitions.Where(t => t.From == step.Id).ToList();
            if (outgoing.Count < 2)
                errors.Add($"Step '{step.Id}': parallel branching needs at least two outgoing transitions.");
            if (outgoing.Any(t => t.Condition is not null))
                errors.Add($"Step '{step.Id}': parallel branches must be unconditional — conditions choose one path, parallel takes them all.");
        }

        foreach (var step in doc.Steps.Where(s => s.Join == JoinMode.All))
        {
            if (doc.Transitions.Count(t => t.To == step.Id) < 2)
                errors.Add($"Step '{step.Id}': join 'All' needs at least two incoming transitions.");
        }

        // Reachability: every step must be reachable from the start step.
        // (Skipped if the start step is invalid — that error is already reported above.)
        if (doc.StartStepId is not null && stepIds.Contains(doc.StartStepId))
        {
            var reachable = new HashSet<string> { doc.StartStepId };
            var frontier = new Queue<string>();
            frontier.Enqueue(doc.StartStepId);

            while (frontier.Count > 0)
            {
                var current = frontier.Dequeue();
                foreach (var t in doc.Transitions.Where(t => t.From == current && stepIds.Contains(t.To)))
                    if (reachable.Add(t.To)) frontier.Enqueue(t.To);
            }

            foreach (var s in doc.Steps.Where(s => !reachable.Contains(s.Id)))
                errors.Add($"Step '{s.Id}' is unreachable from the start step.");
        }

        return errors;
    }
}