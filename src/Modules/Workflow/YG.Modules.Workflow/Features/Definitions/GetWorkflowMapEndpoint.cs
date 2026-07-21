using FastEndpoints;
using Microsoft.EntityFrameworkCore;
using YG.Modules.Workflow.Domain;
using YG.Modules.Workflow.Persistence;

namespace YG.Modules.Workflow.Features.Definitions;

public sealed record WorkflowMapStep(string Id, string Kind, string? Role, string? FormRef);

public sealed record WorkflowMapResponse(string StartStepId, List<WorkflowMapStep> Steps);

public sealed class GetWorkflowMapEndpoint(WorkflowDbContext db)
    : EndpointWithoutRequest<WorkflowMapResponse>
{
    public override void Configure()
    {
        Get("/workflow/definitions/{key}/map");
        Permissions("workflow.tasks.work");
    }

    public override async Task HandleAsync(CancellationToken ct)
    {
        var key = Route<string>("key")!;

        var def = await db.Definitions
            .Where(d => d.Key == key && d.Status == WorkflowDefinitionStatus.Published)
            .OrderByDescending(d => d.Version)
            .FirstOrDefaultAsync(ct);

        if (def is null)
        {
            await Send.NotFoundAsync(ct);
            return;
        }

        var steps = def.Document.Steps
            .Select(s => new WorkflowMapStep(s.Id, s.Kind.ToString(), s.Role, s.FormRef))
            .ToList();

        await Send.OkAsync(new WorkflowMapResponse(def.Document.StartStepId!, steps), ct);
    }
}