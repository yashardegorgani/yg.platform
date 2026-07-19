using Microsoft.EntityFrameworkCore;
using Npgsql;
using Wolverine;
using YG.BuildingBlocks.Messaging;
using YG.Modules.Identity.Contracts;
using YG.Modules.Identity.Domain;
using YG.Modules.Identity.Persistence;

namespace YG.Modules.Identity.Features.Register;

public static class RegisterUserHandler
{
    public static async Task<bool> Handle(
        RegisterUser command, IdentityDbContext db, IYGMessageBus bus, CancellationToken ct)
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

        try
        {
            await db.SaveChangesAsync(ct);
        }
        catch (DbUpdateException ex) when (
            ex.InnerException is PostgresException { SqlState: PostgresErrorCodes.UniqueViolation })
        {
            // Lost a race with a concurrent first sighting. Same outcome as the
            // AnyAsync short-circuit above: the user exists, nothing to announce.
            return false;
        }

        await bus.PublishAsync(new UserRegistered(command.Sub, command.Username));

        return true;                       // first sighting - announced to the world
    }
}
