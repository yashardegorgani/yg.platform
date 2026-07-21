using FastEndpoints;
using Microsoft.EntityFrameworkCore;
using YG.BuildingBlocks.Auth;
using YG.Modules.Workflow.Domain.Runtime;
using YG.Modules.Workflow.Persistence;

namespace YG.Modules.Workflow.Features.Tasks;

public sealed record TaskView(Guid Id, Guid InstanceId, string DefinitionKey, string StepId,
    string Role, string? FormRef, string Status, string? ClaimedBy, DateTimeOffset CreatedAt);

public sealed class MyTasksEndpoint(WorkflowDbContext db, IUserContext user)
    : EndpointWithoutRequest<List<TaskView>>
{
    public override void Configure()
    {
        Get("/workflow/tasks/my");
        Permissions("workflow.tasks.work");
    }

    public override async Task HandleAsync(CancellationToken ct)
    {
        // Slice-2 assignment: the role claims already in the token
        // (courtesy of RoleClaimsTransformer) ARE the assignment mechanism.
        var myRoles = user.Roles.ToList();

        var tasks = await db.Tasks
            .Where(t => (t.Status == WorkflowTaskStatus.Open &&
                            (t.AssignedToSub == null
                                ? myRoles.Contains(t.Role)      // role addressing (as before)
                                : t.AssignedToSub == user.Sub)) // person addressing wins
                     || (t.Status == WorkflowTaskStatus.Claimed && t.ClaimedBy == user.Sub))
            .OrderBy(t => t.CreatedAt)
            .Select(t => new TaskView(t.Id, t.InstanceId, t.DefinitionKey, t.StepId,
                t.Role, t.FormRef, t.Status.ToString(), t.ClaimedBy, t.CreatedAt))
            .ToListAsync(ct);

        await Send.OkAsync(tasks, ct);
    }
}