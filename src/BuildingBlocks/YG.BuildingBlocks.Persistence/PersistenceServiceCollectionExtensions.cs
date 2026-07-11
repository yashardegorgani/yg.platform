using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace YG.BuildingBlocks.Persistence;

public static class PersistenceServiceCollectionExtensions
{
    public static IServiceCollection AddModuleDbContext<TContext>(
        this IServiceCollection services,
        IConfiguration configuration,
        string schema)
        where TContext : ModuleDbContext
    {
        var connectionString = configuration.GetConnectionString("Default")
            ?? throw new InvalidOperationException("ConnectionStrings:Default is missing.");

        services.AddDbContext<TContext>(options =>
            options.UseNpgsql(connectionString, npgsql =>
            {
                npgsql.MigrationsHistoryTable("__ef_migrations", schema);
                // Allows plain POCOs (like ProductAttributes) to map to jsonb columns.
                npgsql.ConfigureDataSource(ds => ds.EnableDynamicJson());
            }));

        if (configuration.GetValue("Database:AutoProvision", defaultValue: true))
            services.AddHostedService<EnsureDatabaseHostedService<TContext>>();

        return services;
    }
}