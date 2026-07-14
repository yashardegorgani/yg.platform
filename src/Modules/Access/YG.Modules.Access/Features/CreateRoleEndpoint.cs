using FastEndpoints;
using Microsoft.EntityFrameworkCore;
using YG.Modules.Access.Domain;
using YG.Modules.Access.Persistence;

public sealed record CreateRoleRequest(string Name, string? Description);

public sealed class CreateRoleEndpoint(AccessDbContext db) : Endpoint<CreateRoleRequest, RoleResponse>
{
    public override void Configure() 
    { 
        Post("/access/roles"); 
        Roles("admin"); 
    }

    public override async Task HandleAsync(CreateRoleRequest req, CancellationToken ct)
    {
        var existing = await db.Roles.SingleOrDefaultAsync(r => r.Name == req.Name, ct);
        if (existing is not null)   // idempotent create — "ensure" semantics again
        { 
            await Send.OkAsync(new RoleResponse(existing.Id, existing.Name, existing.Description), ct); 
            return; 
        }

        var role = new Role { Id = Guid.NewGuid(), Name = req.Name, Description = req.Description };
        db.Roles.Add(role);
        await db.SaveChangesAsync(ct);

        await Send.OkAsync(new RoleResponse(role.Id, role.Name, role.Description), ct);
    }
}