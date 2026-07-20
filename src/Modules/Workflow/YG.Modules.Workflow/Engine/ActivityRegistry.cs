using YG.Modules.Workflow.Contracts;

namespace YG.Modules.Workflow.Engine;

/// <summary>
/// The engine's skill inventory: every IWorkflowActivity found in DI, keyed by Type.
/// Modules contribute by registering — the engine discovers, never lists.
/// </summary>
public sealed class ActivityRegistry
{
    private readonly Dictionary<string, IWorkflowActivity> _byType;

    public ActivityRegistry(IEnumerable<IWorkflowActivity> activities)
    {
        _byType = new Dictionary<string, IWorkflowActivity>(StringComparer.OrdinalIgnoreCase);
        foreach (var activity in activities)
        {
            if (!_byType.TryAdd(activity.Type, activity))
                throw new InvalidOperationException(
                    $"Duplicate workflow activity '{activity.Type}': " +
                    $"{_byType[activity.Type].GetType().FullName} vs {activity.GetType().FullName}.");
        }
    }

    public bool Contains(string type) => _byType.ContainsKey(type);
    public IWorkflowActivity? Find(string type) => _byType.GetValueOrDefault(type);
    public IReadOnlyCollection<IWorkflowActivity> All => _byType.Values;
}