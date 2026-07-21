using FastEndpoints;
using Microsoft.EntityFrameworkCore;
using System.Text.Json;
using YG.Modules.Workflow.Persistence;

namespace YG.Modules.Workflow.Features.Instances;

public sealed record GetInstanceRequest(Guid Id);

public sealed record StepInstanceView(string StepId, string Status,
    Dictionary<string, JsonElement>? CapturedData, string? CompletedBy, DateTimeOffset? CompletedAt);

public sealed record HistoryView(string Action, string? StepId, string? Actor,
    JsonElement? ActorSnapshot, JsonElement? Data, DateTimeOffset OccurredAt);

public sealed record InstanceDetail(Guid Id, string DefinitionKey, int DefinitionVersion,
    string Status, string? BusinessKey, Dictionary<string, JsonElement> Context,
    DateTimeOffset StartedAt, DateTimeOffset? CompletedAt,
    List<StepInstanceView> Steps, List<HistoryView> History);

public sealed class GetInstanceEndpoint(WorkflowDbContext db)
    : Endpoint<GetInstanceRequest, InstanceDetail>
{
    public override void Configure()
    {
        Get("/workflow/instances/{id}");
        Permissions("workflow.instances.read");
    }

    public override async Task HandleAsync(GetInstanceRequest req, CancellationToken ct)
    {
        var instance = await db.Instances.FirstOrDefaultAsync(i => i.Id == req.Id, ct);
        if (instance is null)
        {
            await Send.NotFoundAsync(ct);
            return;
        }

        var steps = await db.StepInstances
            .Where(s => s.InstanceId == req.Id)
            .OrderBy(s => s.StartedAt)
            .Select(s => new StepInstanceView(s.StepId, s.Status.ToString(),
                s.CapturedData, s.CompletedBy, s.CompletedAt))
            .ToListAsync(ct);

        var history = await db.History
            .Where(h => h.InstanceId == req.Id)
            .OrderBy(h => h.OccurredAt)
            .Select(h => new HistoryView(h.Action, h.StepId, h.Actor, h.ActorSnapshot, h.Data, h.OccurredAt))
            .ToListAsync(ct);

        await Send.OkAsync(new(instance.Id, instance.DefinitionKey, instance.DefinitionVersion,
            instance.Status.ToString(), instance.BusinessKey, instance.Context,
            instance.StartedAt, instance.CompletedAt, steps, history), ct);
    }
}