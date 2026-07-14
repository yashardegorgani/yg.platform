using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using YG.Modules.Access.Domain;
using YG.Modules.Access.Persistence;

namespace YG.Modules.Access.Infrastructure;

internal sealed class BootstrapAdminsHostedService(
    IServiceScopeFactory scopeFactory, IConfiguration configuration) : IHostedService
{
    public async Task StartAsync(CancellationToken ct)
    {
        var subs = configuration.GetSection("Access:BootstrapAdminSubs").Get<string[]>() ?? [];
        if (subs.Length == 0) return;

        using var scope = scopeFactory.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<AccessDbContext>();

        foreach (var sub in subs)
            if (!await db.UserRoles.AnyAsync(u => u.Sub == sub && u.RoleId == Role.AdminRoleId, ct))
                db.UserRoles.Add(new UserRole { Sub = sub, RoleId = Role.AdminRoleId });

        await db.SaveChangesAsync(ct);
    }

    public Task StopAsync(CancellationToken ct) => Task.CompletedTask;
}