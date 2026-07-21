using FastEndpoints;
using Microsoft.EntityFrameworkCore;
using YG.Modules.Workflow.Domain.Runtime;
using YG.Modules.Workflow.Persistence;

namespace YG.Modules.Workflow.Features.Instances;

public sealed record PendedStepView(Guid StepInstanceId, Guid InstanceId,
    string DefinitionKey, string StepId, int Attempts, DateTimeOffset StartedAt);

public sealed class ListPendedStepsEndpoint(WorkflowDbContext db)
    : EndpointWithoutRequest<List<PendedStepView>>
{
    public override void Configure()
    {
        Get("/workflow/steps/pended");
        Permissions("workflow.tasks.work");
    }

    public override async Task HandleAsync(CancellationToken ct)
    {
        var pended = await db.StepInstances
            .Where(s => s.Status == StepInstanceStatus.Pended)
            .OrderBy(s => s.StartedAt)
            .Select(s => new PendedStepView(s.Id, s.InstanceId,
                s.Instance!.DefinitionKey, s.StepId, s.Attempts, s.StartedAt))
            .ToListAsync(ct);

        await Send.OkAsync(pended, ct);
    }
}