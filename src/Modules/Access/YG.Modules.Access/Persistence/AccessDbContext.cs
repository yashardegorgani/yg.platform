using Microsoft.EntityFrameworkCore;
using YG.BuildingBlocks.Persistence;
using YG.Modules.Access.Domain;

namespace YG.Modules.Access.Persistence;

public sealed class AccessDbContext(DbContextOptions<AccessDbContext> options)
    : ModuleDbContext(options), ISchemaOwner
{
    public static string SchemaName => "access";
    public override string Schema => SchemaName;

    public DbSet<Role> Roles => Set<Role>();
    public DbSet<UserRole> UserRoles => Set<UserRole>();
    public DbSet<RolePermission> RolePermissions => Set<RolePermission>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        modelBuilder.Entity<Role>(b =>
        {
            b.ToTable("roles");
            b.HasKey(r => r.Id);
            b.Property(r => r.Name).HasMaxLength(100);
            b.HasIndex(r => r.Name).IsUnique();
            b.HasData(
                new Role { Id = Role.MemberRoleId, Name = "member", Description = "Default role granted to every registered user" },
                new Role { Id = Role.AdminRoleId, Name = "admin", Description = "Access administration" });
        });

        modelBuilder.Entity<UserRole>(b =>
        {
            b.ToTable("user_roles");
            b.HasKey(u => new { u.Sub, u.RoleId });              // composite: idempotent grants for free
            b.Property(u => u.Sub).HasMaxLength(64);
        });

        modelBuilder.Entity<RolePermission>(b =>
        {
            b.ToTable("role_permissions");
            b.HasKey(p => new { p.RoleId, p.Permission });   // composite PK: idempotent grants, third time
            b.Property(p => p.Permission).HasMaxLength(200);

            b.HasData(
                // member: everyday capabilities
                new RolePermission { RoleId = Role.MemberRoleId, Permission = "catalog.products.list" },
                new RolePermission { RoleId = Role.MemberRoleId, Permission = "catalog.products.create" },
                new RolePermission { RoleId = Role.MemberRoleId, Permission = "inventory.stock.read" },
                new RolePermission { RoleId = Role.MemberRoleId, Permission = "purchase.orders.place" },
                new RolePermission { RoleId = Role.MemberRoleId, Permission = "purchase.orders.list" },

                // admin: stewardship
                new RolePermission { RoleId = Role.AdminRoleId, Permission = "inventory.stock.set" },
                new RolePermission { RoleId = Role.AdminRoleId, Permission = "access.roles.manage" },
                new RolePermission { RoleId = Role.AdminRoleId, Permission = "access.grants.manage" });
        });
    }
}