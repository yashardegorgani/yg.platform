using FastEndpoints;
using Microsoft.EntityFrameworkCore;
using YG.Modules.Access.Persistence;

public sealed record RoleResponse(Guid Id, string Name, string? Description);

public sealed class ListRolesEndpoint(AccessDbContext db) : EndpointWithoutRequest<List<RoleResponse>>
{
    public override void Configure() 
    { 
        Get("/access/roles"); 
        Roles("admin"); 
    }

    public override async Task HandleAsync(CancellationToken ct)
    {
        await Send.OkAsync(await db.Roles.AsNoTracking()
            .Select(r => new RoleResponse(r.Id, r.Name, r.Description)).ToListAsync(ct), ct);
    }
}