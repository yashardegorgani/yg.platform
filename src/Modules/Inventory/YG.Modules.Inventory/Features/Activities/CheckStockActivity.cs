using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using System.Text.Json;
using YG.Modules.Inventory.Persistence;
using YG.Modules.Workflow.Contracts;

namespace YG.Modules.Inventory.Features.Activities;

/// <summary>
/// Workflow capability "inventory.check-stock": records whether stock can cover
/// a requested quantity. Settings shape:
///   { "productIdFrom": "request.item", "quantityFrom": "request.quantity" }
/// Both are dotted context paths. Output: { productId, requested, quantity, sufficient }.
/// The workflow ROUTES on this recording — conditions read state, steps compute it.
/// </summary>
public sealed class CheckStockActivity(InventoryDbContext db, ILogger<InventoryModule> logger)
    : IWorkflowActivity
{
    public string Type => "inventory.check-stock";

    public async Task<ActivityResult> ExecuteAsync(ActivityContext context, CancellationToken ct)
    {
        if (context.Settings is not { } settings ||
            !settings.TryGetProperty("productIdFrom", out var idKeyProp) ||
            !settings.TryGetProperty("quantityFrom", out var qtyKeyProp) ||
            idKeyProp.GetString() is not { } idKey ||
            qtyKeyProp.GetString() is not { } qtyKey)
            return ActivityResult.Failure("Settings must contain 'productIdFrom' and 'quantityFrom'.");

        if (!context.InstanceContext.TryResolvePath(idKey, out var idElement) ||
            idElement.ValueKind != JsonValueKind.String ||
            !Guid.TryParse(idElement.GetString(), out var productId))
            return ActivityResult.Failure($"Context key '{idKey}' is missing or not a product GUID.");

        if (!context.InstanceContext.TryResolvePath(qtyKey, out var qtyElement) ||
            !qtyElement.TryReadInt(out var requested) || requested < 1)
            return ActivityResult.Failure($"Context key '{qtyKey}' is missing or not a positive quantity.");

        // No stock row = zero stock. That's an ANSWER, not an error.
        var quantity = await db.StockItems
            .Where(s => s.ProductId == productId)
            .Select(s => (int?)s.Quantity)
            .FirstOrDefaultAsync(ct) ?? 0;

        var sufficient = quantity >= requested;

        logger.LogInformation(
            "Inventory: workflow {InstanceId} checked {ProductId}: {Quantity} on hand, {Requested} requested → {Sufficient}",
            context.InstanceId, productId, quantity, requested, sufficient);

        return ActivityResult.Success(JsonSerializer.SerializeToElement(
            new { productId, requested, quantity, sufficient }));
    }
}