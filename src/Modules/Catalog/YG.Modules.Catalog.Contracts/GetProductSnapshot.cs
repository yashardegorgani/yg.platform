namespace YG.Modules.Catalog.Contracts;

// Query with a reply: another module asks, Catalog answers. Lives in Contracts
// because the asker (Purchase) is outside Catalog's boundary.
public sealed record GetProductSnapshot(Guid ProductId);

/// <summary>
/// What Catalog is willing to tell outsiders about a product - deliberately NOT the
/// Product entity. No Attributes, no CreatedAt: the contract exposes the minimum.
/// </summary>
public sealed record ProductSnapshot(Guid Id, string Name, decimal Price);
