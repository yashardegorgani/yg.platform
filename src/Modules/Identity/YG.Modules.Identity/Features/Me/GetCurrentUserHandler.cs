using Microsoft.EntityFrameworkCore;
using YG.Modules.Identity.Domain;
using YG.Modules.Identity.Persistence;

namespace YG.Modules.Identity.Features.Me;

public static class GetCurrentUserHandler
{
    public static Task<User?> Handle(GetCurrentUser query, IdentityDbContext db, CancellationToken ct)
        => db.Users.AsNoTracking().SingleOrDefaultAsync(u => u.Sub == query.Sub, ct);
}
