using FastEndpoints;
using Microsoft.EntityFrameworkCore;
using YG.Modules.Access.Domain;
using YG.Modules.Access.Persistence;

namespace YG.Modules.Access.Features.Grants;

public sealed record GrantRoleRequest(string RoleName);

public sealed class GrantRoleEndpoint(AccessDbContext db) : Endpoint<GrantRoleRequest>
{
    public override void Configure()
    {
        Post("/access/users/{sub}/roles");
        Permissions("access.grants.manage");
    }

    public override async Task HandleAsync(GrantRoleRequest req, CancellationToken ct)
    {
        var sub = Route<string>("sub")!;
        var role = await db.Roles.SingleOrDefaultAsync(r => r.Name == req.RoleName, ct);
        if (role is null)
        {
            await Send.NotFoundAsync(ct);
            return;
        }

        if (!await db.UserRoles.AnyAsync(u => u.Sub == sub && u.RoleId == role.Id, ct))
        {
            db.UserRoles.Add(new UserRole { Sub = sub, RoleId = role.Id });
            await db.SaveChangesAsync(ct);
        }

        await Send.NoContentAsync(ct);   // idempotent: granted or already-granted = same outcome
    }
}
