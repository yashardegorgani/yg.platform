using System.Text.Json.Serialization;

namespace YG.Modules.Workflow.Domain.Definition;

[JsonConverter(typeof(JsonStringEnumConverter))]
public enum FailureAction { Pend, Continue }

public sealed class FailurePolicy
{
    public int MaxRetries { get; set; } = 3;
    public FailureAction OnExhausted { get; set; } = FailureAction.Pend;
}