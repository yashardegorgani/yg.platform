using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using System.Text.Json;
using YG.Modules.Inventory.Persistence;
using YG.Modules.Workflow.Contracts;

namespace YG.Modules.Inventory.Features.Activities;

/// <summary>
/// Workflow capability "inventory.adjust-stock": adjusts a product's stock level.
/// Settings shape: { "delta": -1, "productIdFrom": "productId" }
///   delta         — signed adjustment, fixed by the workflow designer
///   productIdFrom — context key holding the product GUID (collected earlier in the instance)
/// Inventory owns this class entirely; Workflow only ever sees IWorkflowActivity.
/// </summary>
public sealed class AdjustStockActivity(InventoryDbContext db, ILogger<InventoryModule> logger)
    : IWorkflowActivity
{
    public string Type => "inventory.adjust-stock";

    public async Task<ActivityResult> ExecuteAsync(ActivityContext context, CancellationToken ct)
    {
        if (context.Settings is not { } settings ||
            !settings.TryGetProperty("delta", out var d) ||
            !settings.TryGetProperty("productIdFrom", out var keyProp) ||
            keyProp.GetString() is not { } contextKey)
            return ActivityResult.Failure("Settings must contain 'delta' and 'productIdFrom'.");

        var delta = d.GetInt32();

        if (!context.InstanceContext.TryResolvePath(contextKey, out var idElement) ||
            idElement.ValueKind != JsonValueKind.String ||
            !Guid.TryParse(idElement.GetString(), out var productId))
            return ActivityResult.Failure(
                $"Context key '{contextKey}' is missing or not a product GUID.");

        // Same atomic pattern as ReserveStockHandler: check and change in ONE
        // statement. Negative deltas may not take stock below zero.
        var adjusted = await db.StockItems
            .Where(s => s.ProductId == productId && s.Quantity + delta >= 0)
            .ExecuteUpdateAsync(s => s
                .SetProperty(x => x.Quantity, x => x.Quantity + delta)
                .SetProperty(x => x.UpdatedAt, DateTimeOffset.UtcNow), ct);

        if (adjusted == 0)
        {
            var exists = await db.StockItems.AnyAsync(s => s.ProductId == productId, ct);
            return ActivityResult.Failure(exists
                ? $"Adjustment of {delta} would take product {productId} below zero."
                : $"No stock row for product {productId}.");
        }

        var quantity = await db.StockItems
            .Where(s => s.ProductId == productId)
            .Select(s => s.Quantity)
            .FirstAsync(ct);

        logger.LogInformation("Inventory: workflow {InstanceId} adjusted {ProductId} by {Delta} → {Quantity}",
            context.InstanceId, productId, delta, quantity);

        return ActivityResult.Success(JsonSerializer.SerializeToElement(
            new { productId, delta, quantity }));
    }
}