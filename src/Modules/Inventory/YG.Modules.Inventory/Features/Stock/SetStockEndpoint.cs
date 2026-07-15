using FastEndpoints;
using FluentValidation;
using Microsoft.EntityFrameworkCore;
using YG.Modules.Inventory.Domain;
using YG.Modules.Inventory.Persistence;

namespace YG.Modules.Inventory.Features.Stock;

public sealed record SetStockRequest(int Quantity);

public sealed class SetStockValidator : Validator<SetStockRequest>
{
    public SetStockValidator() => RuleFor(x => x.Quantity).GreaterThanOrEqualTo(0);
}

public sealed class SetStockEndpoint(InventoryDbContext db) : Endpoint<SetStockRequest>
{
    public override void Configure()
    {
        Put("/inventory/stock/{productId}");
        Permissions("inventory.stock.set");
    }

    public override async Task HandleAsync(SetStockRequest req, CancellationToken ct)
    {
        var productId = Route<Guid>("productId");

        var item = await db.StockItems.SingleOrDefaultAsync(s => s.ProductId == productId, ct);
        if (item is null)
        {
            // Upsert doubles as backfill for products created before Inventory subscribed.
            db.StockItems.Add(new StockItem { ProductId = productId, Quantity = req.Quantity });
        }
        else
        {
            item.Quantity = req.Quantity;
            item.UpdatedAt = DateTimeOffset.UtcNow;
        }

        await db.SaveChangesAsync(ct);
        await Send.NoContentAsync(ct);
    }
}
