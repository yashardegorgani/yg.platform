using FastEndpoints;
using Microsoft.EntityFrameworkCore;
using Wolverine;
using YG.BuildingBlocks.Auth;
using YG.BuildingBlocks.Messaging;
using YG.Modules.Access.Contracts;
using YG.Modules.Access.Domain;
using YG.Modules.Access.Persistence;

namespace YG.Modules.Access.Features.Grants;

public sealed record GrantRoleRequest(string RoleName);

public sealed class GrantRoleEndpoint(AccessDbContext db, IUserContext user, IYGMessageBus bus)
    : Endpoint<GrantRoleRequest>
{
    public override void Configure()
    {
        Post("/access/users/{sub}/roles");
        Permissions("access.roles.assign");
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

        if (await db.UserRoles.AnyAsync(u => u.Sub == sub && u.RoleId == role.Id, ct))
        {
            await Send.NoContentAsync(ct);   // already granted: same outcome, and NO event — nothing happened
            return;
        }

        db.UserRoles.Add(new UserRole
        {
            Sub = sub,
            RoleId = role.Id,
            GrantedBy = user.Sub,
            GrantedAt = DateTimeOffset.UtcNow,
        });
        await db.SaveChangesAsync(ct);

        await bus.PublishAsync(new UserRoleGranted(sub, role.Name));

        await Send.NoContentAsync(ct);
    }
}