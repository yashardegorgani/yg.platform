using FastEndpoints;
using YG.Modules.Catalog.Services;

namespace YG.Modules.Catalog.Features.ListProducts;

public sealed class ListProductsEndpoint(IProductCatalog catalog)
    : EndpointWithoutRequest<IReadOnlyList<ProductDto>>
{
    public override void Configure()
    {
        Get("/catalog/products");
        AllowAnonymous();
    }

    public override async Task HandleAsync(CancellationToken ct)
        => await Send.OkAsync(catalog.All, ct);
}