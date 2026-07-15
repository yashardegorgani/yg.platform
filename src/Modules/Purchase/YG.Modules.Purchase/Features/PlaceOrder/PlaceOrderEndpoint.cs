using FastEndpoints;
using FluentValidation;
using Wolverine;
using YG.BuildingBlocks.Auth;

namespace YG.Modules.Purchase.Features.PlaceOrder;

public sealed record PlaceOrderRequest(Guid ProductId, int Quantity);
public sealed record PlaceOrderResponse(Guid OrderId);

public sealed class PlaceOrderValidator : Validator<PlaceOrderRequest>
{
    public PlaceOrderValidator()
    {
        RuleFor(x => x.ProductId).NotEmpty();
        RuleFor(x => x.Quantity).GreaterThan(0);
    }
}

public sealed class PlaceOrderEndpoint(IMessageBus bus, IUserContext user)
    : Endpoint<PlaceOrderRequest, PlaceOrderResponse>
{
    public override void Configure()
    {
        Post("/purchase/orders");
        Permissions("purchase.orders.place");
    }

    public override async Task HandleAsync(PlaceOrderRequest req, CancellationToken ct)
    {
        var result = await bus.InvokeAsync<PlaceOrderResult>(
            new PlaceOrder(user.Sub!, req.ProductId, req.Quantity), ct);

        if (!result.Success)
        {
            AddError(result.Reason ?? "rejected");
            await Send.ErrorsAsync(409, ct);   // conflict: the world disagrees with the request
            return;
        }

        await Send.OkAsync(new PlaceOrderResponse(result.OrderId!.Value), ct);
    }
}
