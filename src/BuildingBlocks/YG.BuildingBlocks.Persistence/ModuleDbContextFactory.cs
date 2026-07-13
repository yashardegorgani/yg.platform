using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;

namespace YG.BuildingBlocks.Persistence;

public abstract class ModuleDbContextFactory<TContext> : IDesignTimeDbContextFactory<TContext>
    where TContext : ModuleDbContext, ISchemaOwner
{
    // Never used to connect: Add-Migration only compares model snapshots.
    private const string DesignTimeConnectionString = "Host=localhost;Database=design-time-only";

    public TContext CreateDbContext(string[] args)
    {
        var options = new DbContextOptionsBuilder<TContext>()
            .UseNpgsql(DesignTimeConnectionString,
                npgsql => npgsql.MigrationsHistoryTable("__ef_migrations", TContext.SchemaName))
            .Options;

        return (TContext)Activator.CreateInstance(typeof(TContext), options)!;
    }
}