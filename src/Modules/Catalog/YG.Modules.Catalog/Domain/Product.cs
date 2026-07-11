namespace YG.Modules.Catalog.Domain;

public sealed class Product
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public string Name { get; set; } = default!;
    public decimal Price { get; set; }
    public ProductAttributes Attributes { get; set; } = new();   // → jsonb column
    public DateTimeOffset CreatedAt { get; set; } = DateTimeOffset.UtcNow;
}

/// <summary>Extensible without migrations: new members = new json fields, no table change.</summary>
public sealed class ProductAttributes
{
    public string? Brand { get; set; }
    public List<string> Tags { get; set; } = new();
    public Dictionary<string, string> Extra { get; set; } = new();
}