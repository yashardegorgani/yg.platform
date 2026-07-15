using FastEndpoints;
using Microsoft.EntityFrameworkCore;
using YG.BuildingBlocks.Auth;
using YG.Modules.Purchase.Persistence;

namespace YG.Modules.Purchase.Features.ListOrders;

public sealed record OrderResponse(
    Guid Id, Guid ProductId, string ProductName, decimal UnitPrice, int Quantity, DateTimeOffset PlacedAt);

public sealed class ListMyOrdersEndpoint(PurchaseDbContext db, IUserContext user)
    : EndpointWithoutRequest<List<OrderResponse>>
{
    public override void Configure()
    {
        Get("/purchase/orders");
        Permissions("purchase.orders.list");
    }

    public override async Task HandleAsync(CancellationToken ct)
    {
        await Send.OkAsync(await db.Orders.AsNoTracking()
            .Where(o => o.BuyerSub == user.Sub)
            .OrderByDescending(o => o.PlacedAt)
            .Select(o => new OrderResponse(
                o.Id, o.ProductId, o.ProductName, o.UnitPrice, o.Quantity, o.PlacedAt))
            .ToListAsync(ct), ct);
    }
}
