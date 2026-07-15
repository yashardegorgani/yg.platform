using Microsoft.EntityFrameworkCore;
using YG.BuildingBlocks.Persistence;
using YG.Modules.Purchase.Domain;

namespace YG.Modules.Purchase.Persistence;

public sealed class PurchaseDbContext(DbContextOptions<PurchaseDbContext> options)
    : ModuleDbContext(options), ISchemaOwner
{
    public static string SchemaName => "purchase";
    public override string Schema => SchemaName;

    public DbSet<Order> Orders => Set<Order>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);   // applies HasDefaultSchema(Schema)

        modelBuilder.Entity<Order>(b =>
        {
            b.ToTable("orders");
            b.HasKey(o => o.Id);
            b.Property(o => o.BuyerSub).HasMaxLength(64);
            b.Property(o => o.ProductName).HasMaxLength(200);
            b.HasIndex(o => o.BuyerSub);      // "my orders" is the hot query
        });
    }
}
