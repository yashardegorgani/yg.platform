using YG.Modules.Catalog.Domain;

namespace YG.Modules.Catalog.Features.CreateProduct;

// Command: imperative name, an instruction, exactly one handler, caller wants the result.
public sealed record CreateProduct(string Name, decimal Price, ProductAttributes? Attributes);

// Event: past-tense name, a fact, zero-to-many listeners.
public sealed record ProductCreated(Guid Id, string Name);