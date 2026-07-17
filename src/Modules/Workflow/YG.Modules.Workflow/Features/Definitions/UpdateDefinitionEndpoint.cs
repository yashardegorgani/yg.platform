using FastEndpoints;
using FluentValidation;
using YG.Modules.Workflow.Domain;
using YG.Modules.Workflow.Domain.Definition;
using YG.Modules.Workflow.Persistence;

namespace YG.Modules.Workflow.Features.Definitions;

public sealed record UpdateDefinitionRequest(Guid Id, string Name, DefinitionDocument Document);

public sealed class UpdateDefinitionValidator : Validator<UpdateDefinitionRequest>
{
    public UpdateDefinitionValidator()
    {
        RuleFor(x => x.Name).NotEmpty().MaximumLength(200);
        RuleFor(x => x.Document).NotNull();
    }
}

public sealed class UpdateDefinitionEndpoint(WorkflowDbContext db)
    : Endpoint<UpdateDefinitionRequest>
{
    public override void Configure()
    {
        Put("/workflow/definitions/{id}");
        Permissions("workflow.definitions.design");
    }

    public override async Task HandleAsync(UpdateDefinitionRequest req, CancellationToken ct)
    {
        var definition = await db.Definitions.FindAsync([req.Id], ct);
        if (definition is null)
        {
            await Send.NotFoundAsync(ct);
            return;
        }

        // The immutability rule: only drafts bend.
        if (definition.Status != WorkflowDefinitionStatus.Draft)
        {
            AddError("Only drafts can be edited. Create a new version instead.");
            await Send.ErrorsAsync(409, ct);
            return;
        }

        definition.Name = req.Name;
        definition.Document = req.Document;   // whole-document replace — designer saves the whole canvas
        await db.SaveChangesAsync(ct);

        await Send.NoContentAsync(ct);
    }
}