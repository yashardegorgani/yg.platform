using Wolverine;
using YG.Modules.Catalog.Contracts;
using YG.Modules.Inventory.Contracts;
using YG.Modules.Purchase.Domain;
using YG.Modules.Purchase.Persistence;

namespace YG.Modules.Purchase.Features.PlaceOrder;

public static class PlaceOrderHandler
{
    public static async Task<PlaceOrderResult> Handle(
        PlaceOrder command, PurchaseDbContext db, IMessageBus bus, CancellationToken ct)
    {
        // 1. Ask Catalog what this product is (name + price for the snapshot).
        //    Request/response across the boundary - Purchase never touches catalog.products.
        var snapshot = await bus.InvokeAsync<ProductSnapshot?>(
            new GetProductSnapshot(command.ProductId), ct);
        if (snapshot is null)
            return new(false, null, ReservationResult.UnknownProduct);

        // 2. Ask Inventory to reserve the stock. Inventory decides - Purchase
        //    never reads or writes inventory.stock_items.
        var reservation = await bus.InvokeAsync<ReservationResult>(
            new ReserveStock(command.ProductId, command.Quantity), ct);
        if (!reservation.Success)
            return new(false, null, reservation.Reason);

        // 3. Record the order with the snapshot values.
        var order = new Order
        {
            BuyerSub = command.BuyerSub,
            ProductId = command.ProductId,
            ProductName = snapshot.Name,
            UnitPrice = snapshot.Price,
            Quantity = command.Quantity,
        };
        db.Orders.Add(order);
        await db.SaveChangesAsync(ct);

        return new(true, order.Id, null);
    }
}
