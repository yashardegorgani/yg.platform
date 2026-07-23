using System.Text.Json;
using Microsoft.Extensions.Logging;
using YG.BuildingBlocks.Messaging;
using YG.Modules.Purchase.Features.PlaceOrder;
using YG.Modules.Workflow.Contracts;

namespace YG.Modules.Purchase.Features.Activities;

/// <summary>
/// Workflow capability "purchase.place-order": places an order on behalf of the
/// human who started the instance. Settings shape:
///   { "productIdFrom": "request.item", "quantityFrom": "request.quantity" }
/// Reuses the PlaceOrder command — one handler serves HTTP and workflows alike.
/// </summary>
public sealed class PlaceOrderActivity(IYGMessageBus bus, ILogger<PurchaseModule> logger)
    : IWorkflowActivity
{
    public string Type => "purchase.place-order";

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

        if (!TryResolvePath(context.InstanceContext, qtyKey, out var qtyElement) ||
            !TryReadInt(qtyElement, out var quantity) || quantity < 1)
            return ActivityResult.Failure($"Context key '{qtyKey}' is missing or not a positive quantity.");

        // The full cross-module dance happens inside the existing handler:
        // Catalog snapshot, Inventory reservation, order row — Purchase's own rules.
        var result = await bus.InvokeAsync<PlaceOrderResult>(
            new PlaceOrder.PlaceOrder(context.StartedBy, productId, quantity), ct);

        // By this point a check-stock step said yes — so a refusal here is a real
        // anomaly (race, vanished product), worth the retry/pend machinery.
        if (!result.Success)
            return ActivityResult.Failure(result.Reason ?? "Order rejected.");

        logger.LogInformation("Purchase: workflow {InstanceId} placed order {OrderId} for {Buyer}",
            context.InstanceId, result.OrderId, context.StartedBy);

        return ActivityResult.Success(JsonSerializer.SerializeToElement(
            new { orderId = result.OrderId, productId, quantity }));
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