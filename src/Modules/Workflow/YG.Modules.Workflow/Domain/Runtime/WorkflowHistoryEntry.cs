using System.Text.Json;

namespace YG.Modules.Workflow.Domain.Runtime;

/// <summary>Append-only. Rows are written, never updated, never deleted. This IS the audit trail.</summary>
public sealed class WorkflowHistoryEntry
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public Guid InstanceId { get; set; }
    public string? StepId { get; set; }
    public string Action { get; set; } = default!;  // instance-started | task-created | task-claimed | task-completed | instance-completed
    public string? Actor { get; set; }              // sub; null when the engine itself acts
    public JsonElement? Data { get; set; }          // snapshot of what happened
    public DateTimeOffset OccurredAt { get; set; } = DateTimeOffset.UtcNow;
}