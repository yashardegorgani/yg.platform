using System.Text.Json;

namespace YG.Modules.Workflow.Domain.Runtime;

public enum StepInstanceStatus { Active, Completed, Pended, Cancelled }

public sealed class WorkflowStepInstance
{
    public Guid Id { get; set; } = Guid.NewGuid();

    public Guid InstanceId { get; set; }
    public WorkflowInstance? Instance { get; set; }


    public string StepId { get; set; } = default!;          // id from the definition document

    public StepInstanceStatus Status { get; set; } = StepInstanceStatus.Active;
    
    /// <summary>How many times the activity has failed on this step. Fuel for FailurePolicy.MaxRetries.</summary>
    public int Attempts { get; set; }

    /// <summary>What was entered at this step — frozen on completion. This is what e-signatures will attest.</summary>
    public Dictionary<string, JsonElement>? CapturedData { get; set; }

    public DateTimeOffset StartedAt { get; set; } = DateTimeOffset.UtcNow;
    public DateTimeOffset? CompletedAt { get; set; }
    public string? CompletedBy { get; set; }
}