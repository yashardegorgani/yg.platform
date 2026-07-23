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

        if (!TryResolvePath(context.InstanceContext, idKey, out var idElement) ||
            idElement.ValueKind != JsonValueKind.String ||
            !Guid.TryParse(idElement.GetString(), out var productId))
            return ActivityResult.Failure($"Context key '{idKey}' is missing or not a product GUID.");

        // Form fields arrive as strings; designers may also record real numbers. Take both.
        if (!TryResolvePath(context.InstanceContext, qtyKey, out var qtyElement) ||
            !TryReadInt(qtyElement, out var requested) || requested < 1)
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

    private static bool TryReadInt(JsonElement element, out int value)
    {
        value = 0;
        return element.ValueKind switch
        {
            JsonValueKind.Number => element.TryGetInt32(out value),
            JsonValueKind.String => int.TryParse(element.GetString(), out value),
            _ => false,
        };
    }

    /// <summary>Dotted paths into context: "request.quantity" = context["request"].quantity.</summary>
    private static bool TryResolvePath(
        IReadOnlyDictionary<string, JsonElement> ctx, string path, out JsonElement element)
    {
        element = default;
        var segments = path.Split('.');
        if (!ctx.TryGetValue(segments[0], out element))
            return false;

        foreach (var segment in segments.Skip(1))
        {
            if (element.ValueKind != JsonValueKind.Object ||
                !element.TryGetProperty(segment, out element))
                return false;
        }
        return true;
    }
}