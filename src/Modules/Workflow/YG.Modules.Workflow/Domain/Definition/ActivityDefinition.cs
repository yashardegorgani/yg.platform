using System.Text.Json;

namespace YG.Modules.Workflow.Domain.Definition;

public sealed class ActivityDefinition
{
    public string Type { get; set; } = default!;    // "email", "rest-call", module-contributed names
    public JsonElement? Settings { get; set; }      // opaque to the engine; the activity interprets it
    public string? ResultKey { get; set; }          // where the result lands in instance context
}