using Wolverine;
using YG.BuildingBlocks.Messaging;
using YG.Modules.Catalog.Contracts;
using YG.Modules.Catalog.Domain;
using YG.Modules.Catalog.Persistence;

namespace YG.Modules.Catalog.Features.CreateProduct;

public static class CreateProductHandler
{
    // Convention: public "Handle", 1st param = message, rest injected from DI.
    public static async Task<Guid> Handle(
        CreateProduct command, CatalogDbContext db, IYGMessageBus bus, CancellationToken ct)
    {
        var product = new Product
        {
            Name = command.Name,
            Price = command.Price,
            Attributes = command.Attributes ?? new(),
        };
        db.Products.Add(product);
        await db.SaveChangesAsync(ct);

        await bus.PublishAsync(new ProductCreated(product.Id, product.Name));
        return product.Id;
    }
}