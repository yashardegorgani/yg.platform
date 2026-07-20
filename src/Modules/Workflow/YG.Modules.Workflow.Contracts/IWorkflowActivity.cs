using System.Diagnostics;

namespace YG.Modules.Workflow.Contracts;

/// <summary>
/// A capability the workflow engine can execute as an automatic step.
/// Implementations live in ANY module and are discovered via DI —
/// the engine never knows concrete types, only this contract.
/// </summary>
public interface IWorkflowActivity
{
    /// <summary>
    /// Unique capability name referenced by definitions, namespaced by owner:
    /// "core.log", "inventory.release-stock", "catalog.tag-product".
    /// </summary>
    string Type { get; }

    Task<ActivityResult> ExecuteAsync(ActivityContext context, CancellationToken ct);
}