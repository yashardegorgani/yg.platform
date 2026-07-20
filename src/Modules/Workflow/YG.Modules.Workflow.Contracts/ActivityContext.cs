using System.Text.Json;

namespace YG.Modules.Workflow.Contracts;

/// <summary>
/// Everything an activity is allowed to see: its designer-provided settings
/// and a READ-ONLY view of the instance's accumulated data. Activities never
/// touch workflow persistence — results flow back via ActivityResult only.
/// </summary>
public sealed record ActivityContext(
    Guid InstanceId,
    string StepId,
    JsonElement? Settings,
    IReadOnlyDictionary<string, JsonElement> InstanceContext);