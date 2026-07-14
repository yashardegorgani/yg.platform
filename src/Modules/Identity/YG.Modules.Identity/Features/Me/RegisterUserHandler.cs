using Microsoft.EntityFrameworkCore;
using Wolverine;
using YG.Modules.Identity.Contracts;
using YG.Modules.Identity.Domain;
using YG.Modules.Identity.Persistence;

namespace YG.Modules.Identity.Features.Me;

public sealed record RegisterUser(string Sub, string Username);

public static class RegisterUserHandler
{
    public static async Task<bool> Handle(
        RegisterUser command, IdentityDbContext db, IMessageBus bus, CancellationToken ct)
    {
        var exists = await db.Users.AnyAsync(u => u.Sub == command.Sub, ct);
        if (exists) 
            return false;                  // idempotent: seen before, nothing to announce

        db.Users.Add(new User
        {
            Sub = command.Sub,
            Username = command.Username,
            FirstSeenAt = DateTimeOffset.UtcNow,
        });
        await db.SaveChangesAsync(ct);

        await bus.PublishAsync(new UserRegistered(command.Sub, command.Username));
        
        return true;                               // first sighting — announced to the world
    }
}