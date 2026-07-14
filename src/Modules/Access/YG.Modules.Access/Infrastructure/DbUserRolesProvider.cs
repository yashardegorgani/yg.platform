using Microsoft.EntityFrameworkCore;
using YG.BuildingBlocks.Auth;
using YG.Modules.Access.Persistence;

namespace YG.Modules.Access.Infrastructure;

internal sealed class DbUserRolesProvider(AccessDbContext db) : IUserRolesProvider
{
    public async Task<IReadOnlyCollection<string>> GetRolesAsync(string sub, CancellationToken ct)
        => await db.UserRoles
            .Where(u => u.Sub == sub)
            .Join(db.Roles, u => u.RoleId, r => r.Id, (u, r) => r.Name)
            .ToListAsync(ct);
}