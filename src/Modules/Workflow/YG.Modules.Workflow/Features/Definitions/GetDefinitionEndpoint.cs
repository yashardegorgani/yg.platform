using FastEndpoints;
using Microsoft.EntityFrameworkCore;
using YG.Modules.Workflow.Domain.Definition;
using YG.Modules.Workflow.Persistence;

namespace YG.Modules.Workflow.Features.Definitions;

public sealed record GetDefinitionRequest(Guid Id);
public sealed record DefinitionDetail(Guid Id, string Key, int Version, string Name, string Status,
    DefinitionDocument Document, string CreatedBy, DateTimeOffset CreatedAt,
    DateTimeOffset? SubmittedAt, DateTimeOffset? PublishedAt, string? PublishedBy, string? ReviewNote);

public sealed class GetDefinitionEndpoint(WorkflowDbContext db)
    : Endpoint<GetDefinitionRequest, DefinitionDetail>
{
    public override void Configure()
    {
        Get("/workflow/definitions/{id}");
        Permissions("workflow.definitions.design", "workflow.definitions.publish");  // any-of
    }

    public override async Task HandleAsync(GetDefinitionRequest req, CancellationToken ct)
    {
        var d = await db.Definitions.FirstOrDefaultAsync(x => x.Id == req.Id, ct);
        
        if (d is null)
        {
            await Send.NotFoundAsync(ct);
            return;
        }

        await Send.OkAsync(new(d.Id, d.Key, d.Version, d.Name, d.Status.ToString(),
            d.Document, d.CreatedBy, d.CreatedAt,
            d.SubmittedAt, d.PublishedAt, d.PublishedBy, d.ReviewNote), ct);
    }
}