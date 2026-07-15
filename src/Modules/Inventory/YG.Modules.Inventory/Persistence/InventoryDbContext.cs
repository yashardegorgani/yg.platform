using Microsoft.EntityFrameworkCore;
using YG.BuildingBlocks.Persistence;
using YG.Modules.Inventory.Domain;

namespace YG.Modules.Inventory.Persistence;

public sealed class InventoryDbContext(DbContextOptions<InventoryDbContext> options)
    : ModuleDbContext(options), ISchemaOwner
{
    public static string SchemaName => "inventory";
    public override string Schema => SchemaName;

    public DbSet<StockItem> StockItems => Set<StockItem>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);   // applies HasDefaultSchema(Schema)

        modelBuilder.Entity<StockItem>(b =>
        {
            b.ToTable("stock_items");
            b.HasKey(s => s.ProductId);       // one stock row per product, PK enforces it
        });
    }
}
