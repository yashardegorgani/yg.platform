using FastEndpoints;
using FluentValidation;
using Microsoft.EntityFrameworkCore;
using System.Text.Json;
using YG.BuildingBlocks.Auth;
using YG.BuildingBlocks.Messaging;
using YG.Modules.Workflow.Domain;
using YG.Modules.Workflow.Domain.Runtime;
using YG.Modules.Workflow.Engine;
using YG.Modules.Workflow.Persistence;

namespace YG.Modules.Workflow.Features.Instances;

public sealed record StartInstanceRequest(string DefinitionKey, string? BusinessKey);
public sealed record StartInstanceResponse(Guid InstanceId, string FirstStepId);

public sealed class StartInstanceValidator : Validator<StartInstanceRequest>
{
    public StartInstanceValidator()
    {
        RuleFor(x => x.DefinitionKey).NotEmpty().MaximumLength(100);
        RuleFor(x => x.BusinessKey).MaximumLength(200);
    }
}

public sealed class StartInstanceEndpoint(WorkflowDbContext db, IUserContext user, ActivityRegistry registry, IYGOutbox outbox)
    : Endpoint<StartInstanceRequest, StartInstanceResponse>
{
    public override void Configure()
    {
        Post("/workflow/instances");
        Permissions("workflow.instances.start");
    }

    public override async Task HandleAsync(StartInstanceRequest req, CancellationToken ct)
    {
        // New instances always take the LATEST published version...
        var definition = await db.Definitions
            .Where(d => d.Key == req.DefinitionKey && d.Status == WorkflowDefinitionStatus.Published)
            .OrderByDescending(d => d.Version)
            .FirstOrDefaultAsync(ct);

        if (definition is null)
        {
            await Send.NotFoundAsync(ct);
            return;
        }

        // The capability guard: published but not yet runnable by THIS engine -> honest 409.
        var errors = WorkflowEngine.CheckExecutable(definition.Document, registry);
        if (errors.Count > 0)
        {
            foreach (var error in errors) 
                AddError(error);
            await Send.ErrorsAsync(409, ct);
            return;
        }

        var startStep = definition.Document.Steps
            .First(s => s.Id == definition.Document.StartStepId);

        // ...and pin it forever. Publishing v2 tomorrow will never touch this instance.
        var instance = new WorkflowInstance
        {
            DefinitionKey = definition.Key,
            DefinitionVersion = definition.Version,
            BusinessKey = req.BusinessKey,
            StartedBy = user.Sub!,
        };
        db.Instances.Add(instance);

        db.History.Add(new WorkflowHistoryEntry
        {
            InstanceId = instance.Id,
            Action = "instance-started",
            Actor = user.Sub,
            Data = JsonSerializer.SerializeToElement(new
            {
                definitionVersion = definition.Version,
                businessKey = req.BusinessKey,
            }),
        });
        
        outbox.Enroll(db);

        var workOrder = WorkflowEngine.ActivateStep(db, instance, definition.Key, startStep);
        if (workOrder is not null)
            await outbox.PublishAsync(workOrder);

        await outbox.SaveChangesAndPublishAsync(ct);   // instance + history + step (+ work order): one atomic unit

        await Send.OkAsync(new(instance.Id, startStep.Id), ct);
    }
}