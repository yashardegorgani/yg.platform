using System.Text.Json;

namespace YG.Modules.Workflow.Domain.Runtime;

public enum WorkflowInstanceStatus { Running, Completed }

public sealed class WorkflowInstance
{
    public Guid Id { get; set; } = Guid.NewGuid();

    public string DefinitionKey { get; set; } = default!;
    public int DefinitionVersion { get; set; }              // pinned at start, forever

    public WorkflowInstanceStatus Status { get; set; } = WorkflowInstanceStatus.Running;

    /// <summary>Opaque reference to a business entity. Never a FK, never resolved by the engine.</summary>
    public string? BusinessKey { get; set; }

    /// <summary>Set when this instance was spawned by a SubWorkflow step. Both null = top-level instance.</summary>
    public Guid? ParentInstanceId { get; set; }
    public Guid? ParentStepInstanceId { get; set; }

    /// <summary>The accumulated state. Steps write it; transitions will only ever read it.</summary>
    public Dictionary<string, JsonElement> Context { get; set; } = new();

    public string StartedBy { get; set; } = default!;
    public DateTimeOffset StartedAt { get; set; } = DateTimeOffset.UtcNow;
    public DateTimeOffset? CompletedAt { get; set; }
}