using FastEndpoints;
using FluentValidation;
using YG.Modules.Workflow.Domain;
using YG.Modules.Workflow.Persistence;

namespace YG.Modules.Workflow.Features.Definitions;

public sealed record RejectDefinitionRequest(Guid Id, string Note);

public sealed class RejectDefinitionValidator : Validator<RejectDefinitionRequest>
{
    public RejectDefinitionValidator()
    {
        // A rejection without a reason is useless to the designer — and to the audit trail.
        RuleFor(x => x.Note).NotEmpty().MaximumLength(2000);
    }
}

public sealed class RejectDefinitionEndpoint(WorkflowDbContext db)
    : Endpoint<RejectDefinitionRequest>
{
    public override void Configure()
    {
        Post("/workflow/definitions/{id}/reject");
        Permissions("workflow.definitions.publish");  // ...reviewers reject (or publish)
    }

    public override async Task HandleAsync(RejectDefinitionRequest req, CancellationToken ct)
    {
        var definition = await db.Definitions.FindAsync([req.Id], ct);
        if (definition is null)
        {
            await Send.NotFoundAsync(ct);
            return;
        }

        if (definition.Status != WorkflowDefinitionStatus.PendingApproval)
        {
            AddError("Only submitted definitions can be rejected.");
            await Send.ErrorsAsync(409, ct);
            return;
        }

        definition.Status = WorkflowDefinitionStatus.Draft;
        definition.ReviewNote = req.Note;
        await db.SaveChangesAsync(ct);

        await Send.NoContentAsync(ct);
    }
}