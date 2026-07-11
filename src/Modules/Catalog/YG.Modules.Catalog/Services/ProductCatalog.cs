namespace YG.Modules.Catalog.Services;

public sealed record ProductDto(Guid Id, string Name, decimal Price);

public interface IProductCatalog
{
    IReadOnlyList<ProductDto> All { get; }
}

// internal: even projects that reference this assembly can't touch the implementation.
internal sealed class InMemoryProductCatalog : IProductCatalog
{
    public IReadOnlyList<ProductDto> All { get; } =
    [
        new(Guid.NewGuid(), "Keyboard", 49.99m),
        new(Guid.NewGuid(), "Mouse", 24.50m),
    ];
}