using FastEndpoints;
using Microsoft.EntityFrameworkCore;
using YG.Modules.Identity.Persistence;

namespace YG.Modules.Identity.Features.Directory;

public sealed record UserSummary(string Sub, string Username);

public sealed class ListUsersEndpoint(IdentityDbContext db)
    : EndpointWithoutRequest<List<UserSummary>>
{
    public override void Configure()
    {
        Get("/identity/users");
    }

    public override async Task HandleAsync(CancellationToken ct)
    {
        await Send.OkAsync(await db.Users.AsNoTracking()
            .OrderBy(u => u.Username)
            .Select(u => new UserSummary(u.Sub, u.Username))
            .ToListAsync(ct), ct);
    }
}