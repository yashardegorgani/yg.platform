namespace YG.Modules.Inventory.Domain;

public sealed class StockItem
{
    // Catalog's product ID held as a plain value - reference-by-ID, no FK across
    // schemas. Inventory does not know (or care) what the product is called or costs.
    public Guid ProductId { get; set; }
    public int Quantity { get; set; }
    public DateTimeOffset UpdatedAt { get; set; } = DateTimeOffset.UtcNow;
}
