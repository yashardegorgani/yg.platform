using Microsoft.EntityFrameworkCore;
using Wolverine.EntityFrameworkCore;

namespace YG.BuildingBlocks.Messaging;

public sealed class WolverineOutbox(IDbContextOutbox inner) : IYGOutbox
{
    public void Enroll(DbContext dbContext) => inner.Enroll(dbContext);

    public ValueTask PublishAsync<TMessage>(TMessage message)
        => inner.PublishAsync(message!);

    public Task SaveChangesAndPublishAsync(CancellationToken ct = default)
        => inner.SaveChangesAndFlushMessagesAsync(ct);
}