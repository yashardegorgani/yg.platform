using FastEndpoints;
using Microsoft.EntityFrameworkCore;
using YG.Modules.Access.Persistence;

namespace YG.Modules.Access.Features.Grants;

public sealed class RevokeRoleEndpoint(AccessDbContext db) : EndpointWithoutRequest
{
    public override void Configure()
    {
        Delete("/access/users/{sub}/roles/{roleName}");
        Permissions("access.grants.manage");
    }

    public override async Task HandleAsync(CancellationToken ct)
    {
        var sub = Route<string>("sub")!;
        var roleName = Route<string>("roleName")!;

        await db.UserRoles
            .Where(u => u.Sub == sub && db.Roles.Any(r => r.Id == u.RoleId && r.Name == roleName))
            .ExecuteDeleteAsync(ct);

        await Send.NoContentAsync(ct);
    }
}
