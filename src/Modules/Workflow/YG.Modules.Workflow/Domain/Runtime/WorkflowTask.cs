namespace YG.Modules.Workflow.Domain.Runtime;

public enum WorkflowTaskStatus { Open, Claimed, Completed }

public sealed class WorkflowTask
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public Guid InstanceId { get; set; }
    public WorkflowInstance? Instance { get; set; }
    public Guid StepInstanceId { get; set; }
    public WorkflowStepInstance? StepInstance { get; set; }

    // Denormalized so the inbox renders without loading definitions:
    public string DefinitionKey { get; set; } = default!;
    public string StepId { get; set; } = default!;
    public string Role { get; set; } = default!;            // opaque string; slice 4 deepens this
    public string? FormRef { get; set; }                    // what the UI renders

    public WorkflowTaskStatus Status { get; set; } = WorkflowTaskStatus.Open;
    public string? ClaimedBy { get; set; }
    public DateTimeOffset? ClaimedAt { get; set; }
    public DateTimeOffset CreatedAt { get; set; } = DateTimeOffset.UtcNow;
    public DateTimeOffset? CompletedAt { get; set; }
}