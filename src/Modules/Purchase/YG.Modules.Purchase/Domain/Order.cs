namespace YG.Modules.Purchase.Domain;

public sealed class Order
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public string BuyerSub { get; set; } = "";      // who bought - the Keycloak reference again
    public Guid ProductId { get; set; }             // reference-by-ID, same rule as Inventory

    // Snapshot-at-boundary: what the product was CALLED and what it COST at purchase
    // time. If Catalog renames or reprices tomorrow, order history must not rewrite itself.
    public string ProductName { get; set; } = "";
    public decimal UnitPrice { get; set; }

    public int Quantity { get; set; }
    public DateTimeOffset PlacedAt { get; set; } = DateTimeOffset.UtcNow;
}
