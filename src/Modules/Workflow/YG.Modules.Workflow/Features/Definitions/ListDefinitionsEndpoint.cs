using FastEndpoints;
using Microsoft.EntityFrameworkCore;
using YG.Modules.Workflow.Persistence;

namespace YG.Modules.Workflow.Features.Definitions;

public sealed record DefinitionSummary(Guid Id, string Key, int Version, string Name,
    string Status, DateTimeOffset CreatedAt, DateTimeOffset? PublishedAt);

public sealed class ListDefinitionsEndpoint(WorkflowDbContext db)
    : EndpointWithoutRequest<List<DefinitionSummary>>
{
    public override void Configure()
    {
        Get("/workflow/definitions");
        Permissions("workflow.definitions.design", "workflow.definitions.publish");  // any-of
    }

    public override async Task HandleAsync(CancellationToken ct)
    {
        var definitions = await db.Definitions
            .OrderBy(d => d.Key).ThenByDescending(d => d.Version)
            .Select(d => new DefinitionSummary(d.Id, d.Key, d.Version, d.Name,
                d.Status.ToString(), d.CreatedAt, d.PublishedAt))
            .ToListAsync(ct);

        await Send.OkAsync(definitions, ct);
    }
}