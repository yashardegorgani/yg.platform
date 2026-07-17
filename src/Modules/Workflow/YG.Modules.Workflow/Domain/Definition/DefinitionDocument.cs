namespace YG.Modules.Workflow.Domain.Definition;

/// <summary>The whole designed graph. Stored as ONE jsonb document — versioning stays one-row cheap.</summary>
public sealed class DefinitionDocument
{
    public string? StartStepId { get; set; }
    public List<StepDefinition> Steps { get; set; } = [];
    public List<TransitionDefinition> Transitions { get; set; } = [];
}