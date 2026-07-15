using Microsoft.EntityFrameworkCore;
using YG.Modules.Catalog.Contracts;
using YG.Modules.Catalog.Persistence;

namespace YG.Modules.Catalog.Features.Snapshots;

public static class GetProductSnapshotHandler
{
    public static async Task<ProductSnapshot?> Handle(
        GetProductSnapshot query, CatalogDbContext db, CancellationToken ct)
    {
        var product = await db.Products.AsNoTracking()
            .SingleOrDefaultAsync(p => p.Id == query.ProductId, ct);

        return product is null ? null : new ProductSnapshot(product.Id, product.Name, product.Price);
    }
}
