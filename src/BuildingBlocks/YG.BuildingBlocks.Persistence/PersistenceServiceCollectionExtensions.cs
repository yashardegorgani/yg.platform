using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace YG.BuildingBlocks.Persistence;

public static class PersistenceServiceCollectionExtensions
{
    public static IServiceCollection AddModuleDbContext<TContext>(
        this IServiceCollection services, IConfiguration configuration)
        where TContext : ModuleDbContext, ISchemaOwner
    {
        var connectionString = configuration.GetConnectionString("Default")
            ?? throw new InvalidOperationException("ConnectionStrings:Default is missing.");

        services.AddDbContext<TContext>(options =>
            options.UseNpgsql(connectionString, npgsql =>
            {
                npgsql.MigrationsHistoryTable("__ef_migrations", TContext.SchemaName);
                // Allows plain POCOs (like ProductAttributes) to map to jsonb columns.
                npgsql.ConfigureDataSource(ds => ds.EnableDynamicJson());
            }),
            optionsLifetime: ServiceLifetime.Singleton);

        if (configuration.GetValue("Database:AutoMigrate", true))
        {
            services.AddHostedService<MigrateDatabaseHostedService<TContext>>();
        }

        return services;
    }
}