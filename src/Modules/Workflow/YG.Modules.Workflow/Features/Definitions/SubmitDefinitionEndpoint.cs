using FastEndpoints;
using YG.Modules.Workflow.Domain;
using YG.Modules.Workflow.Persistence;

namespace YG.Modules.Workflow.Features.Definitions;

public sealed record SubmitDefinitionRequest(Guid Id);

public sealed class SubmitDefinitionEndpoint(WorkflowDbContext db)
    : Endpoint<SubmitDefinitionRequest>
{
    public override void Configure()
    {
        Post("/workflow/definitions/{id}/submit");
        Permissions("workflow.definitions.design");   // designers submit...
    }

    public override async Task HandleAsync(SubmitDefinitionRequest req, CancellationToken ct)
    {
        var definition = await db.Definitions.FindAsync([req.Id], ct);
        if (definition is null)
        {
            await Send.NotFoundAsync(ct);
            return;
        }

        if (definition.Status != WorkflowDefinitionStatus.Draft)
        {
            AddError("Only drafts can be submitted for approval.");
            await Send.ErrorsAsync(409, ct);
            return;
        }

        definition.Status = WorkflowDefinitionStatus.PendingApproval;
        definition.SubmittedAt = DateTimeOffset.UtcNow;
        await db.SaveChangesAsync(ct);

        await Send.NoContentAsync(ct);
    }
}