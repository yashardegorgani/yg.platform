namespace YG.BuildingBlocks.Messaging;

/// <summary>
/// The only messaging surface modules may touch.
/// Publish: announce a committed fact — fire-and-forget, zero-to-many listeners.
/// Invoke: execute exactly one handler and wait for its answer — in-module mediation,
/// or cross-module queries that travel through a Contracts type.
/// </summary>
public interface IYGMessageBus
{
    ValueTask PublishAsync<TMessage>(TMessage message);

    Task<TResponse> InvokeAsync<TResponse>(object message, CancellationToken ct = default);
}