using Microsoft.EntityFrameworkCore;

namespace YG.BuildingBlocks.Persistence;

/// <summary>
/// Base DbContext for all modules. Forces every module's tables into its own
/// PostgreSQL schema — boundary separation at the database level.
/// Rule that makes this matter: no module ever touches another module's schema.
/// </summary>
public abstract class ModuleDbContext(DbContextOptions options) : DbContext(options)
{
    public abstract string Schema { get; }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.HasDefaultSchema(Schema);
        base.OnModelCreating(modelBuilder);
    }
}