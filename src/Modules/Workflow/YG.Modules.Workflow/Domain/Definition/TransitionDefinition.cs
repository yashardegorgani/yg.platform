namespace YG.Modules.Workflow.Domain.Definition;

public sealed class TransitionDefinition
{
    public string From { get; set; } = default!;
    public string To { get; set; } = default!;
    public ConditionDefinition? Condition { get; set; }  // null = unconditional
}