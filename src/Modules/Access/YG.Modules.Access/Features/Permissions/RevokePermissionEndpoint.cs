using FastEndpoints;
using Microsoft.EntityFrameworkCore;
using YG.Modules.Access.Persistence;

namespace YG.Modules.Access.Features.Permissions;

public sealed class RevokePermissionEndpoint(AccessDbContext db) : EndpointWithoutRequest
{
    public override void Configure()
    {
        Delete("/access/roles/{roleName}/permissions/{permission}");
        Permissions("access.roles.manage");
    }

    public override async Task HandleAsync(CancellationToken ct)
    {
        var roleName = Route<string>("roleName")!;
        var permission = Route<string>("permission")!;

        await db.RolePermissions
            .Where(p => p.Permission == permission
                && db.Roles.Any(r => r.Id == p.RoleId && r.Name == roleName))
            .ExecuteDeleteAsync(ct);

        await Send.NoContentAsync(ct);
    }
}