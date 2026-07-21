using Microsoft.EntityFrameworkCore;
using YG.BuildingBlocks.Persistence;
using YG.Modules.Catalog.Domain;

namespace YG.Modules.Catalog.Persistence;

public sealed class CatalogDbContext(DbContextOptions<CatalogDbContext> options)
    : ModuleDbContext(options), ISchemaOwner
{
    public static string SchemaName => "catalog";
    public override string Schema => SchemaName;

    public DbSet<Product> Products => Set<Product>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);   // applies HasDefaultSchema(Schema)

        modelBuilder.Entity<Product>(b =>
        {
            b.ToTable("products");
            b.HasKey(p => p.Id);
            b.Property(p => p.Name).HasMaxLength(200);

            // Flexible attributes as jsonb, searchable via GIN.
            b.Property(p => p.Attributes).HasColumnType("jsonb")
                .HasComment("Free-form product attributes: {key: value}, varies per product. Searchable via the GIN index.");
            b.HasIndex(p => p.Attributes).HasMethod("gin");
        });
    }
}