using FastEndpoints;
using FluentValidation;
using Microsoft.EntityFrameworkCore;
using YG.Modules.Access.Domain;
using YG.Modules.Access.Persistence;

namespace YG.Modules.Access.Features.Permissions;

public sealed record GrantPermissionRequest(string Permission);

public sealed class GrantPermissionValidator : Validator<GrantPermissionRequest>
{
    public GrantPermissionValidator() =>
        RuleFor(x => x.Permission)
            .NotEmpty().MaximumLength(200)
            .Matches("^[a-z0-9-]+(\\.[a-z0-9-]+)+$")
            .WithMessage("Permissions look like 'module.resource.action' (lowercase, dot-separated).");
}

public sealed class GrantPermissionEndpoint(AccessDbContext db) : Endpoint<GrantPermissionRequest>
{
    public override void Configure()
    {
        Post("/access/roles/{roleName}/permissions");
        Permissions("access.roles.manage");
    }

    public override async Task HandleAsync(GrantPermissionRequest req, CancellationToken ct)
    {
        var roleName = Route<string>("roleName")!;

        var role = await db.Roles.SingleOrDefaultAsync(r => r.Name == roleName, ct);
        if (role is null) 
        { 
            await Send.NotFoundAsync(ct); 
            return; 
        }

        if (!await db.RolePermissions.AnyAsync(
                p => p.RoleId == role.Id && p.Permission == req.Permission, ct))
        {
            db.RolePermissions.Add(new RolePermission { RoleId = role.Id, Permission = req.Permission });
            await db.SaveChangesAsync(ct);
        }

        await Send.NoContentAsync(ct);   // idempotent: granted or already-granted, same outcome
    }
}