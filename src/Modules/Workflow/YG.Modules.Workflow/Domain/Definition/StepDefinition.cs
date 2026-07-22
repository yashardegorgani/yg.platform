using System.Text.Json;
using System.Text.Json.Serialization;

namespace YG.Modules.Workflow.Domain.Definition;

[JsonConverter(typeof(JsonStringEnumConverter))]
public enum StepKind { Human, Automatic, SubWorkflow }

[JsonConverter(typeof(JsonStringEnumConverter))]
public enum BranchingMode { Exclusive, Parallel }

[JsonConverter(typeof(JsonStringEnumConverter))]
public enum JoinMode { None, All }

public sealed class StepDefinition
{
    public string Id { get; set; } = default!;           // unique within the document, e.g. "fill-request"
    public StepKind Kind { get; set; }

    // Human steps — both opaque to the engine:
    public string? Role { get; set; }                    // resolved via the Access module
    public string? FormRef { get; set; }                 // resolved by the UI, never by the engine

    // Routing semantics — both default to exclusive, single-path behavior:
    public BranchingMode Branching { get; set; } = BranchingMode.Exclusive;  // outgoing: first match wins vs all fire
    public JoinMode Join { get; set; } = JoinMode.None;                      // incoming: activate per arrival vs wait for all

    // Automatic steps:
    public ActivityDefinition? Activity { get; set; }
    public FailurePolicy? OnFailure { get; set; }
    // Sub-workflow steps:
    public SubWorkflowDefinition? SubWorkflow { get; set; }
}