using FastEndpoints;
using YG.Modules.Workflow.Engine;

namespace YG.Modules.Workflow.Features.Activities;

public sealed record ActivityCatalogEntry(string Type, string ImplementedBy);
public sealed record ListActivitiesResponse(IReadOnlyList<ActivityCatalogEntry> Activities);

public sealed class ListActivitiesEndpoint(ActivityRegistry registry)
    : EndpointWithoutRequest<ListActivitiesResponse>
{
    public override void Configure()
    {
        Get("/workflow/activities");
        Permissions("workflow.definitions.design", "workflow.definitions.publish");  // any-of: designers shop for skills
    }

    public override async Task HandleAsync(CancellationToken ct)
    {
        var entries = registry.All
            .Select(a => new ActivityCatalogEntry(a.Type, a.GetType().Assembly.GetName().Name!))
            .OrderBy(e => e.Type)
            .ToList();

        await Send.OkAsync(new(entries), ct);
    }
}