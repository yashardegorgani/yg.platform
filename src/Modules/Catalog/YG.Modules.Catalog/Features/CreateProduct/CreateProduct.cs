using YG.Modules.Catalog.Domain;

namespace YG.Modules.Catalog.Features.CreateProduct;

// Command: imperative name, an instruction, exactly one handler, caller wants the result.
public sealed record CreateProduct(string Name, decimal Price, ProductAttributes? Attributes);