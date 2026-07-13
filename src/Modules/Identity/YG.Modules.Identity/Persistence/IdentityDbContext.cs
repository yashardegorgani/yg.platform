using Microsoft.EntityFrameworkCore;
using YG.BuildingBlocks.Persistence;
using YG.Modules.Identity.Domain;

namespace YG.Modules.Identity.Persistence;

public sealed class IdentityDbContext(DbContextOptions<IdentityDbContext> options)
    : ModuleDbContext(options), ISchemaOwner
{
    public static string SchemaName => "identity";
    public override string Schema => SchemaName;

    public DbSet<User> Users => Set<User>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);        // applies HasDefaultSchema(Schema)

        modelBuilder.Entity<User>(b =>
        {
            b.ToTable("users");
            b.HasKey(u => u.Sub);
            b.Property(u => u.Sub).HasMaxLength(64);
            b.Property(u => u.Username).HasMaxLength(200);
        });
    }
}