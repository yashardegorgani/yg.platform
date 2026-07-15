using FastEndpoints;
using Microsoft.EntityFrameworkCore;
using YG.Modules.Access.Persistence;

namespace YG.Modules.Access.Features.Permissions;

public sealed class ListRolePermissionsEndpoint(AccessDbContext db) : EndpointWithoutRequest<List<string>>
{
    public override void Configure()
    {
        Get("/access/roles/{roleName}/permissions");
        Permissions("access.roles.manage");
    }

    public override async Task HandleAsync(CancellationToken ct)
    {
        var roleName = Route<string>("roleName")!;

        var role = await db.Roles.AsNoTracking()
            .SingleOrDefaultAsync(r => r.Name == roleName, ct);
        if (role is null) 
        { 
            await Send.NotFoundAsync(ct); 
            return; 
        }

        await Send.OkAsync(await db.RolePermissions.AsNoTracking()
            .Where(p => p.RoleId == role.Id)
            .Select(p => p.Permission)
            .OrderBy(p => p)
            .ToListAsync(ct), ct);
    }
}