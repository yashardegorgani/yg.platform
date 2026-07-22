namespace YG.Modules.Workflow.Engine;

/// <summary>
/// A durable engine instruction. The engine decides and returns these;
/// endpoints and handlers publish them on their own transactional rails.
/// </summary>
public interface IWorkOrder
{

    /// <summary>Spawn a child instance for a waiting SubWorkflow step.</summary>
    public sealed record StartChildInstance(Guid ParentInstanceId, Guid ParentStepInstanceId, string DefinitionKey) : IWorkOrder;

    /// <summary>A child finished — its parent step may now complete and advance.</summary>
    public sealed record ChildInstanceCompleted(Guid ChildInstanceId, Guid ParentInstanceId, Guid ParentStepInstanceId) : IWorkOrder;
}
