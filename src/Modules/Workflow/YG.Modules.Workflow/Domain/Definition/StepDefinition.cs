using System.Text.Json;
using System.Text.Json.Serialization;

namespace YG.Modules.Workflow.Domain.Definition;

[JsonConverter(typeof(JsonStringEnumConverter))]
public enum StepKind { Human, Automatic }

public sealed class StepDefinition
{
    public string Id { get; set; } = default!;           // unique within the document, e.g. "fill-request"
    public StepKind Kind { get; set; }

    // Human steps — both opaque to the engine:
    public string? Role { get; set; }                    // resolved via Access much later (slice 4)
    public string? FormRef { get; set; }                 // resolved by the UI, never by the engine

    // Automatic steps (behavior arrives in slice 6; the shape exists now):
    public ActivityDefinition? Activity { get; set; }
    public FailurePolicy? OnFailure { get; set; }
}