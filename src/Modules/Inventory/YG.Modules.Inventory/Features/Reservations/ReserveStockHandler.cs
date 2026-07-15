using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using YG.Modules.Inventory.Contracts;
using YG.Modules.Inventory.Persistence;

namespace YG.Modules.Inventory.Features.Reservations;

public static class ReserveStockHandler
{
    public static async Task<ReservationResult> Handle(
        ReserveStock command, InventoryDbContext db, ILogger<InventoryModule> logger, CancellationToken ct)
    {
        if (command.Quantity <= 0)
            return ReservationResult.Fail(ReservationResult.InvalidQuantity);

        // Atomic conditional decrement: the check and the take happen in ONE SQL
        // statement, so two concurrent buyers can never both take the last unit.
        // (A read-then-save here would be a classic race.)
        var taken = await db.StockItems
            .Where(s => s.ProductId == command.ProductId && s.Quantity >= command.Quantity)
            .ExecuteUpdateAsync(s => s
                .SetProperty(x => x.Quantity, x => x.Quantity - command.Quantity)
                .SetProperty(x => x.UpdatedAt, DateTimeOffset.UtcNow), ct);

        if (taken == 1)
        {
            logger.LogInformation("Inventory: reserved {Quantity} of {ProductId}",
                command.Quantity, command.ProductId);
            return ReservationResult.Ok();
        }

        // Nothing updated: either the product is unknown here, or stock was short.
        var exists = await db.StockItems.AnyAsync(s => s.ProductId == command.ProductId, ct);
        return ReservationResult.Fail(
            exists ? ReservationResult.InsufficientStock : ReservationResult.UnknownProduct);
    }
}
