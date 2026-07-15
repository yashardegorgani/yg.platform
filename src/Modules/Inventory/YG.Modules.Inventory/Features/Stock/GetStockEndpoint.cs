using FastEndpoints;
using Microsoft.EntityFrameworkCore;
using YG.Modules.Inventory.Persistence;

namespace YG.Modules.Inventory.Features.Stock;

public sealed record StockResponse(Guid ProductId, int Quantity, DateTimeOffset UpdatedAt);

public sealed class GetStockEndpoint(InventoryDbContext db) : EndpointWithoutRequest<StockResponse>
{
    public override void Configure()
    {
        Get("/inventory/stock/{productId}");
        Permissions("inventory.stock.read");
    }

    public override async Task HandleAsync(CancellationToken ct)
    {
        var productId = Route<Guid>("productId");

        var item = await db.StockItems.AsNoTracking()
            .SingleOrDefaultAsync(s => s.ProductId == productId, ct);

        if (item is null)
        {
            await Send.NotFoundAsync(ct);
            return;
        }

        await Send.OkAsync(new StockResponse(item.ProductId, item.Quantity, item.UpdatedAt), ct);
    }
}
