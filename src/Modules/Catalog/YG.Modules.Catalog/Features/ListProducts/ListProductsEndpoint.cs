using FastEndpoints;
using Microsoft.EntityFrameworkCore;
using YG.Modules.Catalog.Domain;
using YG.Modules.Catalog.Persistence;

namespace YG.Modules.Catalog.Features.ListProducts;

public sealed class ListProductsEndpoint(CatalogDbContext db)
    : EndpointWithoutRequest<List<Product>>
{
    public override void Configure()
    {
        Get("/catalog/products");
        Roles("member");
    }

    public override async Task HandleAsync(CancellationToken ct)
        => await Send.OkAsync(
            await db.Products.AsNoTracking().OrderByDescending(p => p.CreatedAt).ToListAsync(ct), ct);
}