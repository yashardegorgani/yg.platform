namespace YG.Modules.Workflow.Engine;

/// <summary>
/// Internal work order: "execute this activated automatic step." Published and
/// handled inside the Workflow module only — not a contract, deliberately.
/// </summary>
public sealed record ExecuteActivityStep(Guid InstanceId, Guid StepInstanceId) : IWorkOrder;