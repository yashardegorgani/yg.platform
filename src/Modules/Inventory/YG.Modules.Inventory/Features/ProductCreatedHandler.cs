using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using YG.Modules.Catalog.Contracts;
using YG.Modules.Inventory.Domain;
using YG.Modules.Inventory.Persistence;

namespace YG.Modules.Inventory.Features;

// Third lawful arrow: Inventory -> Catalog.Contracts. Inventory reacts to a fact
// Catalog published; Catalog has no idea this handler exists.
public static class ProductCreatedHandler
{
    public static async Task Handle(
        ProductCreated @event, InventoryDbContext db, ILogger<InventoryModule> logger, CancellationToken ct)
    {
        if (await db.StockItems.AnyAsync(s => s.ProductId == @event.Id, ct))
            return;                                    // replay-safe, like every subscriber

        db.StockItems.Add(new StockItem { ProductId = @event.Id, Quantity = 0 });
        await db.SaveChangesAsync(ct);

        logger.LogInformation("Inventory: stock row opened at 0 for product {ProductId} ({Name})",
            @event.Id, @event.Name);
    }
}
