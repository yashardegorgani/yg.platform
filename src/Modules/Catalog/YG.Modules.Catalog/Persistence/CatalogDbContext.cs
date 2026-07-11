using Microsoft.EntityFrameworkCore;
using YG.BuildingBlocks.Persistence;
using YG.Modules.Catalog.Domain;

namespace YG.Modules.Catalog.Persistence;

public sealed class CatalogDbContext(DbContextOptions<CatalogDbContext> options)
    : ModuleDbContext(options)
{
    public const string SchemaName = "catalog";
    public override string Schema => SchemaName;
    

    public DbSet<Product> Products => Set<Product>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);   // applies HasDefaultSchema("catalog")

        modelBuilder.Entity<Product>(product =>
        {
            product.ToTable("products");
            product.Property(x => x.Name).HasMaxLength(200);
            product.Property(x => x.Price).HasPrecision(18, 2);
            product.Property(x => x.Attributes).HasColumnType("jsonb");
            product.HasIndex(x => x.Attributes).HasMethod("gin");  // indexed json queries
        });
    }
}