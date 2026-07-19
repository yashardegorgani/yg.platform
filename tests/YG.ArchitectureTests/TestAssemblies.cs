using System.Reflection;

namespace YG.ArchitectureTests;

/// <summary>
/// One representative type per assembly pins the project reference and loads the assembly.
/// If a module is renamed, this file is the only place the tests need to learn about it.
/// </summary>
public static class TestAssemblies
{
    public static readonly (string Name, Assembly Assembly)[] Modules =
    [
        ("Catalog", typeof(Modules.Catalog.CatalogModule).Assembly),
        ("Identity", typeof(Modules.Identity.IdentityModule).Assembly),
        ("Access", typeof(Modules.Access.AccessModule).Assembly),
        ("Inventory", typeof(Modules.Inventory.InventoryModule).Assembly),
        ("Purchase", typeof(Modules.Purchase.PurchaseModule).Assembly),
    ];

    public static readonly (string Name, Assembly Assembly)[] Contracts =
    [
        ("Catalog.Contracts", typeof(Modules.Catalog.Contracts.ProductCreated).Assembly),
        ("Identity.Contracts", typeof(Modules.Identity.Contracts.UserRegistered).Assembly),
        ("Inventory.Contracts", typeof(Modules.Inventory.Contracts.ReserveStock).Assembly),
        ("Access.Contracts", typeof(Modules.Access.Contracts.UserRoleGranted).Assembly),
        ("Access.Contracts", typeof(Modules.Access.Contracts.UserRoleRevoked).Assembly),
    ];

    public static readonly (string Name, Assembly Assembly)[] BuildingBlocks =
    [
        ("BuildingBlocks.Auth", typeof(BuildingBlocks.Auth.IUserContext).Assembly),
        ("BuildingBlocks.Modules", typeof(BuildingBlocks.Modules.IYGModule).Assembly),
        ("BuildingBlocks.Persistence", typeof(BuildingBlocks.Persistence.ModuleDbContext).Assembly),
    ];

    public static readonly string[] ModuleNames =
        ["Catalog", "Identity", "Access", "Inventory", "Purchase"];
}
