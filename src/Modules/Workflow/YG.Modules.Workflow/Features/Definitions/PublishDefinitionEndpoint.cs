using FastEndpoints;
using YG.BuildingBlocks.Auth;
using YG.Modules.Workflow.Domain;
using YG.Modules.Workflow.Domain.Definition;
using YG.Modules.Workflow.Persistence;

namespace YG.Modules.Workflow.Features.Definitions;

public sealed record PublishDefinitionRequest(Guid Id);

public sealed class PublishDefinitionEndpoint(WorkflowDbContext db, IUserContext user)
    : Endpoint<PublishDefinitionRequest>
{
    public override void Configure()
    {
        Post("/workflow/definitions/{id}/publish");
        Permissions("workflow.definitions.publish");
    }

    public override async Task HandleAsync(PublishDefinitionRequest req, CancellationToken ct)
    {
        var definition = await db.Definitions.FindAsync([req.Id], ct);
        if (definition is null)
        {
            await Send.NotFoundAsync(ct);
            return;
        }

        if (definition.Status != WorkflowDefinitionStatus.PendingApproval)
        {
            AddError("Only submitted definitions can be published.");
            await Send.ErrorsAsync(409, ct);
            return;
        }

        // The FK-substitute: Published definitions cannot lie.
        var errors = DefinitionGraphValidator.Validate(definition.Document);
        if (errors.Count > 0)
        {
            foreach (var error in errors) AddError(error);
            await Send.ErrorsAsync(400, ct);
            return;
        }

        definition.Status = WorkflowDefinitionStatus.Published;
        definition.PublishedAt = DateTimeOffset.UtcNow;
        definition.PublishedBy = user.Sub;
        definition.ReviewNote = null;
        await db.SaveChangesAsync(ct);

        await Send.NoContentAsync(ct);
    }
}