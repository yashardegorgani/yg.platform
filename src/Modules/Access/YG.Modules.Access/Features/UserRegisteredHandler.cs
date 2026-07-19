using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Wolverine;
using YG.BuildingBlocks.Messaging;
using YG.Modules.Access.Contracts;
using YG.Modules.Access.Domain;
using YG.Modules.Access.Persistence;
using YG.Modules.Identity.Contracts;

namespace YG.Modules.Access.Features;

public static class UserRegisteredHandler
{
    public static async Task Handle(
        UserRegistered message, AccessDbContext db, IYGMessageBus bus,
        ILogger<AccessModule> logger, CancellationToken ct)
    {
        var alreadyGranted = await db.UserRoles.AnyAsync(
            u => u.Sub == message.Sub && u.RoleId == Role.MemberRoleId, ct);
        if (alreadyGranted)
            return;

        db.UserRoles.Add(new UserRole
        {
            Sub = message.Sub,
            RoleId = Role.MemberRoleId,
            GrantedAt = DateTimeOffset.UtcNow,   // GrantedBy stays null: system grant
        });
        await db.SaveChangesAsync(ct);

        await bus.PublishAsync(new UserRoleGranted(message.Sub, "member"));

        logger.LogInformation("Access: granted 'member' to {Sub} ({Username})",
            message.Sub, message.Username);
    }
}