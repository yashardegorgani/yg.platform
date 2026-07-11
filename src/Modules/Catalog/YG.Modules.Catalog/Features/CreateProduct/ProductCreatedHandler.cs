using Microsoft.Extensions.Logging;

namespace YG.Modules.Catalog.Features.CreateProduct;

public sealed class ProductCreatedHandler(ILogger<ProductCreatedHandler> logger)
{
    public void Handle(ProductCreated @event)
        => logger.LogInformation("Product created: {ProductId} ({Name})", @event.Id, @event.Name);
}