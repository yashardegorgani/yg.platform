using FastEndpoints;
using Microsoft.EntityFrameworkCore;
using YG.BuildingBlocks.Auth;
using YG.BuildingBlocks.Messaging;
using YG.Modules.Access.Contracts;
using YG.Modules.Access.Persistence;

namespace YG.Modules.Access.Features.Grants;

public sealed class RevokeRoleEndpoint(AccessDbContext db, IUserContext user, IYGOutbox outbox)
    : EndpointWithoutRequest
{
    public override void Configure()
    {
        Delete("/access/users/{sub}/roles/{roleName}");
        Permissions("access.roles.assign");
    }

    public override async Task HandleAsync(CancellationToken ct)
    {
        var sub = Route<string>("sub")!;
        var roleName = Route<string>("roleName")!;

        if (sub == user.Sub && roleName == "admin")
        {
            AddError("You cannot revoke your own admin role.");
            await Send.ErrorsAsync(409, ct);
            return;
        }

        var userRole = await db.UserRoles
            .SingleOrDefaultAsync(u => u.Sub == sub
                && db.Roles.Any(r => r.Id == u.RoleId && r.Name == roleName), ct);

        if (userRole is null)
        {
            await Send.NoContentAsync(ct);   // nothing revoked: same outcome, NO event
            return;
        }

        outbox.Enroll(db);
        db.UserRoles.Remove(userRole);
        await outbox.PublishAsync(new UserRoleRevoked(sub, roleName));
        await outbox.SaveChangesAndPublishAsync(ct);

        await Send.NoContentAsync(ct);
    }
}