using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace YG.BuildingBlocks.Persistence;

/// <summary>
/// Dev-time provisioning: creates the database + this context's tables at startup.
/// NOTE: EnsureCreated is a learning-phase shortcut — it does nothing if the
/// database already has tables, which breaks once a SECOND module context exists.
/// We replace this with real EF migrations in step 6.
/// </summary>
public sealed class EnsureDatabaseHostedService<TContext>(
    IServiceProvider serviceProvider,
    ILogger<EnsureDatabaseHostedService<TContext>> logger) : IHostedService
    where TContext : ModuleDbContext
{
    public async Task StartAsync(CancellationToken cancellationToken)
    {
        await using var scope = serviceProvider.CreateAsyncScope();
        var db = scope.ServiceProvider.GetRequiredService<TContext>();
        var created = await db.Database.EnsureCreatedAsync(cancellationToken);
        logger.LogInformation("{Context}: database {Result}",
            typeof(TContext).Name, created ? "created" : "already existed");
    }

    public Task StopAsync(CancellationToken cancellationToken) => Task.CompletedTask;
}