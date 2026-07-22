using System.Text.Json;

namespace YG.Modules.Workflow.Domain.Definition;

/// <summary>field/op/value over recorded instance state.</summary>
public sealed class ConditionDefinition
{
    public string Field { get; set; } = default!;   // path into instance context, e.g. "approve.decision"
    public string Op { get; set; } = default!;      // eq | ne | gt | lt | gte | lte
    public JsonElement? Value { get; set; }
}