using YG.Modules.Workflow.Domain.Definition;

namespace YG.Modules.Workflow.Domain;

public enum WorkflowDefinitionStatus { Draft, PendingApproval, Published }

public sealed class WorkflowDefinition
{
    public Guid Id { get; set; } = Guid.NewGuid();

    /// <summary>Stable identity across versions, e.g. "order-approval".</summary>
    public string Key { get; set; } = default!;
    public int Version { get; set; }

    public string Name { get; set; } = default!;
    public WorkflowDefinitionStatus Status { get; set; } = WorkflowDefinitionStatus.Draft;

    /// <summary>The designed graph (steps, transitions, activities, forms) → jsonb.</summary>
    public DefinitionDocument Document { get; set; } = new();

    public string CreatedBy { get; set; } = default!;   // sub — reference-by-ID, never a FK
    public DateTimeOffset CreatedAt { get; set; } = DateTimeOffset.UtcNow;
    public DateTimeOffset? SubmittedAt { get; set; }
    public DateTimeOffset? PublishedAt { get; set; }
    public string? PublishedBy { get; set; }

    /// <summary>Reviewer's note when a submission is sent back to draft.</summary>
    public string? ReviewNote { get; set; }
}