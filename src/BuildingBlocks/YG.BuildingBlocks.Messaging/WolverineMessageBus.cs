using Wolverine;

namespace YG.BuildingBlocks.Messaging;

public sealed class WolverineMessageBus(IMessageBus inner) : IYGMessageBus
{
    public ValueTask PublishAsync<TMessage>(TMessage message)
        => inner.PublishAsync(message);

    public Task<TResponse> InvokeAsync<TResponse>(object message, CancellationToken ct = default)
        => inner.InvokeAsync<TResponse>(message!, ct);
}