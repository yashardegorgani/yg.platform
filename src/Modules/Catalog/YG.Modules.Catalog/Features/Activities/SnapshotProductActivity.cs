using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using System.Diagnostics;
using System.Text.Json;
using YG.Modules.Catalog.Persistence;
using YG.Modules.Workflow.Contracts;

namespace YG.Modules.Catalog.Features.Activities;

/// <summary>
/// Workflow capability "catalog.snapshot-product": records existence, name, price,
/// and order total (price × quantity) for a requested product. Settings shape:
///   { "productIdFrom": "request.item", "quantityFrom": "request.quantity" }
/// Unknown or malformed product id is an ANSWER (exists: false), not a failure —
/// definitions route on it. Conditions can't do math, so the TOTAL is computed
/// here and recorded; transitions merely read the recording.
/// </summary>
public sealed class SnapshotProductActivity(CatalogDbContext db, ILogger<CatalogModule> logger)
    : IWorkflowActivity
{
    public string Type => "catalog.snapshot-product";

    public async Task<ActivityResult> ExecuteAsync(Workflow.Contracts.ActivityContext context, CancellationToken ct)
    {
        if (context.Settings is not { } settings ||
            !settings.TryGetProperty("productIdFrom", out var idKeyProp) ||
            !settings.TryGetProperty("quantityFrom", out var qtyKeyProp) ||
            idKeyProp.GetString() is not { } idKey ||
            qtyKeyProp.GetString() is not { } qtyKey)
            return ActivityResult.Failure("Settings must contain 'productIdFrom' and 'quantityFrom'.");

        // Missing PATH = the designer wired the definition wrong: a real failure.
        if (!context.InstanceContext.TryResolvePath(idKey, out var idElement))
            return ActivityResult.Failure($"Context key '{idKey}' is missing.");

        if (!context.InstanceContext.TryResolvePath(qtyKey, out var qtyElement) ||
            !qtyElement.TryReadInt(out var quantity) || quantity < 1)
            return ActivityResult.Failure($"Context key '{qtyKey}' is missing or not a positive quantity.");

        // Malformed GUID = the REQUESTER typed garbage: an answer, not a malfunction.
        if (idElement.ValueKind != JsonValueKind.String ||
            !Guid.TryParse(idElement.GetString(), out var productId))
            return ActivityResult.Success(JsonSerializer.SerializeToElement(
                new { exists = false }));

        // Inside the boundary: Catalog's own activity reads Catalog's own tables.
        var product = await db.Products.AsNoTracking()
            .Where(p => p.Id == productId)
            .Select(p => new { p.Name, p.Price })
            .SingleOrDefaultAsync(ct);

        if (product is null)
            return ActivityResult.Success(JsonSerializer.SerializeToElement(
                new { productId, exists = false }));

        var total = product.Price * quantity;

        logger.LogInformation(
            "Catalog: workflow {InstanceId} snapshotted {ProductId}: '{Name}' @ {Price} × {Quantity} = {Total}",
            context.InstanceId, productId, product.Name, product.Price, quantity, total);

        return ActivityResult.Success(JsonSerializer.SerializeToElement(
            new { productId, exists = true, name = product.Name, price = product.Price, total }));
    }
}