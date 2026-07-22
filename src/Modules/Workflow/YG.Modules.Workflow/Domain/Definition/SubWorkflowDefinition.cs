namespace YG.Modules.Workflow.Domain.Definition;

public sealed class SubWorkflowDefinition
{
    public string Key { get; set; } = default!;   // child definition key — resolved to latest Published at spawn time
    public string? ResultKey { get; set; }        // where the child's context lands in the parent's context
}