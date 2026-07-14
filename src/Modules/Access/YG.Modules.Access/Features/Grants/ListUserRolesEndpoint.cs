using FastEndpoints;
using Microsoft.EntityFrameworkCore;
using YG.Modules.Access.Persistence;

public sealed class ListUserRolesEndpoint(AccessDbContext db) : EndpointWithoutRequest<List<string>>
{
    public override void Configure() 
    { 
        Get("/access/users/{sub}/roles"); 
        Roles("admin"); 
    }

    public override async Task HandleAsync(CancellationToken ct)
    {
        var sub = Route<string>("sub")!;

        await Send.OkAsync(await db.UserRoles.Where(u => u.Sub == sub)
            .Join(db.Roles, u => u.RoleId, r => r.Id, (u, r) => r.Name).ToListAsync(ct), ct);
    }
}