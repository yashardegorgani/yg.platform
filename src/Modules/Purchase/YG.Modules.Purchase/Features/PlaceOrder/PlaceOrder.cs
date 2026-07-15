namespace YG.Modules.Purchase.Features.PlaceOrder;

// Command: the endpoint resolves WHO (from IUserContext) before sending, so the
// handler stays ignorant of HTTP and claims - same rule as everywhere else.
public sealed record PlaceOrder(string BuyerSub, Guid ProductId, int Quantity);

public sealed record PlaceOrderResult(bool Success, Guid? OrderId, string? Reason);
