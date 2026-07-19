using FastEndpoints;
using Microsoft.EntityFrameworkCore;
using YG.Modules.Access.Persistence;

namespace YG.Modules.Access.Features.Grants;

public sealed record UserRoleView(string Role, string? GrantedBy, DateTimeOffset? GrantedAt);

public sealed class ListUserRolesEndpoint(AccessDbContext db) : EndpointWithoutRequest<List<UserRoleView>>
{
    public override void Configure()
    {
        Get("/access/users/{sub}/roles");
        Permissions("access.roles.assign");
    }

    public override async Task HandleAsync(CancellationToken ct)
    {
        var sub = Route<string>("sub")!;

        await Send.OkAsync(await db.UserRoles.Where(u => u.Sub == sub)
            .Join(db.Roles, u => u.RoleId, r => r.Id,
                (u, r) => new UserRoleView(r.Name, u.GrantedBy, u.GrantedAt))
            .ToListAsync(ct), ct);
    }
}