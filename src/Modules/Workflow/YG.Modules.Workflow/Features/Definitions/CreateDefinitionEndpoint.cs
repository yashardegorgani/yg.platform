using FastEndpoints;
using FluentValidation;
using Microsoft.EntityFrameworkCore;
using YG.BuildingBlocks.Auth;
using YG.Modules.Workflow.Domain;
using YG.Modules.Workflow.Domain.Definition;
using YG.Modules.Workflow.Persistence;

namespace YG.Modules.Workflow.Features.Definitions;

public sealed record CreateDefinitionRequest(string Key, string Name, DefinitionDocument Document);
public sealed record CreateDefinitionResponse(Guid Id, string Key, int Version, string Status);

public sealed class CreateDefinitionValidator : Validator<CreateDefinitionRequest>
{
    public CreateDefinitionValidator()
    {
        RuleFor(x => x.Key).NotEmpty().MaximumLength(100)
            .Matches("^[a-z0-9-]+$")
            .WithMessage("Key must be kebab-case: lowercase letters, digits, hyphens.");
        RuleFor(x => x.Name).NotEmpty().MaximumLength(200);
        RuleFor(x => x.Document).NotNull();
        // No shape checks here: drafts may be structurally incomplete on purpose.
        // The graph validator guards the door at PUBLISH, not at create.
    }
}

public sealed class CreateDefinitionEndpoint(WorkflowDbContext db, IUserContext user)
    : Endpoint<CreateDefinitionRequest, CreateDefinitionResponse>
{
    public override void Configure()
    {
        Post("/workflow/definitions");
        Permissions("workflow.definitions.design");
    }

    public override async Task HandleAsync(CreateDefinitionRequest req, CancellationToken ct)
    {
        var existingVersions = await db.Definitions
            .Where(d => d.Key == req.Key)
            .Select(d => new { d.Version, d.Status })
            .ToListAsync(ct);

        // One work-in-progress version per key — no parallel drafts of the same workflow.
        if (existingVersions.Any(v => v.Status != WorkflowDefinitionStatus.Published))
        {
            AddError("An unpublished version of this key already exists. Edit or publish it first.");
            await Send.ErrorsAsync(409, ct);
            return;
        }

        var definition = new WorkflowDefinition
        {
            Key = req.Key,
            Version = existingVersions.Count == 0 ? 1 : existingVersions.Max(v => v.Version) + 1,
            Name = req.Name,
            Document = req.Document,          // typed model straight in — jsonb does the rest
            CreatedBy = user.Sub!,
        };

        db.Definitions.Add(definition);
        await db.SaveChangesAsync(ct);

        await Send.OkAsync(new(definition.Id, definition.Key, definition.Version,
            definition.Status.ToString()), ct);
    }
}