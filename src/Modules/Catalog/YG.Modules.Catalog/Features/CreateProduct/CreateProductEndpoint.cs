using FastEndpoints;
using FluentValidation;
using Wolverine;
using YG.BuildingBlocks.Messaging;
using YG.Modules.Catalog.Domain;

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

public sealed class CreateProductEndpoint(IYGMessageBus bus)
    : Endpoint<CreateProductRequest, CreateProductResponse>
{
    public override void Configure()
    {
        Post("/catalog/products");
        Permissions("catalog.products.create");
    }

    public override async Task HandleAsync(CreateProductRequest req, CancellationToken ct)
    {
        var id = await bus.InvokeAsync<Guid>(
            new CreateProduct(req.Name, req.Price, req.Attributes), ct);
        await Send.OkAsync(new CreateProductResponse(id), ct);
    }
}