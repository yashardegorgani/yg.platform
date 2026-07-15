namespace YG.Modules.Inventory.Contracts;

// Command with a reply: imperative name, exactly one handler (Inventory), and the
// caller (Purchase) waits for the verdict. Our first request/response contract -
// contrast with ProductCreated, which is fire-and-forget with 0..n listeners.
public sealed record ReserveStock(Guid ProductId, int Quantity);

/// <summary>The verdict. Reason is a machine-readable code, not prose for humans.</summary>
public sealed record ReservationResult(bool Success, string? Reason)
{
    public const string UnknownProduct = "unknown-product";
    public const string InsufficientStock = "insufficient-stock";
    public const string InvalidQuantity = "invalid-quantity";

    public static ReservationResult Ok() => new(true, null);
    public static ReservationResult Fail(string reason) => new(false, reason);
}
