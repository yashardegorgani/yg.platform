using FastEndpoints;
using FluentValidation;
using YG.Modules.Catalog.Domain;
using YG.Modules.Catalog.Persistence;

namespace YG.Modules.Catalog.Features.CreateProduct;

public sealed record CreateProductRequest(string Name, decimal Price, ProductAttributes? Attributes);
public sealed record CreateProductResponse(Guid Id);

public sealed class CreateProductValidator : Validator<CreateProductRequest>
{
    public CreateProductValidator()
    {
        RuleFor(x => x.Name).NotEmpty().MaximumLength(200);
        RuleFor(x => x.Price).GreaterThanOrEqualTo(0);
    }
}

public sealed class CreateProductEndpoint(CatalogDbContext db)
    : Endpoint<CreateProductRequest, CreateProductResponse>
{
    public override void Configure()
    {
        Post("/catalog/products");
        AllowAnonymous(); // step 7 replaces this with Permissions(...)
    }

    public override async Task HandleAsync(CreateProductRequest req, CancellationToken ct)
    {
        var product = new Product { Name = req.Name, Price = req.Price, Attributes = req.Attributes ?? new() };
        db.Products.Add(product);
        await db.SaveChangesAsync(ct);
        await Send.OkAsync(new CreateProductResponse(product.Id), ct);
    }
}